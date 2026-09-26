from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
memory = (BASE / "DynastyBranchEpisodeMemory.cs").read_text(encoding="utf-8")
policy = (BASE / "DynastyStructuralEpisodePolicy.cs").read_text(encoding="utf-8")
retrieval = (BASE / "DynastyEpisodeRetrievalPolicy.cs").read_text(encoding="utf-8")
ledger = (BASE / "KingdomContinuityBehavior.cs").read_text(encoding="utf-8")
failed = []

for token in [
    'RulingClanChangedKind', 'KingdomRulingClanChanged',
    'CampaignEvents.RulingClanChanged', 'kingdomId=',
    'oldRulingClanId=', 'newRulingClanId=', 'oldRulerHeroId=',
    'newRulerHeroId=', 'successionOrdinal=', 'OptionIndex = -1',
    'OptionText = ""', 'IdentityFromStoredRow',
]:
    if token not in policy:
        failed.append("missing structural contract token: " + token)

guard = ledger.find('if (string.Equals(record.CurrentRulingClanId, newRulingClanId')
record = ledger.find('DynastyBranchEpisodeMemory.RecordKingdomRulingClanChanged')
if guard < 0 or record < 0 or record <= guard:
    failed.append("structural writer is not after existing native-state guard")
if ledger.count('RecordKingdomRulingClanChanged') != 1:
    failed.append("structural writer must have exactly one production call site")

if 'case "KingdomRulingClanChanged":' not in retrieval or 'return true;' not in retrieval:
    failed.append("structural kind missing from branch-history allowlist")
for kind in ('IncidentChoice', 'IncidentOpened'):
    if f'case "{kind}":' not in retrieval:
        failed.append(kind + " exclusion missing")

for token in [
    '"ClanAI_DynastyBranchEpisodes_v1"', '"D1"', '"D2"', '"D2|"',
    'IsAcceptedProductionKind(kind)', 'RecordKingdomRulingClanChanged',
]:
    if token not in memory:
        failed.append("missing persistence/writer token: " + token)
if '"D3|"' in memory or 'ClanAI_DynastyBranchEpisodes_v2' in memory:
    failed.append("save schema changed")

combined = policy + "\n" + retrieval + "\n" + ledger
for token in [
    'KillCharacterAction.Apply', 'ChangeClanLeaderAction', 'ChangeRulingClan',
    'ChangeOwnerOfSettlementAction.Apply', 'ChangeRelationAction',
    'ChangeKingdomAction.Apply',
]:
    if token in combined:
        failed.append("political/gameplay mutation token: " + token)

for pattern, label in [
    (r'\.RulingClan\s*=(?!=)', 'RulingClan assignment'),
    (r'\.Leader\s*=(?!=)', 'Leader assignment'),
]:
    if re.search(pattern, combined):
        failed.append("political/gameplay mutation: " + label)

if re.search(r'@?"[A-Za-z]:[\\/]', policy + retrieval):
    failed.append("absolute development path in production policy")

if failed:
    for item in failed:
        print("FAIL", item)
    raise SystemExit(1)

print("PASS Phase 7C-I3 guarded native-event writer invariant")
print("PASS Phase 7C-I3 structural allowlist/personal exclusion invariant")
print("PASS Phase 7C-I3 D1/D2 schema compatibility invariant")
print("PASS Phase 7C-I3 no political/gameplay mutation invariant")

