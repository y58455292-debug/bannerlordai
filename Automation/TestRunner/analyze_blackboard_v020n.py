from pathlib import Path
import re, json, statistics, collections

p = Path(r"D:\BannerlordAIResearch\Telemetry\ClanAI\sessions\ClanAI_20260919_031537_169_v020N_review.log")
lines = [
    x for x in p.read_text(encoding="utf-8-sig", errors="replace").splitlines()
    if "ACTOR_INTENT_SWITCH " in x
]

rx = re.compile(
    r"ACTOR_INTENT_SWITCH actor=(.*?) "
    r"fromState=(\S+) toState=(\S+) "
    r"from=(.*?) to=(.*?) "
    r"objectiveAgeHours=([\-0-9.Ee]+) "
    r"previousEligible=(True|False) "
    r"previousScore=(\S+) winnerScore=(\S+) "
    r"switchGapPct=(\S+) requiredFactor=(\S+) "
    r"runnerGapPct=(\S+) readiness=(\S+) "
    r"foodDays=(\S+) reason=(\S+)"
)

def num(v):
    if v == "NaN":
        return float("nan")
    if v == "+Inf":
        return float("inf")
    if v == "-Inf":
        return float("-inf")
    return float(v)

rows = []
for x in lines:
    m = rx.search(x)
    if not m:
        continue
    actor, fs, ts, fr, to, age, pe, ps, ws, gap, rf, rg, read, food, reason = m.groups()
    rows.append({
        "actor": actor,
        "fromState": fs,
        "toState": ts,
        "from": fr,
        "to": to,
        "age": num(age),
        "eligible": pe == "True",
        "prevScore": num(ps),
        "winnerScore": num(ws),
        "gap": num(gap),
        "required": num(rf),
        "runnerGap": num(rg),
        "readiness": num(read),
        "foodDays": int(food),
        "reason": reason,
    })

elig = [r for r in rows if r["eligible"]]
same = [r for r in elig if r["fromState"] == r["toState"]]
cross = [r for r in elig if r["fromState"] != r["toState"]]

def finite(xs):
    return [x for x in xs if x == x and x not in (float("inf"), float("-inf"))]

gaps = finite([r["gap"] for r in elig])
ages = finite([r["age"] for r in rows])

thresholds = {
    str(t): sum(
        1 for r in elig
        if r["required"] == r["required"] and r["required"] <= t
    )
    for t in [1.02, 1.05, 1.10, 1.15, 1.20, 1.25]
}
near_same = {
    str(pct): sum(
        1 for r in same
        if r["gap"] == r["gap"] and r["gap"] <= pct
    )
    for pct in [0.05, 0.10, 0.20]
}
near_cross = {
    str(pct): sum(
        1 for r in cross
        if r["gap"] == r["gap"] and r["gap"] <= pct
    )
    for pct in [0.05, 0.10, 0.20]
}

combos = collections.Counter(
    (r["fromState"], r["toState"]) for r in rows
)
actor_counts = collections.Counter(
    r["actor"] for r in rows
)

# Exact reversal: A->B followed later by B->A for same actor.
last_transition = {}
reversals = []
for r in rows:
    key = r["actor"]
    prev = last_transition.get(key)
    if prev and prev["from"] == r["to"] and prev["to"] == r["from"]:
        reversals.append({
            "actor": key,
            "first": prev,
            "second": r,
        })
    last_transition[key] = r

mins = sorted(
    [r for r in elig if r["gap"] == r["gap"]],
    key=lambda r: r["gap"],
)[:15]

out = {
    "parsed": len(rows),
    "eligible": len(elig),
    "missing": len(rows) - len(elig),
    "sameStateEligible": len(same),
    "crossStateEligible": len(cross),
    "gapMedian": statistics.median(gaps) if gaps else None,
    "gapP25": statistics.quantiles(gaps, n=4, method="inclusive")[0] if len(gaps) >= 2 else None,
    "gapP75": statistics.quantiles(gaps, n=4, method="inclusive")[2] if len(gaps) >= 2 else None,
    "ageMedianHours": statistics.median(ages) if ages else None,
    "retainedByCommitmentFactor": thresholds,
    "nearSameState": near_same,
    "nearCrossState": near_cross,
    "stateCombos": [
        {"from": k[0], "to": k[1], "count": v}
        for k, v in combos.most_common()
    ],
    "repeatActors": [
        {"actor": a, "switches": c}
        for a, c in actor_counts.most_common()
        if c > 1
    ],
    "exactReversals": reversals,
    "smallestEligibleSwitches": mins,
}

print(json.dumps(out, indent=2, ensure_ascii=False))
