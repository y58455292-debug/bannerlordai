import argparse
import json
import math
import pathlib
import re
import time

ROOT = pathlib.Path(r"D:\BannerlordAIResearch")
TR = ROOT / "Automation/TestRunner"
DP = ROOT / "Automation/DecisionPolicy"
POLICY = json.loads(
    (DP / "manan_live_policy.json").read_text(encoding="utf-8")
)
WEIGHTS = POLICY["weights"]
MAX_MEMORY_SCORE_DELTA = 0.18

def read_kv(path):
    out = {}
    try:
        lines = pathlib.Path(path).read_text(
            encoding="utf-8-sig",
            errors="replace",
        ).splitlines()
    except FileNotFoundError:
        return out
    for line in lines:
        if "=" in line:
            k, v = line.split("=", 1)
            out[k] = v
    return out

def parse_live_decision():
    kv = read_kv(TR / "decision_state.txt")
    if kv.get("type") != "incident":
        return None
    count = int(kv.get("optionCount", "0"))
    return {
        "title": kv.get("title", ""),
        "description": kv.get("description", ""),
        "options": [kv.get(f"option{i}Text", "") for i in range(count)],
    }

def read_choice_receipt(path):
    p = pathlib.Path(path)
    text = p.read_text(encoding="utf-8").strip()
    if not text:
        raise RuntimeError("choice receipt is empty")
    if text.startswith("{") and "\n" not in text:
        return json.loads(text)
    lines = [x for x in text.splitlines() if x.strip()]
    return json.loads(lines[-1])

def fv():
    return {
        "family_survival": 0.0,
        "territorial_defense": 0.0,
        "military_readiness": 0.0,
        "logistics_food_supply": 0.0,
        "wealth_reserve": 0.0,
        "local_stability": 0.0,
        "legitimacy_influence": 0.0,
        "personal_relationships": 0.0,
        "mercy_honor": 0.0,
        "fear_deterrence": 0.0,
        "long_term_risk": 0.0,
        "immediate_cost": 0.0,
        "doctrine_alignment": 0.0,
        "memory_resonance": 0.0,
        "uncertainty_penalty": -0.08,
    }

def clamp(x):
    return max(-1.0, min(1.0, x))

def infer_baseline(text, desc):
    t = (text + " " + desc).lower()
    f = fv()
    hits = 0

    def add(name, val):
        nonlocal hits
        f[name] = clamp(f[name] + val)
        hits += 1

    if any(w in t for w in (
        "gold", "coin", "reward", "pay", "purse",
        "generously", "shower him",
    )):
        add("wealth_reserve", -0.18)
        add("immediate_cost", -0.18)
    if any(w in t for w in (
        "little amount of coin", "small amount", "little coin",
    )):
        add("wealth_reserve", -0.06)
        add("immediate_cost", -0.06)

    if any(w in t for w in (
        "renown", "heroic deeds", "epic ballad",
        "recount your", "fame", "reputation",
    )):
        add("legitimacy_influence", 0.30)
    if "embellish" in t:
        add("legitimacy_influence", 0.22)
        add("mercy_honor", -0.22)
        add("long_term_risk", -0.10)
    if any(w in t for w in (
        "losses and faults", "faults as well", "describe your losses",
    )):
        add("mercy_honor", 0.34)
        add("legitimacy_influence", 0.10)
        add("long_term_risk", 0.08)
    if any(w in t for w in (
        "don't need a bard", "do not need a bard", "you don't need",
    )):
        add("wealth_reserve", 0.14)
        add("immediate_cost", 0.12)
        add("legitimacy_influence", -0.06)

    if any(w in t for w in (
        "reward him", "generously", "honest", "truth", "faults",
    )):
        add("personal_relationships", 0.16)
        add("local_stability", 0.10)
    if any(w in t for w in (
        "madness", "daemon", "witch",
        "terrible but heroic death", "tragic tales",
    )):
        add("legitimacy_influence", -0.20)
        add("long_term_risk", -0.28)
        add("uncertainty_penalty", -0.10)

    if any(w in t for w in ("faults", "losses", "truth", "honest")):
        add("memory_resonance", 0.18)
    if "embellish" in t:
        add("memory_resonance", -0.18)

    return f, hits

