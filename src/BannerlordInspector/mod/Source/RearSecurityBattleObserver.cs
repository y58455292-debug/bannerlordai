using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerlordInspector
{
    /// <summary>
    /// Read-only, event-driven proof bridge for rear-security research.
    /// Logs only map battles where a lord party and a bandit party are the
    /// two primary participants, together with the lord's public AI state
    /// at battle start. No polling and no campaign state mutation.
    /// </summary>
    public sealed class RearSecurityBattleObserver : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent
                .AddNonSerializedListener(
                    this,
                    new Action<CampaignGameStarter>(
                        OnSessionLaunched));

            CampaignEvents.MapEventStarted.AddNonSerializedListener(
                this,
                new Action<MapEvent, PartyBase, PartyBase>(
                    OnMapEventStarted));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static void OnSessionLaunched(
            CampaignGameStarter starter)
        {
            InitiativeModelProbe.LogActiveModel();

            InspectorLog.Info(
                "RUNTIME_PURSUIT_DISCOVERY_READY");
        }

        private static void OnMapEventStarted(
            MapEvent mapEvent,
            PartyBase attacker,
            PartyBase defender)
        {
            try
            {
                MobileParty a =
                    attacker == null
                        ? null
                        : attacker.MobileParty;

                MobileParty d =
                    defender == null
                        ? null
                        : defender.MobileParty;

                MobileParty lord = null;
                MobileParty bandit = null;

                if (IsLord(a) && IsBandit(d))
                {
                    lord = a;
                    bandit = d;
                }
                else if (IsLord(d) && IsBandit(a))
                {
                    lord = d;
                    bandit = a;
                }

                if (lord == null || bandit == null)
                    return;

                InspectorLog.Info(
                    "REAR_SECURITY_BATTLE_START" +
                    " lord=" +
                    SafeName(lord) +
                    " lordId=" +
                    SafeId(lord) +
                    " bandit=" +
                    SafeName(bandit) +
                    " banditId=" +
                    SafeId(bandit) +
                    " defaultBehavior=" +
                    Safe(
                        delegate
                        {
                            return lord.DefaultBehavior.ToString();
                        }) +
                    " shortTermBehavior=" +
                    Safe(
                        delegate
                        {
                            return lord.ShortTermBehavior.ToString();
                        }) +
                    " targetParty=" +
                    SafeName(
                        SafeRead(
                            delegate
                            {
                                return lord.TargetParty;
                            })) +
                    " shortTermTargetParty=" +
                    SafeName(
                        SafeRead(
                            delegate
                            {
                                return lord.ShortTermTargetParty;
                            })) +
                    " targetSettlement=" +
                    SafeName(
                        SafeRead(
                            delegate
                            {
                                return lord.TargetSettlement;
                            })) +
                    " shortTermTargetSettlement=" +
                    SafeName(
                        SafeRead(
                            delegate
                            {
                                return lord.ShortTermTargetSettlement;
                            })) +
                    " engaging=" +
                    Safe(
                        delegate
                        {
                            return lord.IsEngaging.ToString();
                        }) +
                    " readiness=" +
                    Safe(
                        delegate
                        {
                            return lord.PartySizeRatio
                                .ToString("0.000");
                        }) +
                    " foodDays=" +
                    Safe(
                        delegate
                        {
                            return lord
                                .GetNumDaysForFoodToLast()
                                .ToString();
                        }) +
                    " inArmy=" +
                    Safe(
                        delegate
                        {
                            return (
                                lord.Army != null
                            ).ToString();
                        }) +
                    " men=" +
                    Safe(
                        delegate
                        {
                            return lord.MemberRoster
                                .TotalManCount
                                .ToString();
                        }));
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "Rear-security battle observer failed: " +
                    ex.GetType().Name +
                    " - " +
                    ex.Message);
            }
        }

        private static bool IsLord(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsLordParty;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBandit(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsBandit;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeName(
            MobileParty party)
        {
            try
            {
                return
                    party == null
                        ? "<null>"
                        : party.Name.ToString();
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static string SafeName(
            TaleWorlds.CampaignSystem.Settlements.Settlement settlement)
        {
            try
            {
                return
                    settlement == null
                        ? "<null>"
                        : settlement.Name.ToString();
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static string SafeId(
            MobileParty party)
        {
            try
            {
                return
                    party == null
                        ? "<null>"
                        : party.StringId;
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static string Safe(
            Func<string> read)
        {
            try
            {
                string value = read();
                return value ?? "<null>";
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static T SafeRead<T>(
            Func<T> read)
            where T : class
        {
            try
            {
                return read();
            }
            catch
            {
                return null;
            }
        }
    }
}
