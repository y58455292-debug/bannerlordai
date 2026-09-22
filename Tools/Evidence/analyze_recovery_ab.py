import json
import re
import sys
from pathlib import Path

GATE_KINDS = {
    "GLOBAL_AI_WEAK_BANDIT_ENGAGE_CONTROL_PASS": "Observe",
    "GLOBAL_AI_WEAK_BANDIT_ENGAGE_SUPPRESSED": "Suppress",
}

KV_RE = re.compile(
    r"(actorId|targetId|readinessExact|campaignHours|foodDays|gateMode)=([^ ]+)"
)


def parse_gate_lines(lines):
    mode = None
    opportunities = []
    for line in lines:
        if "GLOBAL_AI_RECOVERY_GATE_MODE " in line:
            m = re.search(r" mode=(Observe|Suppress)", line)
            if m:
                mode = m.group(1)
        for kind, expected_mode in GATE_KINDS.items():
            if kind not in line:
                continue
            fields = dict(KV_RE.findall(line))
            if not {"actorId", "targetId", "readinessExact", "campaignHours"} <= set(fields):
                continue
            opportunities.append({
                "kind": kind,
                "expectedMode": expected_mode,
                "actorId": fields["actorId"],
                "targetId": fields["targetId"],
                "readiness": float(fields["readinessExact"]),
                "campaignHours": float(fields["campaignHours"]),
                "foodDays": float(fields.get("foodDays", "nan")),
                "gateMode": fields.get("gateMode", expected_mode),
            })
    return mode, opportunities


def parse_json_records(lines, prefix):
    out = []
    for line in lines:
        if prefix not in line:
            continue
        try:
            out.append(json.loads(line.split(prefix, 1)[1]))
        except Exception:
            pass
    return out


def load_arm(path):
    path = Path(path)
    clan = (path / "clanai_session.log").read_text(
        encoding="utf-8-sig", errors="replace"
    ).splitlines()
    inspector = (path / "inspector_segment.log").read_text(
        encoding="utf-8-sig", errors="replace"
    ).splitlines()
    mode, opportunities = parse_gate_lines(clan)
    if mode is None and opportunities:
        observed_modes = {
            r.get("gateMode") or r.get("expectedMode")
            for r in opportunities
        }
        observed_modes.discard(None)
        if len(observed_modes) == 1:
            mode = next(iter(observed_modes))

    outcome = parse_json_records(inspector, "OUTCOME_V1 ")
    lifecycle = parse_json_records(inspector, "RECOVERY_LIFECYCLE_V1 ")
    hours = [
        float(r["campaignHours"])
        for r in outcome + lifecycle
        if r.get("campaignHours") is not None
    ]
    return {
        "path": str(path),
        "mode": mode,
        "opportunities": opportunities,
        "outcome": outcome,
        "lifecycle": lifecycle,
        "startHour": min(hours) if hours else None,
        "endHour": max(hours) if hours else None,
    }



def clip_arm(arm, end_hour):
    def in_horizon(r):
        h = r.get("campaignHours")
        return h is not None and float(h) <= end_hour

    kept_start_keys = {
        r.get("eventKey")
        for r in arm["outcome"]
        if r.get("kind") == "start"
        and in_horizon(r)
        and r.get("eventKey")
    }

    clipped_outcome = []
    for r in arm["outcome"]:
        kind = r.get("kind")
        if in_horizon(r):
            clipped_outcome.append(r)
            continue

        if (
            kind in {"end", "capture"}
            and r.get("eventKey") in kept_start_keys
        ):
            carried = dict(r)
            carried["carriedPastHorizon"] = True
            clipped_outcome.append(carried)

    return {
        **arm,
        "opportunities": [
            r for r in arm["opportunities"]
            if r["campaignHours"] <= end_hour
        ],
        "outcome": clipped_outcome,
        "lifecycle": [
            r for r in arm["lifecycle"]
            if in_horizon(r)
        ],
        "endHour": end_hour,
    }