def baseline_score(text, desc):
    f, hits = infer_baseline(text, desc)
    contrib = {}
    total = 0.0
    for name, value in f.items():
        weight = float(WEIGHTS.get(name, 0.0))
        delta = float(value) * weight
        contrib[name] = round(delta, 4)
        total += delta
    confidence = min(0.92, 0.48 + 0.035 * hits)
    return round(total, 4), confidence, f, contrib

def baseline_rank(decision):
    rows = []
    for index, text in enumerate(decision["options"]):
        score, conf, features, contrib = baseline_score(
            text,
            decision["description"],
        )
        rows.append({
            "index": index,
            "text": text,
            "score": score,
            "confidence": conf,
            "features": features,
            "contrib": contrib,
        })
    rows.sort(key=lambda x: (-x["score"], x["index"]))
    return rows

CONCEPT_TERMS = {
    "family_survival": (
        "survival", "survive", "safe", "safety",
        "protect", "family", "children", "kin",
    ),
    "territorial_defense": (
        "defend", "defense", "settlement", "town",
        "village", "castle", "border", "home",
    ),
    "military_readiness": (
        "recruit", "recruits", "troop", "troops",
        "soldier", "soldiers", "men", "army",
        "lancer", "warrior", "equipment", "weapon",
    ),
    "logistics_food_supply": (
        "supply", "supplies", "food", "grain",
        "provision", "provisions", "ration", "rations",
    ),
    "wealth_reserve": (
        "money", "gold", "coin", "purse",
        "pay", "spend", "cost", "loan", "debt", "interest",
    ),

    "local_stability": (
        "merchant", "villager", "villagers", "stability",
        "peace", "calm", "dispute", "order",
    ),
    "legitimacy_influence": (
        "reputation", "renown", "influence",
        "fame", "legitimacy", "respect",
    ),
    "personal_relationships": (
        "friend", "relationship", "companion", "family",
    ),
    "mercy_honor": (
        "mercy", "honor", "honest", "truth",
        "spare", "forgive", "faults", "repay", "debt",
    ),
    "fear_deterrence": (
        "threaten", "chase", "punish", "attack", "fear", "blood",
    ),
    "long_term_risk": (
        "risk", "survival", "future", "danger", "loss",
    ),
    "immediate_cost": (
        "pay", "spend", "cost", "gold", "coin", "purse", "money",
    ),
}

def concept_profile(text):
    t = text.lower()
    profile = {}
    for name, terms in CONCEPT_TERMS.items():
        hits = sum(1 for term in terms if term in t)
        profile[name] = min(1.0, hits / 2.0)
    return profile

def weighted_jaccard(a, b):
    names = set(a) | set(b)
    numerator = 0.0
    denominator = 0.0
    for name in names:
        weight = abs(float(WEIGHTS.get(name, 1.0)))
        av = float(a.get(name, 0.0))
        bv = float(b.get(name, 0.0))
        numerator += min(av, bv) * weight
        denominator += max(av, bv) * weight
    return numerator / denominator if denominator > 0 else 0.0

STOP = {
    "the", "a", "an", "to", "of", "and", "or", "your",
    "you", "his", "her", "their", "our", "in", "on", "for",
    "with", "that", "this", "be", "is", "are", "as",
}

def token_set(text):
    return {
        token for token in re.findall(r"[a-z]+", text.lower())
        if len(token) > 2 and token not in STOP
    }

def token_jaccard(a, b):
    left = token_set(a)
    right = token_set(b)
    union = left | right
    if not union:
        return 0.0
    return len(left & right) / len(union)

def memory_similarity(memory_text, option_text):
    memory_profile = concept_profile(memory_text)
    option_profile = concept_profile(option_text)
    concept = weighted_jaccard(memory_profile, option_profile)
    lexical = token_jaccard(memory_text, option_text)
    combined = 0.90 * concept + 0.10 * lexical
    return min(1.0, max(0.0, combined)), memory_profile, option_profile

def recency_decay(current_hours, known_hours):
    age_hours = max(0.0, float(current_hours) - float(known_hours))
    age_days = age_hours / 24.0
    return 1.0 / (1.0 + age_days / 30.0)

