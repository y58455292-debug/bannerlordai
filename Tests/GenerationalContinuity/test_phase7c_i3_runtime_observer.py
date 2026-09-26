from pathlib import Path
import hashlib, re
ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src/ClanAI/src/ClanAI"
s = (BASE / "DynastyStructuralRuntimeObserver.cs").read_text()
sub = (BASE / "SubModule.cs").read_text()
expected = {'DynastyBranchEpisodeMemory.cs': '86d13bf2698408743b09d6919ce4c3bdef8d1a439220db08cd351ac8c4a63380', 'DynastyStructuralEpisodePolicy.cs': 'e92b09c74cc41bfd9a173a36b14793b6657a9c61544b2dba3be29303c442a0a2', 'DynastyEpisodeRetrievalPolicy.cs': '0df3651de73f06f75dab11737bc693936d46ff22d7c3640da320f33901583aeb', 'DynastyMindOperatorBridge.cs': '0d7d8365198b1fa007d95e155e4cd57343b99ccbf2ec506f39344dd10bc12248', 'KingdomContinuityBehavior.cs': 'b6c98c2d5233557122b490ffca67eddd08b958aabd2fe7c73e02a99abee0e420'}
for name, digest in expected.items():
    assert hashlib.sha256((BASE/name).read_bytes()).hexdigest() == digest, name
for token in ["CampaignEvents.RulingClanChanged", "OnGameLoadFinishedEvent", "OnBeforeSaveEvent", "RetrieveLatestDynastyHistory", "RetrieveLatestBranchEpisode", "RetrieveLatestBranchChoice", "SelectLatestActorEpisode", "IdentityFromStoredRow", "mutationByClanAI=False", "sameIdentityCount=", "personalStructuralVisibleCount=", "I3_RUNTIME_LEDGER", "I3_RUNTIME_ERROR", "FlushPending"]:
    assert token in s, token
for token in ["RecordKingdomRulingClanChanged", "TryCreateRulingClanChanged", "SetValue(", "AddBehavior(", "dataStore.SyncData", "[Saveable", "File.", "Directory.", "System.IO", "System.Net", "HttpClient", "TestRunner", "watchdog", "KillCharacterAction", "ChangeKingdomAction", "ChangeRulingClanAction", "ChangeClanLeaderAction", "ChangeOwnerOfSettlementAction"]:
    assert token not in s, token
assert not re.search(r'\.(?:Leader|RulingClan|Age|IsDead|OwnerClan|ActorId|BranchId)\s*=(?!=)', s)
assert not re.search(r'[A-Za-z]:[\\/]', s)
assert 'public override void SyncData(IDataStore dataStore) { }' in s
assert sub.count('new DynastyStructuralRuntimeObserver()') == 1
assert sub.count('DynastyStructuralRuntimeObserver.FlushPending();') == 1
print('PASS I3 runtime observer event/retrieval/duplicate evidence wiring')
print('PASS I3 runtime observer no gameplay mutation, save schema or external dependency')
print('PASS writer/retrieval/bridge/ledger byte-for-byte preservation')
