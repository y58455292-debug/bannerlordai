// Minimal native boundary fixtures. These do not replace or ship with ClanAI.
using System;
using System.Collections.Generic;
using System.Reflection;
namespace HarmonyLib {
 public class Harmony { public Harmony(string id) {} public void Patch(MethodInfo m, HarmonyMethod postfix) {} }
 public class HarmonyMethod { public HarmonyMethod(Type t,string n) {} }
 public static class AccessTools { public static MethodInfo Method(Type t,string n,Type[] p=null) {
 return p==null ? t.GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic) :
 t.GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic,null,p,null); }}
}
namespace TaleWorlds.ObjectSystem {
 public class MBObjectManager {
 public static MBObjectManager Instance=new MBObjectManager();
 public readonly Dictionary<string,object> Objects=new Dictionary<string,object>();
 public T GetObject<T>(string id) where T:class { object o; return Objects.TryGetValue(id,out o)?o as T:null; }
 }}
namespace TaleWorlds.CampaignSystem {
 public interface IFaction { List<IFaction> FactionsAtWarWith{get;} bool IsBanditFaction{get;} bool IsOutlaw{get;} }
 public class Clan:IFaction {
 public static Clan PlayerClan=new Clan();
 public List<IFaction> FactionsAtWarWith{get;}=new List<IFaction>();
 public bool IsBanditFaction{get;set;} public bool IsOutlaw{get;set;}
 }
 public class Hero {
 public static Hero MainHero;
 public string StringId; public Clan Clan; public float Age=30;
 public bool IsHumanPlayerCharacter,IsTemplate,IsPrisoner;
 public bool IsAlive=true,IsLord=true,IsPlayerCompanion,IsActive=true;
 public Party.MobileParty PartyBelongedTo;
 }
 public class Campaign {
 public static Campaign Current=new Campaign();
 public Models Models=new Models();
 public CampaignBehaviors.IDisbandPartyCampaignBehavior Disband;
 public T GetCampaignBehavior<T>() where T:class {return Disband as T;}
 }
 public class Models {public AgeModel AgeModel=new AgeModel();}
 public class AgeModel {public int HeroComesOfAge=18;}
 public struct CampaignTime {public static CampaignTime Now; public double ToHours;}
 public enum AiBehavior {GoToSettlement,PatrolAroundPoint,DefendSettlement,Other}
 public struct AIBehaviorData {public object Party; public AiBehavior AiBehavior;}
 public class PartyThinkParams {public List<Tuple<AIBehaviorData,float>> AIBehaviorScores=new List<Tuple<AIBehaviorData,float>>();}
}
namespace TaleWorlds.CampaignSystem.CampaignBehaviors {
 public interface IDisbandPartyCampaignBehavior { bool IsPartyWaitingForDisband(Party.MobileParty p); }
}
namespace TaleWorlds.CampaignSystem.Settlements {
 public class Settlement {public string StringId; public bool IsTown,IsCastle,IsUnderSiege,IsUnderRaid; public CampaignSystem.Clan OwnerClan;}
}
namespace TaleWorlds.CampaignSystem.Party {
 public class PartyAi {public bool IsDisabled,DoNotMakeNewDecisions;}
 public class MobileParty {
 public enum NavigationType {None,Default,Naval}
 public static MobileParty MainParty;
 public bool IsActive=true,IsLordParty=true,IsMainParty,IsCaravan,IsDisbanding;
 public object Army,MapEvent,SiegeEvent,AttachedTo;
 public CampaignSystem.Clan ActualClan;
 public CampaignSystem.IFaction MapFaction;
 public CampaignSystem.Hero LeaderHero;
 public string StringId;
 public PartyAi Ai=new PartyAi();
 public NavigationType NavigationCapability=NavigationType.Default;
 public CampaignSystem.AiBehavior DefaultBehavior,ShortTermBehavior;
 public Settlements.Settlement TargetSettlement,ShortTermTargetSettlement;
 }}
namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors {
 using TaleWorlds.CampaignSystem.Party;
 using TaleWorlds.CampaignSystem.Settlements;
 public class AiVisitSettlementBehavior {
 private readonly struct SettlementNavigationData {
 public readonly float Distance; public readonly int SettlementIdentifier;
 public readonly Settlement Settlement; public readonly MobileParty.NavigationType BestNavigationType;
 public readonly bool IsFromPort,IsTargetingPortBetter;
 public SettlementNavigationData(float d,int id,Settlement s,MobileParty.NavigationType n,bool f,bool t)
 {Distance=d;SettlementIdentifier=id;Settlement=s;BestNavigationType=n;IsFromPort=f;IsTargetingPortBetter=t;}
 }
 public static bool Suitable=true; public static MobileParty.NavigationType Navigation=MobileParty.NavigationType.Default;
 public static Settlement Seen; public static int Calls;
 private static void FillSettlementsToVisitWithDistancesAsDays(MobileParty p,List<SettlementNavigationData> list) {}
 private static bool IsSettlementSuitableForVisitingCondition(MobileParty p,Settlement s) {Seen=s;Calls++;return Suitable;}
 private static void GetBestNavigationDataForVisitingSettlement(MobileParty p,Settlement s,
 out MobileParty.NavigationType nav,out float distance,out bool from,out bool target)
 {nav=Navigation;distance=10f;from=false;target=false;}
 public static System.Collections.IList List() {return new List<SettlementNavigationData>();}
 }}
namespace ClanAI {
 internal static class RuntimeProfile {internal static bool EvidenceEnabled=true;}
 internal static class ClanAIPostVanilla {internal static readonly List<string> Logs=new List<string>();internal static void WriteExternalLog(string s){Logs.Add(s);}}
 internal static class StrategicDecisionComposer {
 internal sealed class Frame {
 internal int BeforeWinner,CandidateCount;
 internal readonly Dictionary<int,float> Scores=new Dictionary<int,float>();
 internal float CurrentScore(int i,float raw){return Scores.ContainsKey(i)?Scores[i]:raw;}
 internal int CurrentBestIndex(TaleWorlds.CampaignSystem.PartyThinkParams t) {
 int best=-1;float score=float.MinValue;for(int i=0;i<t.AIBehaviorScores.Count;i++){float s=CurrentScore(i,t.AIBehaviorScores[i].Item2);if(s>score){best=i;score=s;}}return best;
 }
 internal void ApplyFactor(int i,string source,float before,float factor,string reason){if(i>=0&&i<CandidateCount)Scores[i]=before*factor;}
 }
 }}