def summarize(arm):
    starts = [
        r for r in arm["outcome"]
        if r.get("kind") == "start"
    ]
    ends = {
        r.get("eventKey"): r
        for r in arm["outcome"]
        if r.get("kind") == "end"
    }
    captures = [
        r for r in arm["outcome"]
        if r.get("kind") == "capture"
        and r.get("linkGrade") == "exact_native_object_reference"
    ]
    entered = [
        r for r in arm["lifecycle"]
        if r.get("kind") == "entered"
    ]
    left = [
        r for r in arm["lifecycle"]
        if r.get("kind") == "left"
    ]

    pair_first = {}
    actor_first = {}
    for op in sorted(
        arm["opportunities"],
        key=lambda r: r["campaignHours"]
    ):
        pair_first.setdefault(
            (op["actorId"], op["targetId"]),
            op
        )
        actor_first.setdefault(op["actorId"], op)

    actor_rows = []
    for actor, op in actor_first.items():
        h0 = op["campaignHours"]
        actor_starts = [
            r for r in starts
            if r.get("lordId") == actor
            and float(r.get("campaignHours", -1)) >= h0
        ]
        actor_lifecycle = [
            r for r in entered + left
            if r.get("partyId") == actor
            and float(r.get("campaignHours", -1)) >= h0
        ]
        observations = actor_starts + actor_lifecycle
        observations.sort(
            key=lambda r: float(r.get("campaignHours", 1e99))
        )

        first_cross = next(
            (
                r for r in observations
                if r.get("weakByResearchThreshold") is False
            ),
            None,
        )
        cross_hour = (
            float(first_cross["campaignHours"])
            if first_cross else None
        )

        pre_cross_starts = [
            r for r in actor_starts
            if cross_hour is None
            or float(r["campaignHours"]) < cross_hour
        ]
        weak_pre_cross = [
            r for r in pre_cross_starts
            if r.get("weakByResearchThreshold") is True
        ]
        actor_capture = any(
            r.get("prisonerHeroId") ==
            next(
                (
                    s.get("lordHeroId")
                    for s in actor_starts
                    if s.get("lordHeroId")
                ),
                None,
            )
            for r in captures
        )

        actor_entered = any(
            r.get("partyId") == actor
            and float(r["campaignHours"]) >= h0
            for r in entered
        )

        healthy_starts = [
            r for r in actor_starts
            if r.get("weakByResearchThreshold") is False
        ]

        actor_rows.append({
            "actorId": actor,
            "firstOpportunityHour": h0,
            "settlementEntered": actor_entered,
            "thresholdCrossObserved": first_cross is not None,
            "thresholdCrossHour": cross_hour,
            "weakBattleBeforeCross": bool(weak_pre_cross),
            "exactCaptureObserved": actor_capture,
            "healthyBattleAfterOpportunity": bool(healthy_starts),
        })

    pair_rows = []
    for pair, op in pair_first.items():
        actor, target = pair
        matching = [
            r for r in starts
            if r.get("lordId") == actor
            and r.get("banditId") == target
            and float(r.get("campaignHours", -1)) >=
                op["campaignHours"]
        ]
        matching.sort(
            key=lambda r: float(r.get("campaignHours", 1e99))
        )
        first = matching[0] if matching else None
        end = ends.get(first.get("eventKey")) if first else None
        pair_rows.append({
            "actorId": actor,
            "targetId": target,
            "opportunityHour": op["campaignHours"],
            "opportunityReadiness": op["readiness"],
            "laterSameTargetBattle": first is not None,
            "battleWeak": (
                first.get("weakByResearchThreshold")
                if first else None
            ),
            "battleLordSide": (
                first.get("lordSide")
                if first else None
            ),
            "battleWinningSide": (
                end.get("winningSide")
                if end else None
            ),
            "lordPrisonerAtEnd": (
                end.get("lordIsPrisonerAtCallback")
                if end else None
            ),
        })

    def rate(name):
        if not actor_rows:
            return None
        return sum(bool(r[name]) for r in actor_rows) / len(actor_rows)

    return {
        "mode": arm["mode"],
        "startHour": arm["startHour"],
        "endHour": arm["endHour"],
        "distinctOpportunityPairs": len(pair_rows),
        "distinctOpportunityActors": len(actor_rows),
        "settlementEntryRate": rate("settlementEntered"),
        "thresholdCrossObservedRate": rate("thresholdCrossObserved"),
        "weakBattleBeforeCrossRate": rate("weakBattleBeforeCross"),
        "exactCaptureObservedRate": rate("exactCaptureObserved"),
        "healthyBattleAfterOpportunityRate": rate("healthyBattleAfterOpportunity"),
        "completedBattleCount": sum(
            1 for r in starts if r.get("eventKey") in ends
        ),
        "exactCaptureCount": len(captures),
        "actorRows": actor_rows,
        "pairRows": pair_rows,
    }



def compare(control, treatment):
    if control["startHour"] is None or treatment["startHour"] is None:
        raise SystemExit("Missing campaign-hours evidence")

    control_span = control["endHour"] - control["startHour"]
    treatment_span = treatment["endHour"] - treatment["startHour"]
    common_span = min(control_span, treatment_span)

    control = clip_arm(
        control,
        control["startHour"] + common_span,
    )
    treatment = clip_arm(
        treatment,
        treatment["startHour"] + common_span,
    )

    c = summarize(control)
    t = summarize(treatment)

    rate_keys = [
        "settlementEntryRate",
        "thresholdCrossObservedRate",
        "weakBattleBeforeCrossRate",
        "exactCaptureObservedRate",
        "healthyBattleAfterOpportunityRate",
    ]

    effects = {}
    for key in rate_keys:
        cv = c.get(key)
        tv = t.get(key)
        effects[key] = (
            None
            if cv is None or tv is None
            else tv - cv
        )

    enough = (
        c["distinctOpportunityPairs"] >= 20
        and t["distinctOpportunityPairs"] >= 20
    )

    return {
        "schema": "BannerlordAI.RecoveryAB.v1",
        "commonCampaignHours": common_span,
        "control": c,
        "treatment": t,
        "treatmentMinusControl": effects,
        "minimumPairTargetMet": enough,
        "interpretationLimit": (
            "Engineering A/B evidence. Report effect sizes and "
            "uncertainty; do not infer subjective intent."
        ),
    }


def main():
    if len(sys.argv) != 3:
        raise SystemExit(
            "usage: analyze_recovery_ab.py <control_arm_dir> <treatment_arm_dir>"
        )

    control = load_arm(sys.argv[1])
    treatment = load_arm(sys.argv[2])

    if control["mode"] != "Observe":
        raise SystemExit(
            "First arm is not verified Observe control: " +
            repr(control["mode"])
        )

    if treatment["mode"] != "Suppress":
        raise SystemExit(
            "Second arm is not verified Suppress treatment: " +
            repr(treatment["mode"])
        )

    result = compare(control, treatment)
    print(json.dumps(
        result,
        indent=2,
        ensure_ascii=False,
    ))


if __name__ == "__main__":
    main()