def adjusted_rank(decision, baseline, choice_receipt):
    choice = choice_receipt.get("latestChoice") or {}
    memory_text = choice.get("optionText") or ""
    current_hours = float(
        choice_receipt.get("currentCampaignHours") or 0.0
    )
    known_hours = float(choice.get("knownByHours") or 0.0)
    decay = recency_decay(current_hours, known_hours)
    rows = []

    for base in baseline:
        similarity, mem_profile, opt_profile = memory_similarity(
            memory_text,
            base["text"],
        )
        delta = min(
            MAX_MEMORY_SCORE_DELTA,
            MAX_MEMORY_SCORE_DELTA * similarity * decay,
        )
        row = dict(base)
        row["baselineScore"] = base["score"]
        row["memoryDelta"] = round(delta, 4)
        row["adjustedScore"] = round(base["score"] + delta, 4)

        row["memory"] = {
            "similarity": round(similarity, 4),
            "recencyDecay": round(decay, 4),
            "maxScoreDelta": MAX_MEMORY_SCORE_DELTA,
            "memoryProfile": mem_profile,
            "optionProfile": opt_profile,
            "sourceChoiceId": choice.get("id"),
            "sourceContextId": choice.get("contextId"),
            "sourceOptionIndex": choice.get("optionIndex"),
            "sourceOptionText": memory_text,
            "source": choice.get("source"),
        }
        rows.append(row)

    rows.sort(key=lambda x: (-x["adjustedScore"], x["index"]))
    return rows

def margin(rows, key):
    if len(rows) < 2:
        return None
    return round(rows[0][key] - rows[1][key], 4)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--decision-json")
    ap.add_argument(
        "--choice-file",
        default=str(TR / "dynasty_branch_choice_retrievals.jsonl"),
    )
    ap.add_argument(
        "--out",
        default=str(DP / "current_persisted_choice_shadow.json"),
    )
    args = ap.parse_args()

    if args.decision_json:
        decision = json.loads(
            pathlib.Path(args.decision_json).read_text(encoding="utf-8")
        )
    else:
        decision = parse_live_decision()
    if not decision:
        raise SystemExit("no active incident decision")

    choice_receipt = read_choice_receipt(args.choice_file)
    choice = choice_receipt.get("latestChoice") or {}
    if (
        choice_receipt.get("schema") !=
            "BannerlordAI.DynastyBranchChoiceRetrieval.v1"
        or choice.get("actorValid") is not True
        or choice.get("branchValid") is not True
        or choice.get("futureLeak") is not False
        or choice_receipt.get("scoreMutation") is not False
    ):
        raise RuntimeError("choice receipt failed validity gates")

    baseline = baseline_rank(decision)
    adjusted = adjusted_rank(decision, baseline, choice_receipt)
    baseline_top = baseline[0]
    adjusted_top = adjusted[0]
    would_flip = baseline_top["index"] != adjusted_top["index"]

    incident_id = (
        "incident_" +
        re.sub(r"[^a-z0-9]+", "_", decision["title"].lower()).strip("_")
    )

    receipt = {
        "schema": "BannerlordAI.PersistedChoiceShadow.v1",
        "wallUnix": time.time(),
        "mode": "observe",
        "actor": choice_receipt.get("actor"),
        "actorId": choice_receipt.get("actorId"),
        "branchId": choice_receipt.get("branchId"),
        "currentIncident": {
            "id": incident_id,
            "title": decision["title"],
            "description": decision["description"],
        },
        "memoryChoice": choice,
        "baseline": {
            "selected": baseline_top["index"],
            "selectedText": baseline_top["text"],
            "margin": margin(baseline, "score"),
            "ranking": baseline,
        },

        "memoryAdjusted": {
            "selected": adjusted_top["index"],
            "selectedText": adjusted_top["text"],
            "margin": margin(adjusted, "adjustedScore"),
            "ranking": adjusted,
        },
        "maxMemoryScoreDelta": MAX_MEMORY_SCORE_DELTA,
        "wouldFlip": would_flip,
        "executionApplied": False,
        "intentMutation": False,
        "scoreMutation": False,
        "nativeScoreWrites": 0,
        "note": (
            "Choice continuity only. A past choice can boost similar options; "
            "negative reinforcement requires later outcome learning."
        ),
    }

    out = pathlib.Path(args.out)
    out.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps(receipt, indent=2))

if __name__ == "__main__":
    main()
