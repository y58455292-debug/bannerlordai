from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
OBSERVER = (BASE / "Phase4ARecoveryObserverBehavior.cs").read_text(encoding="utf-8")
POLICY = (BASE / "Phase4ARecoveryClassificationPolicy.cs").read_text(encoding="utf-8")
SUBMODULE = (BASE / "SubModule.cs").read_text(encoding="utf-8")

checks = {
    "behavior_registered":
        r"starter\.AddBehavior\(new Phase4ARecoveryObserverBehavior\(\)\)",
    "session_launch":
        r"CampaignEvents\.OnSessionLaunchedEvent",
    "party_created":
        r"CampaignEvents\.MobilePartyCreated",
    "party_destroyed":
        r"CampaignEvents\.MobilePartyDestroyed",
    "map_event_ended":
        r"CampaignEvents\.MapEventEnded",
    "hourly_party":
        r"CampaignEvents\.HourlyTickPartyEvent",
    "before_settlement":
        r"CampaignEvents\.BeforeSettlementEnteredEvent",
    "after_settlement":
        r"CampaignEvents\.AfterSettlementEntered",
    "settlement_left":
        r"CampaignEvents\.OnSettlementLeftEvent",
    "npc_lord_scope":
        r"party\.IsLordParty[\s\S]*?party != MobileParty\.MainParty",
    "player_clan_excluded":
        r"party\.ActualClan !=[\s\S]*?Clan\.PlayerClan",
    "post_battle_native_party":
        r"mapEvent\.PartiesOnSide\(",
    "severe_policy":
        r"Phase4ARecoveryClassificationPolicy[\s\S]*?IsSeverelyDepleted",
    "member_roster_read":
        r"party\.MemberRoster",
    "tier_read":
        r"character\.Tier",
    "wounded_read":
        r"roster\.TotalWounded",
    "party_limit_read":
        r"PartySizeLimit",
    "party_ratio_read":
        r"party\.PartySizeRatio",
    "food_read":
        r"GetNumDaysForFoodToLast\(\)",
    "current_settlement_read":
        r"party\.CurrentSettlement",
    "target_settlement_read":
        r"party\.TargetSettlement",
    "volunteer_read":
        r"notable\.VolunteerTypes",
    "garrison_read":
        r"settlement\.Town[\s\S]*?GarrisonParty",
    "classification_policy":
        r"Phase4ARecoveryClassificationPolicy[\s\S]*?ClassifyIncrease",
    "first_settlement_pre":
        r'"PHASE4A_FIRST_SETTLEMENT_PRE"',
    "settlement_enter_log":
        r'"PHASE4A_SETTLEMENT_ENTER"',
    "settlement_exit_log":
        r'"PHASE4A_SETTLEMENT_EXIT"',
}

failed = []
for name, pattern in checks.items():
    source = SUBMODULE if name == "behavior_registered" else OBSERVER
    if not re.search(pattern, source, re.S):
        failed.append(name)

if "OtherOrUnknownNativeSource" not in POLICY:
    failed.append("unknown-source-policy")
if "UnknownSettlementSource" not in POLICY:
    failed.append("unknown-settlement-policy")

if failed:
    print("FAIL Phase 4A recovery wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS Phase 4A recovery wiring")
print("native creation/battle/hourly/settlement observations and conservative source classification are wired")