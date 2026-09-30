from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[2]
src = ROOT / "src/ClanAI/src/ClanAI"
texts = {p.name: p.read_text(encoding="utf-8") for p in src.glob("HomeAssignment*.cs")}
combined = "\n".join(texts.values())
assert not re.search(r"\b(?:TargetSettlement|ShortTermTargetSettlement)\s*=(?!=)", combined)
for token in ("new AIBehaviorData", "SetMoveGoToSettlement", "SetPosition", "Teleport", "File.", "Directory.", "Settlement.All", "Hero.All"):
    assert token not in combined, token
store = texts["HomeAssignmentStore.cs"]
assert "Records.TryGet(id, out saved)" in store
assert "MBObjectManager" not in store[store.index("internal static bool TryHome"):store.index("internal static Settlement CurrentHome")]
for token in ("party.IsMainParty", "party.IsCaravan", "party.Army != null", "party.Ai.IsDisabled", "party.Ai.DoNotMakeNewDecisions", "party.IsDisbanding", "party.IsCurrentlyAtSea", "party.LeaderHero.PartyBelongedTo"):
    assert token in store, token
assert "party.NavigationCapability" not in store[store.index("internal static bool Eligible"):store.index("internal static bool Peace")]
assert "home.OwnerClan == Clan.PlayerClan" in store
assert "home.IsTown || home.IsCastle" in store
patch = texts["HomeAssignmentVisitPatch.cs"]
assert "FillSettlementsToVisitWithDistancesAsDays" in patch
assert patch.count("list.Add(") == 1
assert "Suitable.Invoke" in patch and "Navigation.Invoke" in patch
assert "home.GetHashCode()" in patch and "ReferenceEquals(RowSettlement.GetValue(row), home)" in patch
assert "new Harmony" in patch and "postfix:" in patch and "transpiler:" not in patch
layer = texts["HomeAssignmentLayer.cs"]
assert "composer.ApplyFactor(i," in layer and "AIBehaviorScores.Add" not in layer
assert "actor.DefaultBehavior == pending.Behavior && ReferenceEquals(actor.TargetSettlement, pending.Home)" in layer
assert "actor.ShortTermBehavior == pending.Behavior && ReferenceEquals(actor.ShortTermTargetSettlement, pending.Home)" in layer
assert "after == before" in layer and "Records.Revision" in layer
assert "RuntimeProfile.EvidenceEnabled" in layer
ui = texts["HomeAssignmentCampaignBehavior.cs"]
assert 'AddGameMenuOption("town"' in ui and 'AddGameMenuOption("castle"' in ui
assert "ValidHome(holding)" in ui and "HomeAssignmentStore.Eligible(party)" in ui
assert "store.SyncData(HomeAssignmentRecords.SaveKey, ref rows)" in ui
assert '"ClanAI_HomeAssignment_v1"' in texts["HomeAssignmentRecords.cs"]
composer = (src / "StrategicDecisionComposer.cs").read_text(encoding="utf-8")
assert "thinkParams.AIBehaviorScores.Count !=" in composer
strategic = (src / "ClanAIStrategicBehavior.cs").read_text(encoding="utf-8")
assert strategic.count("HomeAssignmentLayer.Apply(party, thinkParams,") == 3
assert "HomeResponsibilityLayer.Apply(" in strategic
assert "EligibleIndependentLordAtWar" in (src / "HomeResponsibilityLayer.cs").read_text(encoding="utf-8")
print("PASS LW1-B native authority, save isolation, O(1), eligibility, UI, composer and paired verifier invariants")
