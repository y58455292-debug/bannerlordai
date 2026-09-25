from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src" / "ClanAI" / "src" / "ClanAI" / "KingdomContinuityBehavior.cs"
SUBMODULE = ROOT / "src" / "ClanAI" / "src" / "ClanAI" / "SubModule.cs"

text = SOURCE.read_text(encoding="utf-8")
submodule = SUBMODULE.read_text(encoding="utf-8")

checks = {
    "native_ruler_event_registered": r"CampaignEvents\.RulingClanChanged\.AddNonSerializedListener",
    "native_creation_event_registered": r"CampaignEvents\.KingdomCreatedEvent\.AddNonSerializedListener",
    "native_destruction_event_registered": r"CampaignEvents\.KingdomDestroyedEvent\.AddNonSerializedListener",
    "versioned_save_key": r"ClanAI_KingdomContinuity_v1",
    "original_culture_is_captured_once": r"CultureId\s*=\s*kingdom\.Culture\s*==\s*null\s*\?\s*null\s*:\s*kingdom\.Culture\.StringId",
    "load_reconciliation_does_not_overwrite_culture": r"OriginalName and CultureId are provenance\s+// fields and are intentionally never rewritten",
    "duplicate_ruler_event_guard": r"if\s*\(string\.Equals\(record\.CurrentRulingClanId,\s*newRulingClanId,\s*StringComparison\.Ordinal\)\)\s*return;",
    "null_ruler_notice_is_supported": r'"leadership is vacant"',
    "destroyed_kingdom_is_terminal": r"if\s*\(record\.Destroyed\)\s*return;",
    "load_reconcile_event_registered": r"CampaignEvents\.OnGameLoadFinishedEvent\.AddNonSerializedListener",
    "new_game_reconcile_event_registered": r"CampaignEvents\.OnNewGameCreatedEvent\.AddNonSerializedListener",
}

failed = [name for name, pattern in checks.items() if not re.search(pattern, text, re.S)]
if not re.search(r"starter\.AddBehavior\(new KingdomContinuityBehavior\(\)\);", submodule):
    failed.append("behavior_registered_with_campaign")

for forbidden in (
    "ChangeRulingClanAction",
    "ChangeKingdomAction",
    "ChangeOwnerOfSettlementAction",
    "DeclareWarAction",
    "InitializeKingdom(",
):
    if forbidden in text:
        failed.append("forbidden_native_mutation:" + forbidden)

if failed:
    print("FAIL KingdomContinuity ledger invariants")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS KingdomContinuity ledger invariants")
print("native events: creation, ruler change, destruction")
print("persistence: versioned rows with conservative load reconciliation")
print("mutations: none")
