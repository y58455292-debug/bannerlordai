using System;
using System.Reflection;
using ClanAI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.ObjectSystem;
class Program {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static MobileParty Actor(string id) {
 var h=new Hero{StringId=id,Name=id,Clan=Clan.PlayerClan};
 var p=new MobileParty{StringId="party_"+id,LeaderHero=h,ActualClan=Clan.PlayerClan,MapFaction=Clan.PlayerClan};h.PartyBelongedTo=p;return p;
 }
 static Settlement Home(string id){return new Settlement{StringId=id,OwnerClan=Clan.PlayerClan,IsCastle=true};}
 static void Main(){
 var a=Actor("a");var b=Actor("b");var home=Home("castle_a");var second=Home("castle_b");Settlement result;
 Check(!HomeAssignmentStore.TryHome(a,out result)&&MBObjectManager.Instance.Objects.Count==0,"unassigned fast path no resolution/enumeration");
 Check(HomeAssignmentStore.Assign(a,home)&&HomeAssignmentStore.Assign(b,second),"multiple runtime assignments");
 Check(HomeAssignmentStore.TryHome(a,out result)&&ReferenceEquals(home,result),"cached direct home");
 a.Army=new object();Check(!HomeAssignmentStore.Eligible(a),"army excluded");a.Army=null;
 a.IsMainParty=true;Check(!HomeAssignmentStore.Eligible(a),"main excluded");a.IsMainParty=false;
 a.IsCaravan=true;Check(!HomeAssignmentStore.Eligible(a),"caravan excluded");a.IsCaravan=false;
 a.NavigationCapability=MobileParty.NavigationType.All;Check(HomeAssignmentStore.Eligible(a),"ship-capable party on land remains eligible");
 a.IsCurrentlyAtSea=true;Check(!HomeAssignmentStore.Eligible(a),"ship-capable party currently at sea excluded");
 a.NavigationCapability=MobileParty.NavigationType.Default;Check(!HomeAssignmentStore.Eligible(a),"current sea state excludes regardless of capability");
 a.IsCurrentlyAtSea=false;Check(HomeAssignmentStore.Eligible(a),"returning to land restores eligibility");
 a.Ai.IsDisabled=true;Check(!HomeAssignmentStore.Eligible(a),"disabled excluded");a.Ai.IsDisabled=false;
 a.Ai.DoNotMakeNewDecisions=true;Check(!HomeAssignmentStore.Eligible(a),"stopped excluded");a.Ai.DoNotMakeNewDecisions=false;
 a.IsDisbanding=true;Check(!HomeAssignmentStore.Eligible(a),"disbanding excluded");a.IsDisbanding=false;
 a.LeaderHero.Age=12;Check(!HomeAssignmentStore.Eligible(a),"child excluded");a.LeaderHero.Age=30;
 a.LeaderHero.PartyBelongedTo=null;Check(!HomeAssignmentStore.Eligible(a)&&HomeAssignmentStore.CurrentHome(a.LeaderHero)==home,"dormant hero keeps home");a.LeaderHero.PartyBelongedTo=a;
 HomeAssignmentVisitPatch.Install();
 var postfix=typeof(HomeAssignmentVisitPatch).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static);
 var list=AiVisitSettlementBehavior.List();postfix.Invoke(null,new object[]{a,list});
 Check(list.Count==1&&AiVisitSettlementBehavior.Seen==home,"actual postfix exposes only exact native home");
 postfix.Invoke(null,new object[]{a,list});Check(list.Count==1,"seam does not duplicate existing home");
 AiVisitSettlementBehavior.Suitable=false;list=AiVisitSettlementBehavior.List();postfix.Invoke(null,new object[]{a,list});Check(list.Count==0,"native unsuitable/hostile refusal");AiVisitSettlementBehavior.Suitable=true;
 AiVisitSettlementBehavior.Navigation=MobileParty.NavigationType.None;postfix.Invoke(null,new object[]{a,list});Check(list.Count==0,"native navigation refusal");AiVisitSettlementBehavior.Navigation=MobileParty.NavigationType.Default;
 a.NavigationCapability=MobileParty.NavigationType.All;AiVisitSettlementBehavior.Navigation=MobileParty.NavigationType.Naval;
 postfix.Invoke(null,new object[]{a,list});Check(list.Count==0,"ship-capable land party cannot expose a naval home route");AiVisitSettlementBehavior.Navigation=MobileParty.NavigationType.Default;
 postfix.Invoke(null,new object[]{a,list});Check(list.Count==1,"ship-capable land party can expose native land home route");a.NavigationCapability=MobileParty.NavigationType.Default;
 var think=new PartyThinkParams();think.AIBehaviorScores.Add(Tuple.Create(new AIBehaviorData{Party=home,AiBehavior=AiBehavior.GoToSettlement},1f));
 think.AIBehaviorScores.Add(Tuple.Create(new AIBehaviorData{Party=second,AiBehavior=AiBehavior.GoToSettlement},1.1f));
 var frame=new StrategicDecisionComposer.Frame{CandidateCount=2,BeforeWinner=1};
 HomeAssignmentLayer.Apply(a,think,frame);
 Check(frame.CurrentBestIndex(think)==0&&think.AIBehaviorScores.Count==2&&ReferenceEquals(think.AIBehaviorScores[0].Item1.Party,home),"composer changes winner preserving count and target");
 a.DefaultBehavior=AiBehavior.GoToSettlement;a.TargetSettlement=second;
 a.ShortTermBehavior=AiBehavior.Other;a.ShortTermTargetSettlement=home;
 HomeAssignmentLayer.Apply(a,think,new StrategicDecisionComposer.Frame{CandidateCount=2,BeforeWinner=1});
 Check(HomeAssignmentCounters.CommitMismatches==1&&HomeAssignmentCounters.CommitMatches==0,"crossed behavior/target pairs do not falsely match");
 a.TargetSettlement=home;
 HomeAssignmentLayer.Apply(a,think,new StrategicDecisionComposer.Frame{CandidateCount=2,BeforeWinner=1});
 Check(HomeAssignmentCounters.CommitMatches==1,"paired native action observed");
 a.DefaultBehavior=AiBehavior.DefendSettlement;a.TargetSettlement=second;
 list=AiVisitSettlementBehavior.List();postfix.Invoke(null,new object[]{a,list});
 frame=new StrategicDecisionComposer.Frame{CandidateCount=2};
 HomeAssignmentLayer.Apply(a,think,frame);
 Check(list.Count==0&&frame.Scores.Count==0,"existing urgent native defense disables seam and policy");
 a.DefaultBehavior=AiBehavior.Other;
 think.AIBehaviorScores.Add(Tuple.Create(new AIBehaviorData{Party=second,AiBehavior=AiBehavior.DefendSettlement},0.1f));
 frame=new StrategicDecisionComposer.Frame{CandidateCount=3};HomeAssignmentLayer.Apply(a,think,frame);
 Check(frame.Scores.Count==0,"new legal urgent clan defense candidate preserves responsibility");
 think.AIBehaviorScores.RemoveAt(2);
 Clan.PlayerClan.FactionsAtWarWith.Add(new Clan());frame=new StrategicDecisionComposer.Frame{CandidateCount=2};
 HomeAssignmentLayer.Apply(a,think,frame);postfix.Invoke(null,new object[]{a,AiVisitSettlementBehavior.List()});
 Check(frame.Scores.Count==0&&!HomeAssignmentStore.Peace(a),"war contribution passthrough");Clan.PlayerClan.FactionsAtWarWith.Clear();
 home.OwnerClan=new Clan();Check(!HomeAssignmentStore.TryHome(a,out result)&&HomeAssignmentStore.CurrentHome(a.LeaderHero)==null,"foreign ownership invalidates immediately");
 Check(HomeAssignmentStore.TryHome(b,out result)&&result==second,"loss does not change another leader");
 Check(HomeAssignmentStore.Assign(a,second)&&HomeAssignmentStore.Clear("a")&&!HomeAssignmentStore.TryHome(a,out result),"runtime replacement/clear");
 HomeAssignmentStore.Records.Set("missinghero","missingsettlement");HomeAssignmentStore.ResolveLoaded();
 Check(!HomeAssignmentStore.Records.TryGet("missinghero",out var id),"missing references rejected on load");
 MBObjectManager.Instance.Objects["b"]=b.LeaderHero;MBObjectManager.Instance.Objects["castle_b"]=second;
 HomeAssignmentStore.Records.Set("b","castle_b");HomeAssignmentStore.BeginNewSession(true);
 Check(HomeAssignmentStore.TryHome(b,out result)&&result==second,"session restores cached identities");
 var recreated=Actor("b");recreated.LeaderHero=b.LeaderHero;b.LeaderHero.PartyBelongedTo=recreated;
 Check(HomeAssignmentStore.TryHome(recreated,out result)&&result==second,"party recreation retains hero identity");
 HomeAssignmentStore.BeginNewSession(false);Check(!HomeAssignmentStore.TryHome(recreated,out result),"new campaign resets static assignments");
 HomeAssignmentStore.BeginNewSession(false);
 Clan.PlayerClan.Heroes.Clear();Clan.PlayerClan.Companions.Clear();
 var rosterActive=Actor("roster_active");rosterActive.LeaderHero.Name="Alrika";
 var rosterSea=Actor("roster_sea");rosterSea.LeaderHero.Name="Biliya";
 var rosterArmy=Actor("roster_army");rosterArmy.LeaderHero.Name="Mira";
 var rosterPrisoner=Actor("roster_prisoner");rosterPrisoner.LeaderHero.Name="Prisoner";
 var rosterNoParty=Actor("roster_no_party");rosterNoParty.LeaderHero.Name="NoParty";
 var rosterHome=Home("roster_home");rosterHome.Name="Balgard";
 Check(HomeAssignmentStore.Assign(rosterActive,rosterHome)&&HomeAssignmentStore.Assign(rosterSea,rosterHome)&&
       HomeAssignmentStore.Assign(rosterArmy,rosterHome)&&HomeAssignmentStore.Assign(rosterPrisoner,rosterHome)&&
       HomeAssignmentStore.Assign(rosterNoParty,rosterHome),"active eligible assignments created by party identity");
 Clan.PlayerClan.Heroes.Add(rosterActive.LeaderHero);Clan.PlayerClan.Heroes.Add(rosterSea.LeaderHero);
 Clan.PlayerClan.Heroes.Add(rosterArmy.LeaderHero);Clan.PlayerClan.Heroes.Add(rosterPrisoner.LeaderHero);
 Clan.PlayerClan.Heroes.Add(rosterNoParty.LeaderHero);
 rosterSea.IsCurrentlyAtSea=true;rosterArmy.Army=new object();rosterPrisoner.LeaderHero.IsPrisoner=true;
 rosterNoParty.LeaderHero.PartyBelongedTo=null;
 Check(!HomeAssignmentStore.Eligible(rosterSea)&&!HomeAssignmentStore.Eligible(rosterArmy)&&
       !HomeAssignmentStore.Eligible(rosterPrisoner)&&!HomeAssignmentStore.Eligible(rosterNoParty.LeaderHero.PartyBelongedTo),"temporary states remain behavior-ineligible");
 var roster=HomeAssignmentRoster.Build(Clan.PlayerClan);
 Check(roster.Contains(rosterActive.LeaderHero)&&roster.Contains(rosterSea.LeaderHero)&&roster.Contains(rosterArmy.LeaderHero)&&
       roster.Contains(rosterPrisoner.LeaderHero)&&roster.Contains(rosterNoParty.LeaderHero),"persistent roster includes active, sea, army, prisoner, and no-party heroes");
 Check(HomeAssignmentRoster.Status(rosterActive.LeaderHero)==HomeAssignmentStatus.Active,"eligible active row status");
 Check(HomeAssignmentRoster.Status(rosterSea.LeaderHero)==HomeAssignmentStatus.AtSea,"sea status suspended");
 Check(HomeAssignmentRoster.Status(rosterArmy.LeaderHero)==HomeAssignmentStatus.InArmy,"army status suspended");
 Check(HomeAssignmentRoster.Status(rosterPrisoner.LeaderHero)==HomeAssignmentStatus.Prisoner,"prisoner status suspended");
 Check(HomeAssignmentRoster.Status(rosterNoParty.LeaderHero)==HomeAssignmentStatus.NoActiveParty,"no-party status suspended");
 Check(HomeAssignmentStore.CurrentHome(rosterSea.LeaderHero)==rosterHome&&HomeAssignmentStore.CurrentHome(rosterArmy.LeaderHero)==rosterHome&&
       HomeAssignmentStore.CurrentHome(rosterPrisoner.LeaderHero)==rosterHome&&HomeAssignmentStore.CurrentHome(rosterNoParty.LeaderHero)==rosterHome,
       "sea, army, prisoner, and no-party preserve saved home");
 Check(HomeAssignmentStore.Clear(rosterSea.LeaderHero.StringId)&&HomeAssignmentStore.CurrentHome(rosterSea.LeaderHero)==null,"selected unavailable-row clear by Hero.StringId");
 Check(HomeAssignmentStore.Assign(rosterActive,second)&&HomeAssignmentStore.CurrentHome(rosterActive.LeaderHero)==second&&
       HomeAssignmentStore.Assign(rosterActive,rosterHome)&&HomeAssignmentStore.CurrentHome(rosterActive.LeaderHero)==rosterHome,
       "eligible party assignment change follows Hero.StringId");
 var neverParty=Actor("no_party_never_assigned");neverParty.LeaderHero.PartyBelongedTo=null;
 Check(!HomeAssignmentStore.Assign(neverParty,rosterHome)&&HomeAssignmentStore.CurrentHome(neverParty.LeaderHero)==null,
       "no dormant assignment created without an eligible party");
 var roleChanged=Actor("role_changed");Check(HomeAssignmentStore.Assign(roleChanged,rosterHome),"assigned hero before roster role change");
 roleChanged.LeaderHero.IsLord=false;roleChanged.LeaderHero.IsPlayerCompanion=false;roleChanged.LeaderHero.PartyBelongedTo=null;
 var roleRoster=HomeAssignmentRoster.Build(Clan.PlayerClan);
 Check(roleRoster.Contains(roleChanged.LeaderHero)&&HomeAssignmentStore.CurrentHome(roleChanged.LeaderHero)==rosterHome,
       "assigned alive clan member remains visible without party or current lord role");
 var dead=Actor("dead_hero");Check(HomeAssignmentStore.Assign(dead,rosterHome),"dead invalidation setup");
 Clan.PlayerClan.Heroes.Add(dead.LeaderHero);dead.LeaderHero.IsAlive=false;
 var alien=Actor("out_of_clan");Check(HomeAssignmentStore.Assign(alien,rosterHome),"out-of-clan invalidation setup");
 Clan.PlayerClan.Heroes.Add(alien.LeaderHero);alien.LeaderHero.Clan=new Clan();
 var invalidRoster=HomeAssignmentRoster.Build(Clan.PlayerClan);
 Check(!invalidRoster.Contains(dead.LeaderHero)&&!HomeAssignmentStore.Records.TryGet("dead_hero",out id),"dead hero excluded and assignment invalidated");
 Check(!invalidRoster.Contains(alien.LeaderHero)&&!HomeAssignmentStore.Records.TryGet("out_of_clan",out id),"out-of-clan hero excluded and assignment invalidated");
 var lostHome=Home("lost_home");var lostLeader=Actor("lost_home_leader");
 Check(HomeAssignmentStore.Assign(lostLeader,lostHome),"lost holding invalidation setup");lostHome.OwnerClan=new Clan();
 HomeAssignmentStore.InvalidateOwnership();
 Check(!HomeAssignmentStore.Records.TryGet("lost_home_leader",out id),"foreign or lost holding invalidates assignment");
 var main=Actor("main_hero");Hero.MainHero=main.LeaderHero;Clan.PlayerClan.Heroes.Add(main.LeaderHero);
 var child=Actor("child_hero");child.LeaderHero.Age=12;Clan.PlayerClan.Heroes.Add(child.LeaderHero);
 var template=Actor("template_hero");template.LeaderHero.IsTemplate=true;Clan.PlayerClan.Heroes.Add(template.LeaderHero);
 var specialRoster=HomeAssignmentRoster.Build(Clan.PlayerClan);
 Check(!specialRoster.Contains(main.LeaderHero),"main hero excluded from household roster");
 Check(!specialRoster.Contains(child.LeaderHero)&&!specialRoster.Contains(template.LeaderHero),"children and templates excluded from household roster");
 Hero.MainHero=null;
 var orderA=Actor("sort_b");orderA.LeaderHero.Name="Same";
 var orderB=Actor("sort_a");orderB.LeaderHero.Name="Same";
 var orderC=Actor("sort_z");orderC.LeaderHero.Name="Alpha";
 Clan.PlayerClan.Heroes.Add(orderA.LeaderHero);Clan.PlayerClan.Heroes.Add(orderB.LeaderHero);Clan.PlayerClan.Companions.Add(orderC.LeaderHero);
 var ordered=HomeAssignmentRoster.Build(Clan.PlayerClan);
 Check(ordered.IndexOf(orderC.LeaderHero)<ordered.IndexOf(orderB.LeaderHero)&&ordered.IndexOf(orderB.LeaderHero)<ordered.IndexOf(orderA.LeaderHero),
       "display name then StringId sort is ordinal and deterministic");
 var disbanding=Actor("disband_status");disbanding.IsDisbanding=true;
 var attached=Actor("attached_status");attached.AttachedTo=new object();
 var stopped=Actor("stopped_status");stopped.Ai.IsDisabled=true;
 var battle=Actor("battle_status");battle.MapEvent=new object();
 var other=Actor("other_status");other.IsCaravan=true;
 Check(HomeAssignmentRoster.Status(disbanding.LeaderHero)==HomeAssignmentStatus.Disbanding,"disbanding status");
 Check(HomeAssignmentRoster.Status(attached.LeaderHero)==HomeAssignmentStatus.Attached,"attached status");
 Check(HomeAssignmentRoster.Status(stopped.LeaderHero)==HomeAssignmentStatus.AiStopped,"stopped AI status");
 Check(HomeAssignmentRoster.Status(battle.LeaderHero)==HomeAssignmentStatus.InBattleOrSiege,"battle status");
 Check(HomeAssignmentRoster.Status(other.LeaderHero)==HomeAssignmentStatus.OtherTemporaryUnavailable,"other temporary status");
 var target=Home("target_settlement");target.Name="Omor";
 rosterActive.CurrentSettlement=null;rosterActive.TargetSettlement=target;
 Check(HomeAssignmentRoster.Location(rosterActive.LeaderHero)=="Traveling toward Omor","safe direct target location");
 rosterActive.CurrentSettlement=rosterHome;
 Check(HomeAssignmentRoster.Location(rosterActive.LeaderHero)=="Balgard","party current settlement location takes precedence");
 rosterNoParty.LeaderHero.CurrentSettlement=target;
 Check(HomeAssignmentRoster.Location(rosterNoParty.LeaderHero)=="Omor","hero settlement used when party is absent");
 Check(HomeAssignmentRoster.StatusText(HomeAssignmentStatus.AtSea).Contains("suspended"),"unavailable status explains suspended responsibility");
 string rosterSource=System.IO.File.ReadAllText("src/ClanAI/src/ClanAI/HomeAssignmentRoster.cs");
 string layerSource=System.IO.File.ReadAllText("src/ClanAI/src/ClanAI/HomeAssignmentLayer.cs");
 string behaviorSource=System.IO.File.ReadAllText("src/ClanAI/src/ClanAI/HomeAssignmentCampaignBehavior.cs");
 Check(!rosterSource.Contains("Hero.All")&&!rosterSource.Contains("MobileParty.All")&&
       !rosterSource.Contains("Settlement.All")&&!rosterSource.Contains("GetAll")&&
       rosterSource.Contains("clan.Heroes")&&rosterSource.Contains("clan.Companions")&&
       rosterSource.Contains("MaximumRows = HomeAssignmentRecords.MaximumRows"),
       "bounded roster uses only direct clan collections, with no global scans");
 Check(!layerSource.Contains("HomeAssignmentRoster")&&behaviorSource.Contains("HomeAssignmentRoster.Build(Clan.PlayerClan)")&&
       behaviorSource.Contains("DisplayName(hero)")&&behaviorSource.Contains("Home: ")&&
       behaviorSource.Contains("StatusText(status)"),
       "menu-demand roster rows show name, home, and status outside unchanged AI layer");
 Console.WriteLine("TOTAL "+count+" PASS");
 }}
