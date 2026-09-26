from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
memory = (BASE / "DynastyBranchEpisodeMemory.cs").read_text(encoding="utf-8")
policy = (BASE / "DynastyEpisodeRetrievalPolicy.cs").read_text(encoding="utf-8")
bridge = (BASE / "DynastyMindOperatorBridge.cs").read_text(encoding="utf-8")
failed = []

for token in [
    '"ClanAI_DynastyBranchEpisodes_v1"', '"D1"', '"D2"', '"D2|"',
    'IsAcceptedProductionKind(kind)',
    'BuildLatestRetrievalReceipt', 'BuildLatestChoiceRetrievalReceipt',
    'BuildLatestBranchHistoryReceipt', 'SelectLatestActorEpisode',
    'SelectLatestActorChoice', 'SelectLatestBranchHistory',
    '\\"contextType\\":\\"branch-history\\"',
    '\\"personalMemory\\":false', '\\"provenancePreserved\\":true',
]:
    if token not in memory:
        failed.append("missing memory compatibility/scope token: " + token)

for token in ['RetrieveLatestBranchEpisode', 'RetrieveLatestBranchChoice',
              'RetrieveLatestDynastyHistory']:
    if token not in bridge:
        failed.append("missing bridge entry point: " + token)

personal = re.search(r'BuildLatestRetrievalReceipt.*?\n        }', memory, re.S)
choice = re.search(r'BuildLatestChoiceRetrievalReceipt.*?\n        }', memory, re.S)
if not personal or 'BuildLatestBranchHistoryReceipt' in personal.group(0):
    failed.append("personal episode retrieval depends on branch history")
if not choice or 'BuildLatestBranchHistoryReceipt' in choice.group(0):
    failed.append("personal choice retrieval depends on branch history")

for kind in ('IncidentOpened', 'IncidentChoice'):
    if f'case "{kind}":' not in policy:
        failed.append("production kind not explicitly excluded: " + kind)
if 'default:' not in policy or 'return false;' not in policy:
    failed.append("branch-history allowlist is not closed")

for token in ['KillCharacterAction', 'ChangeClanLeaderAction', 'ChangeRulingClan',
              'ChangeOwnerOfSettlementAction', 'ChangeRelationAction',
              'ChangeKingdomAction', 'dataStore.SyncData', 'System.IO',
              'System.Net', 'TestRunner', 'ChatGPT', 'Codex']:
    if token in policy:
        failed.append("policy mutation/dependency token: " + token)

if ('"D3|"' in memory or 'ClanAI_DynastyBranchEpisodes_v2' in memory or
        '[Saveable' in memory):
    failed.append("new save schema detected")
if 'TestOnlyStructuralHistory' in memory or 'TestOnlyStructuralHistory' in bridge:
    failed.append("test-only structural kind leaked into production")

if failed:
    for item in failed:
        print("FAIL", item)
    raise SystemExit(1)

print("PASS Phase 7C actor-specific retrieval preservation invariant")
print("PASS Phase 7C explicit branch-history isolation invariant")
print("PASS Phase 7C D1/D2 save compatibility invariant")
print("PASS Phase 7C no gameplay mutation/development dependency invariant")

