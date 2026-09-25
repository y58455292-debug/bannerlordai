# Phase 6 exact native audit excerpts

Base: bb46ee809b6d33bfc75d80a805e16ca4b734c88c. Date: 2026-09-25 UTC. OFFLINE ONLY.

## E00. Provenance

TaleWorlds.CampaignSystem.dll SHA-256: 5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F
SandBox.dll SHA-256: 16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A
Both FileVersion=1.0.0.0 and ProductVersion=1.0.0; use binary hashes, not those generic version strings, as the supported-version identifier.
ilspycmd and ICSharpCode.Decompiler 11.0.0.9375; installed SDK 11.0.100-rc.1.26425.128.
Commands: ilspycmd -l c <dll>; ilspycmd -t <inventory-resolved-type> <dll>; SandBox additionally -r <game-bin>.
Process-scoped DOTNET_ROLL_FORWARD=Major and DOTNET_ROLL_FORWARD_TO_PRERELEASE=1 used; no runtime/software installation.
The numbered fragments below are noncontiguous verbatim lines from targeted decompiler output. Omitted blank/braces/other lines are NOT a replacement implementation. Ranges and output hashes allow exact reconstruction. Broad decompiled class files stay local.

## E01. NPC issue resolution

TaleWorlds.CampaignSystem.CampaignBehaviors.IssuesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 377-394
```text
377: private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
379: CharacterObject characterObject = ((party == null) ? hero.CharacterObject : party.LeaderHero?.CharacterObject);
380: if (characterObject == null || characterObject.IsPlayerCharacter || party?.Army != null || !Campaign.Current.GameStarted)
382: return;
384: MBList<IssueBase> mBList = IssueManager.GetIssuesInSettlement(settlement).ToMBList();
385: float num = ((settlement.OwnerClan == characterObject.HeroObject.Clan) ? 0.05f : 0.01f);
386: if (mBList.Count > 0 && MBRandom.RandomFloat < num)
388: IssueBase randomElement = mBList.GetRandomElement();
389: if (randomElement.CanBeCompletedByAI() && randomElement.IsOngoingWithoutQuest)
391: randomElement.CompleteIssueWithAiLord(characterObject.HeroObject);
```

## E02. Completion, effects, finalization

TaleWorlds.CampaignSystem.Issues.IssueBase | TaleWorlds.CampaignSystem.dll | output lines 474-481
```text
474: public float GetActiveIssueEffectAmount(IssueEffect issueEffect)
476: if (!_areIssueEffectsResolved)
478: return GetIssueEffectAmountInternal(issueEffect);
480: return 0f;
```

TaleWorlds.CampaignSystem.Issues.IssueBase | TaleWorlds.CampaignSystem.dll | output lines 640-649
```text
640: public void IssueFinalized()
642: IssueQuest = null;
643: CampaignEventDispatcher.Instance.RemoveListeners(this);
644: Campaign.Current.IssueManager.DeactivateIssue(this);
645: _areIssueEffectsResolved = true;
646: AlternativeSolutionSentTroops.Clear();
647: RemoveAllTrackedObjects();
648: OnIssueFinalized();
```

TaleWorlds.CampaignSystem.Issues.IssueBase | TaleWorlds.CampaignSystem.dll | output lines 711-715
```text
711: public void CompleteIssueWithAiLord(Hero issueSolver)
713: CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFinishedByAILord, issueSolver);
714: IssueFinalized();
```

## E03. Issue lifecycle and effect aggregation

TaleWorlds.CampaignSystem.Issues.IssueManager | TaleWorlds.CampaignSystem.dll | output lines 258-286
```text
258: int progress = ((journalLog.CurrentProgress + 1 > journalLog.Range) ? journalLog.Range : (journalLog.CurrentProgress + 1));
259: journalLog.UpdateCurrentProgress(progress);
262: if (value.IsOngoingWithoutQuest && !value.IssueStayAliveConditions())
264: list3.Add(value);
265: flag = true;
267: if (value.IssueDueTime.IsPast && value.IsOngoingWithoutQuest && !flag && MBRandom.RandomFloat <= 0.2f)
269: list.Add(value);
272: foreach (IssueBase item in list2)
274: item.CompleteIssueWithAlternativeSolution();
276: foreach (IssueBase item2 in list)
278: item2.CompleteIssueWithTimedOut();
280: foreach (IssueBase item3 in list3)
282: item3.CompleteIssueWithStayAliveConditionsFailed();
286: public override void HourlyTick()
```

TaleWorlds.CampaignSystem.GameComponents.DefaultIssueModel | TaleWorlds.CampaignSystem.dll | output lines 28-58
```text
28: public override void GetIssueEffectsOfSettlement(IssueEffect issueEffect, Settlement settlement, ref ExplainedNumber explainedNumber)
30: foreach (Hero aliveLord in settlement.OwnerClan.AliveLords)
32: if (aliveLord.Issue != null)
34: GetIssueEffectOfHeroInternal(issueEffect, aliveLord, ref explainedNumber, SettlementIssuesText);
37: foreach (Hero item in settlement.HeroesWithoutParty)
39: if (item.Issue != null)
41: GetIssueEffectOfHeroInternal(issueEffect, item, ref explainedNumber, SettlementIssuesText);
44: if (!settlement.IsTown && !settlement.IsCastle)
46: return;
48: foreach (Village boundVillage in settlement.BoundVillages)
50: foreach (Hero notable in boundVillage.Settlement.Notables)
52: if (notable.Issue != null)
54: GetIssueEffectOfHeroInternal(issueEffect, notable, ref explainedNumber, RelatedSettlementIssuesText);
```

## E04. Representative issue consequences

TaleWorlds.CampaignSystem.Issues.HeadmanNeedsGrainIssueBehavior | TaleWorlds.CampaignSystem.dll | output lines 208-219
```text
208: protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
210: if (issueEffect == DefaultIssueEffects.SettlementProsperity)
212: return -0.2f;
214: if (issueEffect == DefaultIssueEffects.SettlementLoyalty)
216: return -0.5f;
218: return 0f;
```

TaleWorlds.CampaignSystem.Issues.MerchantNeedsHelpWithOutlawsIssueQuestBehavior | TaleWorlds.CampaignSystem.dll | output lines 161-176
```text
161: protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
163: if (issueEffect == DefaultIssueEffects.SettlementProsperity)
165: return -0.2f;
167: if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
169: return -0.1f;
171: if (issueEffect == DefaultIssueEffects.SettlementSecurity)
173: return -1f;
175: return 0f;
```

TaleWorlds.CampaignSystem.Issues.NearbyBanditBaseIssueBehavior | TaleWorlds.CampaignSystem.dll | output lines 687-697
```text
687: private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
689: if (!(issue is NearbyBanditBaseIssue nearbyBanditBaseIssue) || details != IssueBase.IssueUpdateDetails.IssueFinishedByAILord)
691: return;
693: foreach (MobileParty party in nearbyBanditBaseIssue.TargetHideout.Parties)
695: party.SetMovePatrolAroundSettlement(nearbyBanditBaseIssue.TargetHideout, MobileParty.NavigationType.Default, isTargetingPort: false);
```

## E05. Cleanup must not count as civic action

TaleWorlds.CampaignSystem.CampaignBehaviors.NotablesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 281-306
```text
281: private void DailyTickHero(Hero hero)
283: if (hero.IsNotable && hero.CurrentSettlement != null)
285: if (MBRandom.RandomFloat < 0.01f)
287: UpdateNotableRelations(hero);
289: UpdateNotableSupport(hero);
290: ManageCaravanExpensesOfNotable(hero);
291: CheckAndMakeNotableDisappear(hero);
295: private void CheckAndMakeNotableDisappear(Hero notable)
297: if (notable.OwnedWorkshops.IsEmpty() && notable.OwnedCaravans.IsEmpty() && notable.OwnedAlleys.IsEmpty() && notable.CanDie(KillCharacterAction.KillCharacterActionDetail.Lost) && notable.CanHaveCampaignIssues() && notable.Power < (float)Campaign.Current.Models.NotablePowerModel.NotableDisappearPowerLimit)
299: float randomFloat = MBRandom.RandomFloat;
300: float notableDisappearProbability = GetNotableDisappearProbability(notable);
301: if (randomFloat < notableDisappearProbability)
303: KillCharacterAction.ApplyByRemove(notable);
304: notable.Issue?.CompleteIssueWithAiLord(notable.CurrentSettlement.OwnerClan.Leader);
```

## E06. NPC issue reward and journal boundary

TaleWorlds.CampaignSystem.CampaignBehaviors.IssuesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 396-427
```text
396: private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
398: if (details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess && issueSolver != null && issueSolver.GetPerkValue(DefaultPerks.Charm.Oratory))
400: GainRenownAction.Apply(issueSolver, TaleWorlds.Library.MathF.Round(DefaultPerks.Charm.Oratory.PrimaryBonus));
401: GainKingdomInfluenceAction.ApplyForDefault(issueSolver, TaleWorlds.Library.MathF.Round(DefaultPerks.Charm.Oratory.PrimaryBonus));
403: if ((details == IssueBase.IssueUpdateDetails.IssueFail || details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess || details == IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal || details == IssueBase.IssueUpdateDetails.IssueTimedOut || details == IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest || details == IssueBase.IssueUpdateDetails.SentTroopsFailedQuest) && issueSolver != null && issue.IssueOwner != null)
405: int num = (issue.IsSolvingWithQuest ? issue.IssueQuest.RelationshipChangeWithQuestGiver : issue.RelationshipChangeWithIssueOwner);
406: if (num > 0)
408: if (issueSolver.GetPerkValue(DefaultPerks.Trade.DistributedGoods) && issue.IssueOwner.IsArtisan)
410: num *= (int)DefaultPerks.Trade.DistributedGoods.PrimaryBonus;
412: if (issueSolver.GetPerkValue(DefaultPerks.Trade.LocalConnection) && issue.IssueOwner.IsMerchant)
414: num *= (int)DefaultPerks.Trade.LocalConnection.PrimaryBonus;
416: ChangeRelationAction.ApplyPlayerRelation(issue.IsSolvingWithQuest ? issue.IssueQuest.QuestGiver : issue.IssueOwner, num);
418: else if (num < 0)
420: ChangeRelationAction.ApplyPlayerRelation(issue.IsSolvingWithQuest ? issue.IssueQuest.QuestGiver : issue.IssueOwner, num);
423: if (details == IssueBase.IssueUpdateDetails.IssueCancel || details == IssueBase.IssueUpdateDetails.IssueFail || details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess || details == IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal || details == IssueBase.IssueUpdateDetails.IssueTimedOut || details == IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest || details == IssueBase.IssueUpdateDetails.SentTroopsFailedQuest || details == IssueBase.IssueUpdateDetails.IssueFinishedByAILord)
425: Campaign.Current.IssueManager.AddIssueCoolDownData(issue.GetType(), new HeroRelatedIssueCoolDownData(issue.IssueOwner, CampaignTime.DaysFromNow(Campaign.Current.Models.IssueModel.IssueOwnerCoolDownInDays)));
```

TaleWorlds.CampaignSystem.CampaignBehaviors.JournalLogsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 69-81
```text
69: private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver)
71: if (issueSolver == Hero.MainHero)
73: JournalLogEntry journalLogEntry = GetRelatedLog(issue);
74: if (journalLogEntry == null)
76: journalLogEntry = CreateRelatedLog(issue);
77: LogEntry.AddLogEntry(journalLogEntry);
79: journalLogEntry.Update(GetEntries(issue), details);
```

## E07. Governors have native effects

TaleWorlds.CampaignSystem.CampaignBehaviors.GovernorCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 54-91
```text
54: private void DailyTickSettlement(Settlement settlement)
56: if ((!settlement.IsTown && !settlement.IsCastle) || settlement.Town.Governor == null)
58: return;
60: Hero governor = settlement.Town.Governor;
61: if (MBRandom.RandomFloat <= DefaultPerks.Charm.InBloom.SecondaryBonus && governor.GetPerkValue(DefaultPerks.Charm.InBloom))
63: Hero randomElementWithPredicate = settlement.Notables.GetRandomElementWithPredicate((Hero x) => x.IsFemale != governor.IsFemale);
64: if (randomElementWithPredicate != null)
66: int relationChange = 1;
67: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(governor.Clan.Leader, randomElementWithPredicate, relationChange);
70: if (MBRandom.RandomFloat <= DefaultPerks.Charm.YoungAndRespectful.SecondaryBonus && governor.GetPerkValue(DefaultPerks.Charm.YoungAndRespectful))
72: Hero randomElementWithPredicate2 = settlement.Notables.GetRandomElementWithPredicate((Hero x) => x.IsFemale == governor.IsFemale);
73: if (randomElementWithPredicate2 != null)
75: int relationChange2 = 1;
76: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(governor.Clan.Leader, randomElementWithPredicate2, relationChange2);
79: if (MBRandom.RandomFloat <= DefaultPerks.Charm.MeaningfulFavors.SecondaryBonus && governor.GetPerkValue(DefaultPerks.Charm.MeaningfulFavors))
81: foreach (Hero notable in settlement.Notables)
83: if (notable.Power >= 200f)
85: int relationChange3 = 1;
86: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(settlement.OwnerClan.Leader, notable, relationChange3);
90: SkillLevelingManager.OnSettlementGoverned(governor, settlement);
```

## E08. NPC governor assignment

TaleWorlds.CampaignSystem.CampaignBehaviors.ClanVariablesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 115-127
```text
115: private void UpdateGovernorsOfClan(Clan clan)
117: List<Tuple<Town, float>> list = new List<Tuple<Town, float>>();
118: foreach (Town fief in clan.Fiefs)
120: float num = 0f;
121: num += (float)((!fief.IsTown) ? 1 : 3);
122: num += TaleWorlds.Library.MathF.Sqrt(fief.Prosperity / 1000f);
123: num += (float)fief.Settlement.BoundVillages.Count;
124: num *= ((clan.Culture == fief.Settlement.Culture) ? 1f : 0.5f);
125: float num2 = (clan.Leader.MapFaction.IsKingdomFaction ? Campaign.Current.Models.MapDistanceModel.GetDistance(fief.Settlement, clan.Leader.MapFaction.FactionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All) : 100f);
126: num *= 1f - TaleWorlds.Library.MathF.Sqrt(num2 / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType.Default));
127: list.Add(new Tuple<Town, float>(fief, num));
```

TaleWorlds.CampaignSystem.CampaignBehaviors.ClanVariablesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 149-173
```text
149: foreach (Hero aliveLord in clan.AliveLords)
151: if (Campaign.Current.Models.ClanPoliticsModel.CanHeroBeGovernor(aliveLord) && aliveLord.PartyBelongedTo == null && aliveLord.Clan != Clan.PlayerClan && !list2.Contains(aliveLord))
153: float num5 = ((tuple.Item1.Governor == aliveLord) ? 1f : 0.75f) * Campaign.Current.Models.DiplomacyModel.GetHeroGoverningStrengthForClan(aliveLord);
154: if (num5 > num4)
156: num4 = num5;
157: hero = aliveLord;
161: if (hero == null)
163: continue;
165: if (tuple.Item1.Governor != hero)
167: if (hero.GovernorOf != null)
169: ChangeGovernorAction.RemoveGovernorOf(hero);
171: ChangeGovernorAction.Apply(tuple.Item1, hero);
173: list2.Add(hero);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.ClanVariablesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 435-440
```text
435: if (clan != Clan.PlayerClan)
437: UpdateGovernorsOfClan(clan);
438: UpdateClanSettlementsPaymentLimit(clan);
439: UpdateClanSettlementAutoRecruitment(clan);
```

## E09. Native project caller, cadence, and commit

TaleWorlds.CampaignSystem.CampaignBehaviors.BuildingsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 40-59
```text
40: private static void DecideDailyProject(Town town)
42: Building nextDailyBuilding = Campaign.Current.Models.BuildingScoreCalculationModel.GetNextDailyBuilding(town);
43: if (nextDailyBuilding != null && nextDailyBuilding != town.CurrentDefaultBuilding)
45: BuildingHelper.ChangeDefaultBuilding(nextDailyBuilding, town);
49: private static void DecideBuildingQueue(Town town)
51: if (town.BuildingsInProgress.IsEmpty())
53: Building nextBuilding = Campaign.Current.Models.BuildingScoreCalculationModel.GetNextBuilding(town);
54: if (nextBuilding != null)
56: town.BuildingsInProgress.Enqueue(nextBuilding);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.BuildingsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 75-89
```text
75: if (town.Owner.Settlement.OwnerClan != Clan.PlayerClan)
77: if (MBRandom.RandomFloat < 0.1f)
79: DecideBuildingQueue(town);
81: if (MBRandom.RandomFloat < 0.01f)
83: DecideDailyProject(town);
86: if (!town.CurrentBuilding.BuildingType.IsDailyProject)
88: TickCurrentBuildingForTown(town);
```

## E10. Exact selected-model interface and default random selection

TaleWorlds.CampaignSystem.ComponentInterfaces.BuildingScoreCalculationModel | TaleWorlds.CampaignSystem.dll | output lines 5-12
```text
5: namespace TaleWorlds.CampaignSystem.ComponentInterfaces;
7: public abstract class BuildingScoreCalculationModel : MBGameModel<BuildingScoreCalculationModel>
9: public abstract Building GetNextBuilding(Town town);
11: public abstract Building GetNextDailyBuilding(Town town);
```

TaleWorlds.CampaignSystem.GameComponents.DefaultBuildingScoreCalculationModel | TaleWorlds.CampaignSystem.dll | output lines 5-20
```text
5: using TaleWorlds.LinQuick;
7: namespace TaleWorlds.CampaignSystem.GameComponents;
9: public class DefaultBuildingScoreCalculationModel : BuildingScoreCalculationModel
11: public override Building GetNextDailyBuilding(Town town)
13: return town.Buildings.GetRandomElementWithPredicate((Building b) => b.BuildingType.IsDailyProject);
16: public override Building GetNextBuilding(Town town)
18: return town.Buildings.WhereQ((Building x) => !x.BuildingType.IsDailyProject && x.CurrentLevel < 3 && !town.BuildingsInProgress.Contains(x)).GetRandomElementInefficiently();
```

## E11. Existing daily project and native low-loyalty threshold

TaleWorlds.CampaignSystem.Settlements.Buildings.DefaultBuildingTypes | TaleWorlds.CampaignSystem.dll | output lines 297-313
```text
297: _buildingSettlementDailyHousing.InitializeDailyProject(new TextObject("{=F4V7oaVx}Housing"), new TextObject("{=yWXtcxqb}Construct housing so that more folks can settle, increasing population."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
299: new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Prosperity, BuildingEffectIncrementType.Add, 2f, 2f, 2f)
300: });
301: _buildingSettlementDailyTrainMilitia.InitializeDailyProject(new TextObject("{=p1Y3EU5O}Train Militia"), new TextObject("{=61J1wa6k}Schedule drills for commoners, increasing militia recruitment and auto recruitment."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
303: new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Militia, BuildingEffectIncrementType.Add, 2f, 2f, 2f),
304: new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.GarrisonAutoRecruitment, BuildingEffectIncrementType.Add, 1f, 1f, 1f)
305: });
306: _buildingSettlementDailyFestivalAndGames.InitializeDailyProject(new TextObject("{=aEmYZadz}Festival and Games"), new TextObject("{=ovDbQIo9}Organize festivals and games in the settlement, increasing loyalty."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
308: new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.Loyalty, BuildingEffectIncrementType.Add, 3f, 3f, 3f)
309: });
310: _buildingSettlementDailyIrrigation.InitializeDailyProject(new TextObject("{=O4cknzhW}Irrigation"), new TextObject("{=CU9g49fo}Provide irrigation, increasing hearth growth in bound villages."), new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[1]
312: new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.VillageHeartsPerDay, BuildingEffectIncrementType.Add, 1f, 1f, 1f)
313: });
```

TaleWorlds.CampaignSystem.GameComponents.DefaultSettlementLoyaltyModel | TaleWorlds.CampaignSystem.dll | output lines 58-79
```text
58: public override int RebellionStartLoyaltyThreshold
60: get
62: if (!Campaign.Current.Options.IsHighRebellionEnabled)
64: return 15;
66: return 50;
70: public override int RebelliousStateStartLoyaltyThreshold
72: get
74: if (!Campaign.Current.Options.IsHighRebellionEnabled)
76: return 25;
78: return 60;
```

## E12. Queue gate, native saved flag, native effects

TaleWorlds.CampaignSystem.Settlements.Town | TaleWorlds.CampaignSystem.dll | output lines 216-237
```text
216: public Building CurrentBuilding
218: get
220: if (!BuildingsInProgress.IsEmpty())
222: return BuildingsInProgress.Peek();
224: return CurrentDefaultBuilding;
228: public Building CurrentDefaultBuilding
230: get
232: if (!BuildingsInProgress.IsEmpty())
234: return null;
236: return Buildings.FirstOrDefaultQ((Building k) => k.IsCurrentlyDefault);
```

Helpers.BuildingHelper | TaleWorlds.CampaignSystem.dll | output lines 29-42
```text
29: public static void ChangeDefaultBuilding(Building newDefault, Town town)
31: foreach (Building building in town.Buildings)
33: if (building.IsCurrentlyDefault)
35: building.IsCurrentlyDefault = false;
37: if (building == newDefault)
39: building.IsCurrentlyDefault = true;
```

TaleWorlds.CampaignSystem.Settlements.Buildings.Building | TaleWorlds.CampaignSystem.dll | output lines 20-24
```text
20: [SaveableField(2)]
21: public bool IsCurrentlyDefault;
23: [SaveableField(3)]
24: private int _currentLevel;
```

TaleWorlds.CampaignSystem.Settlements.Buildings.Building | TaleWorlds.CampaignSystem.dll | output lines 167-178
```text
167: if (_currentLevel != 0 && (!BuildingType.IsDailyProject || Town.CurrentDefaultBuilding == this) && BuildingType.HasEffect(buildingEffect))
169: BuildingEffectIncrementType buildingEffectType = BuildingType.GetBuildingEffectType(buildingEffect);
170: float resultNumber = Campaign.Current.Models.BuildingEffectModel.GetBuildingEffect(this, buildingEffect).ResultNumber;
171: switch (buildingEffectType)
173: case BuildingEffectIncrementType.Add:
174: result.Add(resultNumber, Name);
175: break;
176: case BuildingEffectIncrementType.AddFactor:
177: result.AddFactor(resultNumber, Name);
178: break;
```

## E13. Native loyalty calculation remains authoritative

TaleWorlds.CampaignSystem.GameComponents.DefaultSettlementLoyaltyModel | TaleWorlds.CampaignSystem.dll | output lines 119-135
```text
119: private ExplainedNumber CalculateLoyaltyChangeInternal(Town town, bool includeDescriptions = false)
121: ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
122: GetSettlementLoyaltyChangeDueToFoodStocks(town, ref explainedNumber);
123: GetSettlementLoyaltyChangeDueToOwnerCulture(town, ref explainedNumber);
124: GetSettlementLoyaltyChangeDueToPolicies(town, ref explainedNumber);
125: GetSettlementLoyaltyChangeDueToProjects(town, ref explainedNumber);
126: GetSettlementLoyaltyChangeDueToIssues(town, ref explainedNumber);
127: GetSettlementLoyaltyChangeDueToSecurity(town, ref explainedNumber);
128: GetSettlementLoyaltyChangeDueToNotableRelations(town, ref explainedNumber);
129: GetSettlementLoyaltyChangeDueToGovernorPerks(town, ref explainedNumber);
130: GetSettlementLoyaltyChangeDueToLoyaltyDrift(town, ref explainedNumber);
131: if (town.Governor != null && town.Governor.CurrentSettlement?.Town == town && explainedNumber.ResultNumber > 0f)
133: TraitEffectHelper.ApplyTraitEffect(town.Governor, DefaultPersonalityTraitEffects.HonorLoyaltyGainEffect, ref explainedNumber);
135: return explainedNumber;
```

TaleWorlds.CampaignSystem.GameComponents.DefaultSettlementLoyaltyModel | TaleWorlds.CampaignSystem.dll | output lines 287-299
```text
287: private void GetSettlementLoyaltyChangeDueToProjects(Town town, ref ExplainedNumber explainedNumber)
289: town.AddEffectOfBuildings(BuildingEffectEnum.Loyalty, ref explainedNumber);
292: private void GetSettlementLoyaltyChangeDueToIssues(Town town, ref ExplainedNumber explainedNumber)
294: Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementLoyalty, town.Settlement, ref explainedNumber);
297: private void GetSettlementLoyaltyChangeDueToLoyaltyDrift(Town town, ref ExplainedNumber explainedNumber)
299: explainedNumber.Add(-0.1f * (town.Loyalty - (float)LoyaltyDriftMedium), LoyaltyDriftText);
```

TaleWorlds.CampaignSystem.GameComponents.DefaultBuildingEffectModel | TaleWorlds.CampaignSystem.dll | output lines 28-37
```text
28: PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.Contractors, building.Town, isPrimaryBonus: false, ref bonuses);
29: if (building.BuildingType.IsDailyProject)
31: PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.MasterOfPlanning, building.Town, isPrimaryBonus: false, ref bonuses);
33: if (building.BuildingType == DefaultBuildingTypes.SettlementMarketplace || building.BuildingType == DefaultBuildingTypes.SettlementDailyFestivalAndGames)
35: PerkHelper.AddPerkBonusForTown(DefaultPerks.Charm.PublicSpeaker, building.Town, isPrimaryBonus: false, ref bonuses);
37: return bonuses;
```

## E14. Notable power is a living model

TaleWorlds.CampaignSystem.GameComponents.DefaultNotablePowerModel | TaleWorlds.CampaignSystem.dll | output lines 88-107
```text
88: private void CalculateDailyPowerChangePerPropertyOwned(Hero hero, ref ExplainedNumber explainedNumber)
90: int count = hero.OwnedAlleys.Count;
91: explainedNumber.Add(0.1f * (float)count, _propertyEffect);
94: private void CalculateDailyPowerChangeForAffiliationWithRulerClan(ref ExplainedNumber explainedNumber)
96: explainedNumber.Add(0.2f, _rulerClanEffect);
99: private void CalculateDailyPowerChangeForInfluentialNotables(Hero hero, ref ExplainedNumber explainedNumber)
101: float value = -1f * ((hero.Power - (float)RegularNotableMaxPowerLevel) / 500f);
102: explainedNumber.Add(value, _currentRankEffect);
105: private void CalculatePowerChangeFromIssues(Hero hero, ref ExplainedNumber explainedNumber)
107: Campaign.Current.Models.IssueModel.GetIssueEffectOfHero(DefaultIssueEffects.IssueOwnerPower, hero, ref explainedNumber);
```

## E15. Existing persistent local relations

TaleWorlds.CampaignSystem.CampaignBehaviors.CharacterRelationCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 137-140
```text
137: if (mapEvent.EventType == MapEvent.BattleTypes.Raid && winnerSide.MissionSide == BattleSideEnum.Defender && mapEvent.MapEventSettlement.Notables.Count > 0)
139: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mapEvent.MapEventSettlement.Notables.GetRandomElement(), party2.LeaderHero, 5);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CharacterRelationCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 352-375
```text
352: if (item2.Town.Security >= (float)settlementSecurityModel.ThresholdForNotableRelationBonus)
354: foreach (Hero notable in item2.Notables)
356: if ((notable.IsArtisan || notable.IsMerchant) && MBRandom.RandomFloat < 0.05f)
358: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item2.OwnerClan.Leader, notable, settlementSecurityModel.DailyNotableRelationBonus, showQuickNotification: false);
359: flag2 = flag2 || item2.OwnerClan.Leader.IsHumanPlayerCharacter;
363: else
365: if (!(item2.Town.Security < (float)settlementSecurityModel.ThresholdForNotableRelationPenalty))
367: continue;
369: foreach (Hero notable2 in item2.Notables)
371: if ((notable2.IsArtisan || notable2.IsMerchant) && MBRandom.RandomFloat < 0.05f)
373: notable2.AddPower(settlementSecurityModel.DailyNotablePowerPenalty);
374: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item2.OwnerClan.Leader, notable2, settlementSecurityModel.DailyNotableRelationPenalty, showQuickNotification: false);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CharacterRelationCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 388-399
```text
388: if (!item2.IsVillage || !(item2.Village.Bound.Town.Loyalty >= settlementLoyaltyModel.ThresholdForNotableRelationBonus))
390: continue;
392: foreach (Hero notable4 in item2.Notables)
394: if ((notable4.IsHeadman || notable4.IsRuralNotable) && MBRandom.RandomFloat < 0.05f)
396: ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item2.OwnerClan.Leader, notable4, settlementLoyaltyModel.DailyNotableRelationBonus, showQuickNotification: false);
397: flag = flag || item2.OwnerClan.Leader.IsHumanPlayerCharacter;
```

## E16. Workshop production and bankruptcy

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 279-289
```text
279: private void DailyTickTown(Town town)
281: Workshop[] workshops = town.Workshops;
282: foreach (Workshop workshop in workshops)
284: if (!town.InRebelliousState)
286: RunTownWorkshop(town, workshop);
288: HandleDailyExpense(workshop);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 749-769
```text
749: private bool TickOneProductionCycleForNotableWorkshop(WorkshopType.Production production, Workshop workshop, bool effectCapital)
751: Town town = workshop.Settlement.Town;
752: int inputMaterialCost = 0;
753: if (!DetermineItemRosterHasSufficientInputs(production, town.Owner.ItemRoster, town, out inputMaterialCost))
755: return false;
757: List<EquipmentElement> itemsToProduce = GetItemsToProduce(production, workshop, out var income);
758: if (CanNotableWorkshopProduceThisCycle(production, workshop, inputMaterialCost, income, effectCapital))
760: foreach (var input in production.Inputs)
762: ConsumeInputFromTownMarket(input.Item1, input.Item2, town, workshop, effectCapital);
764: foreach (EquipmentElement item in itemsToProduce)
766: ProduceAnOutputToTown(item, workshop, effectCapital);
767: CampaignEventDispatcher.Instance.OnItemProduced(item.Item, workshop.Settlement, 1);
769: return true;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 792-802
```text
792: private void HandleNotableWorkshopExpense(Workshop shop)
794: int expense = shop.Expense;
795: if (shop.Capital >= expense)
797: shop.ChangeGold(-expense);
799: else
801: ChangeWorkshopOwnerByBankruptcy(shop);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 1091-1097
```text
1091: private void ChangeWorkshopOwnerByBankruptcy(Workshop workshop)
1093: int costForNotable = Campaign.Current.Models.WorkshopModel.GetCostForNotable(workshop);
1094: Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
1095: WorkshopType workshopType = DecideBestWorkshopType(workshop.Settlement, atGameStart: false, workshop.WorkshopType);
1096: ChangeOwnerOfWorkshopAction.ApplyByBankruptcy(workshop, notableOwnerForWorkshop, workshopType, costForNotable);
```

## E17. NPC workshop type choice is input-sensitive, weighted, not optimal planning

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 1276-1284
```text
1276: private WorkshopType DecideBestWorkshopType(Settlement currentSettlement, bool atGameStart, WorkshopType workshopToExclude = null)
1278: IDictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
1279: foreach (Village item2 in Village.All.Where((Village x) => x.TradeBound == currentSettlement))
1281: foreach (var production in item2.VillageType.Productions)
1283: ItemCategory itemCategory = production.Item1.ItemCategory;
1284: if (itemCategory != DefaultItemCategories.Grain || item2.VillageType.PrimaryProduction == DefaultItems.Grain)
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 1306-1329
```text
1306: Dictionary<WorkshopType, float> dictionary2 = new Dictionary<WorkshopType, float>();
1307: float num = 0f;
1308: foreach (WorkshopType item3 in WorkshopType.All)
1310: if (!item3.IsHidden && (workshopToExclude == null || workshopToExclude != item3))
1312: float num2 = FindTotalInputDensityScore(currentSettlement, item3, dictionary, atGameStart);
1313: dictionary2.Add(item3, num2);
1314: num += num2;
1317: float num3 = num * MBRandom.RandomFloat;
1318: WorkshopType workshopType = null;
1319: foreach (WorkshopType item4 in WorkshopType.All)
1321: if (!item4.IsHidden && (workshopToExclude == null || workshopToExclude != item4))
1323: num3 -= dictionary2[item4];
1324: if (num3 < 0f)
1326: workshopType = item4;
1327: break;
```

## E18. Player workshop controls and war transfer

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCharactersCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 161-175
```text
161: private void workshop_notable_owner_player_buys_workshop_on_consequence()
163: ChangeOwnerOfWorkshopAction.ApplyByPlayerBuying(_lastSelectedWorkshop);
166: private bool workshop_notable_owner_player_buys_workshop_on_clickable_condition(out TextObject explanation)
168: return can_player_buy_workshop_clickable_condition(_lastSelectedWorkshop, out explanation);
171: private bool can_player_buy_workshop_clickable_condition(Workshop workshop, out TextObject explanation)
173: bool flag = Hero.MainHero.Gold < Campaign.Current.Models.WorkshopModel.GetCostForPlayer(workshop);
174: bool num = Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(Clan.PlayerClan.Tier) <= Hero.MainHero.OwnedWorkshops.Count;
175: bool result = false;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCharactersCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 409-417
```text
410: private void conversation_shopworker_player_sell_workshop_on_consequence()
412: Workshop workshop = FindCurrentWorkshop();
413: if (workshop.Owner == Hero.MainHero)
415: Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
416: ChangeOwnerOfWorkshopAction.ApplyByPlayerSelling(workshop, notableOwnerForWorkshop, workshop.WorkshopType);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 310-313
```text
310: private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
312: TransferPlayerWorkshopsIfNeeded();
```

TaleWorlds.CampaignSystem.CampaignBehaviors.WorkshopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 337-347
```text
337: protected void InitializeGameMenus(CampaignGameStarter campaignGameStarter)
339: campaignGameStarter.AddGameMenuOption("town", "manage_warehouse", "{=LK4kNZkb}Enter the warehouse", warehouse_manage_on_condition, warehouse_manage_on_consequence, isLeave: false, 7);
340: campaignGameStarter.AddPlayerLine("workshop_worker_manage_warehouse", "player_options", "warehouse", "{=mBnoWa8R}I would like to access the Warehouse.", null, null);
341: campaignGameStarter.AddDialogLine("workshop_worker_manage_warehouse_answer", "warehouse", "player_options", "{=Y4LhmAdi}Sure, boss. Go ahead.", null, warehouse_manage_on_consequence);
344: private void warehouse_manage_on_consequence()
346: InventoryLogic.CapacityData otherSideCapacity = new InventoryLogic.CapacityData(CapacityDelegate, CapacityExceededWarningDelegate, CapacityExceededHintDelegate);
347: InventoryScreenHelper.OpenScreenAsWarehouse(GetWarehouseRoster(Settlement.CurrentSettlement), otherSideCapacity);
```

## E19. Villager trade and raid response

TaleWorlds.CampaignSystem.CampaignBehaviors.VillagerCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 61-70
```text
61: public override void RegisterEvents()
63: CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, HourlyTickSettlement);
64: CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
65: CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
66: CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
67: CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
68: CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
69: CampaignEvents.OnLootDistributedToPartyEvent.AddNonSerializedListener(this, OnLootDistributedToParty);
70: CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.VillagerCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 284-288
```text
284: if (villagerParty.DefaultBehavior == AiBehavior.GoToSettlement && villagerParty.TargetSettlement == villagerParty.HomeSettlement && villagerParty.HomeSettlement.IsUnderRaid && ((villagerParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(villagerParty.CurrentSettlement, villagerParty.HomeSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) : Campaign.Current.Models.MapDistanceModel.GetDistance(villagerParty, villagerParty.HomeSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out var _)) < Campaign.Current.EstimatedAverageVillagerPartySpeed * 2.5f)
286: villagerParty.SetMoveModeHold();
287: flag = false;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.VillagerCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 324-347
```text
324: private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
326: if (mobileParty != null && mobileParty.IsActive && mobileParty.IsVillager)
328: if (settlement.IsTown)
330: SellGoodsForTradeAction.ApplyByVillagerTrade(settlement, mobileParty);
332: if (settlement.IsVillage && mobileParty.PartyTradeGold != 0)
334: int num = Campaign.Current.Models.SettlementTaxModel.CalculateVillageTaxFromIncome(mobileParty.HomeSettlement.Village, mobileParty.PartyTradeGold);
335: mobileParty.PartyTradeGold = 0;
336: mobileParty.HomeSettlement.Village.TradeTaxAccumulated += num;
338: if (settlement.IsTown && settlement.Town.Governor != null && settlement.Town.Governor.GetPerkValue(DefaultPerks.Trade.TravelingRumors))
340: int num2 = MathF.Round(DefaultPerks.Trade.TravelingRumors.SecondaryBonus);
341: settlement.Town.TradeTaxAccumulated += num2;
346: private void SetPlayerInteraction(MobileParty mobileParty, PlayerInteraction interaction)
```

## E20. Native production and recovery

TaleWorlds.CampaignSystem.CampaignBehaviors.VillageGoodProductionCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 138-168
```text
138: private void TickProductions(Settlement settlement, bool initialProductionForTowns = false)
140: Village village = settlement.Village;
141: if (village != null && !village.IsDeserted)
143: int num = 0;
144: for (int i = 0; i < village.Owner.ItemRoster.Count; i++)
146: num += village.Owner.ItemRoster[i].Amount;
148: int warehouseCapacity = village.GetWarehouseCapacity();
149: if ((float)num < (float)warehouseCapacity * 1.5f)
151: TickGoodProduction(village, initialProductionForTowns);
152: TickFoodProduction(village, initialProductionForTowns);
157: private void TickGoodProduction(Village village, bool initialProductionForTowns)
159: foreach (var production in village.VillageType.Productions)
161: ItemObject item = production.Item1;
162: int num = MBRandom.RoundRandomized(Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(village, production.Item1).ResultNumber);
163: if (num > 0)
165: if (!initialProductionForTowns)
167: village.Owner.ItemRoster.AddToCounts(item, num);
168: CampaignEventDispatcher.Instance.OnItemProduced(item, village.Owner.Settlement, num);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.VillageHealCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 16-31
```text
16: private void DailyTickSettlement(Settlement settlement)
18: if ((settlement.IsVillage || settlement.IsTown) && settlement.SettlementHitPoints < 1f && settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null)
20: float num = (7000f - MathF.Min(7000f, MathF.Max(1000f, settlement.MapFaction.CurrentTotalStrength))) / 100000f;
21: ExplainedNumber bonuses = new ExplainedNumber(0.06f + num);
22: if (settlement.IsVillage && settlement.Village.TradeBound != null)
24: PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.CleanInfrastructure, settlement.Village.TradeBound.Town, isPrimaryBonus: false, ref bonuses);
26: if (settlement.OwnerClan.Leader.GetPerkValue(DefaultPerks.Roguery.InBestLight))
28: bonuses.AddFactor(DefaultPerks.Roguery.InBestLight.SecondaryBonus, DefaultPerks.Roguery.InBestLight.Name);
30: IncreaseSettlementHealthAction.Apply(settlement, bonuses.ResultNumber);
```

## E21. Militia is separate from volunteers

TaleWorlds.CampaignSystem.GameComponents.DefaultSettlementMilitiaModel | TaleWorlds.CampaignSystem.dll | output lines 82-114
```text
82: private static ExplainedNumber CalculateMilitiaChangeInternal(Settlement settlement, bool includeDescriptions = false)
84: ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
85: if (settlement.IsVillage && settlement.Village.VillageState != Village.VillageStates.Normal)
87: return result;
89: float militia = settlement.Militia;
90: if (settlement.IsFortification)
92: result.Add(2f, BaseText);
94: else if (settlement.IsVillage)
96: result.Add(0.5f, BaseText);
98: float value = (0f - militia) * 0.025f;
99: result.Add(value, RetiredText);
100: if (settlement.IsVillage)
102: float value2 = settlement.Village.Hearth / 400f;
103: result.Add(value2, FromHearthsText);
105: else if (settlement.IsFortification)
107: float num = settlement.Town.Prosperity / 1000f;
108: result.Add(num, FromProsperityText);
109: if (settlement.Town.InRebelliousState)
111: float num2 = MBMath.Map(settlement.Town.Loyalty, 0f, Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold, Campaign.Current.Models.SettlementLoyaltyModel.MilitiaBoostPercentage, 0f);
112: float value3 = MathF.Abs(num * (num2 * 0.01f));
113: result.Add(value3, LowLoyaltyText);
```

TaleWorlds.CampaignSystem.GameComponents.DefaultSettlementMilitiaModel | TaleWorlds.CampaignSystem.dll | output lines 176-179
```text
176: private static void GetSettlementMilitiaChangeDueToIssues(Settlement settlement, ref ExplainedNumber result)
178: Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementMilitia, settlement, ref result);
```

## E22. Garrison volunteer pool AND separate basic-troop path

TaleWorlds.CampaignSystem.CampaignBehaviors.GarrisonRecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 53-71
```text
53: private void OnDailySettlementTick(Settlement settlement)
55: if (!settlement.IsFortification)
57: return;
59: Town town = settlement.Town;
60: if (settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null)
62: TickGarrisonChangeForTown(town);
63: if (CanSettlementAutoRecruit(settlement))
65: TickAutoRecruitmentGarrisonChange(town);
68: if (town.GarrisonParty != null)
70: HandleGarrisonXpChange(town);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.GarrisonRecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 83-91
```text
83: for (int i = 0; (float)i < resultNumber; i++)
85: VolunteerTroop volunteerTroop = _volunteerListCache.ElementAt(i);
86: Hero ownerNotable = volunteerTroop.OwnerNotable;
87: int notableVolunteerArrayIndex = volunteerTroop.NotableVolunteerArrayIndex;
88: town.GarrisonParty.MemberRoster.AddToCounts(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex], 1);
89: town.Settlement.OwnerClan.AutoRecruitmentExpenses += Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex], town.Settlement.OwnerClan.Leader).RoundedResultNumber;
90: ownerNotable.VolunteerTypes[notableVolunteerArrayIndex] = null;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.GarrisonRecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 95-105
```text
95: private void TickGarrisonChangeForTown(Town town)
97: int num = (int)GetBaseGarrisonChangeExplainedNumber(town, includeDescriptions: false).ResultNumber;
98: if (num > 0)
100: if (town.GarrisonParty == null)
102: town.Owner.Settlement.AddGarrisonParty();
104: town.GarrisonParty.MemberRoster.AddToCounts(GetBasicTroopForTown(town), num);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.GarrisonRecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 133-155
```text
133: foreach (Village boundVillage in town.Settlement.BoundVillages)
135: if (boundVillage.VillageState != Village.VillageStates.Normal)
137: continue;
139: foreach (Hero notable2 in boundVillage.Settlement.Notables)
141: if (notable2.IsAlive)
143: list.Add(notable2);
147: foreach (Hero item2 in list)
149: int num = Campaign.Current.Models.VolunteerModel.MaximumIndexGarrisonCanRecruitFromHero(town.Settlement, item2);
150: for (int i = 0; i < num; i++)
152: if (item2.VolunteerTypes[i] != null)
154: VolunteerTroop item = new VolunteerTroop(item2, i);
155: _volunteerListCache.Add(item);
```

## E23. Native garrison transfers and exclusions

TaleWorlds.CampaignSystem.CampaignBehaviors.GarrisonTroopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 248-265
```text
248: private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
250: if (!Campaign.Current.GameStarted || mobileParty == null || !mobileParty.IsLordParty || mobileParty.IsDisbanding || mobileParty.LeaderHero == null || !settlement.IsFortification || !DiplomacyHelper.IsSameFactionAndNotEliminated(mobileParty.MapFaction, settlement.MapFaction) || (settlement.OwnerClan == Clan.PlayerClan && settlement != _newlyConqueredFortification))
252: return;
254: if (mobileParty.Army != null)
256: if (mobileParty.Army.LeaderParty == mobileParty)
258: ManageGarrisonForArmy(mobileParty, settlement);
261: else if (!mobileParty.IsMainParty)
263: ManageGarrisonForParty(mobileParty, settlement);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.GarrisonTroopsCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 388-409
```text
388: if ((settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.IsWageLimitExceeded()) || (mobileParty.LeaderHero.Clan == Clan.PlayerClan && _newlyConqueredFortification == null) || !mobileParty.LeaderHero.CanDonateTroopsToGarrison)
390: partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison = false;
394: private void TryToLeaveTroopsToGarrisonForParty(in PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
396: int numberOfTroopsToLeaveForParty = partyGarrisonTransferDataArgs.GetNumberOfTroopsToLeaveForParty();
397: if (numberOfTroopsToLeaveForParty > 0)
399: LeaveTroopsToGarrison(partyGarrisonTransferDataArgs.MobileParty, partyGarrisonTransferDataArgs.Settlement, numberOfTroopsToLeaveForParty, archersAreHighPriority: true);
403: private void TryToTakeTroopsFromGarrisonForParty(in PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
405: int numberOfTroopsToTakeForParty = partyGarrisonTransferDataArgs.GetNumberOfTroopsToTakeForParty();
406: if (numberOfTroopsToTakeForParty > 0)
408: TakeTroopsFromGarrison(partyGarrisonTransferDataArgs.MobileParty, partyGarrisonTransferDataArgs.Settlement, numberOfTroopsToTakeForParty, archersAreHighPriority: false);
```

## E24. Shared native volunteer production and access

TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 238-257
```text
238: for (int i = 0; i < 6; i++)
240: if (!(MBRandom.RandomFloat < Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(notable, i, settlement)))
242: continue;
244: CharacterObject characterObject = notable.VolunteerTypes[i];
245: if (characterObject == null)
247: notable.VolunteerTypes[i] = basicVolunteer;
248: flag = true;
250: else if (characterObject.UpgradeTargets.Length != 0 && characterObject.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier)
252: float num = MathF.Log(notable.Power / (float)characterObject.Tier, 2f) * 0.01f;
253: if (MBRandom.RandomFloat < num)
255: notable.VolunteerTypes[i] = characterObject.UpgradeTargets[MBRandom.RandomInt(characterObject.UpgradeTargets.Length)];
256: flag = true;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 530-539
```text
530: int num = notable.VolunteerTypes.FindIndexQ((CharacterObject x) => x != null);
531: if (num < 0)
533: continue;
535: int num2 = MBRandom.RandomInt(6);
536: int num3 = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(mobileParty.IsGarrison ? mobileParty.Party.Owner : mobileParty.LeaderHero, notable);
537: if (num > num3)
539: continue;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 665-668
```text
665: private void GetRecruitVolunteerFromIndividual(MobileParty side1Party, CharacterObject subject, Hero individual, int bitCode)
667: ApplyInternal(side1Party, individual.CurrentSettlement, individual, subject, 1, bitCode, RecruitingDetail.VolunteerFromIndividual);
```

## E25. NPC prisoner selling and ransom

TaleWorlds.CampaignSystem.CampaignBehaviors.PartiesSellPrisonerCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 24-39
```text
24: private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
26: if (mobileParty == null || mobileParty.IsMainParty || !settlement.IsFortification || mobileParty.MapFaction == null || mobileParty.IsDisbanding || mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) || (mobileParty.PrisonRoster.TotalRegulars <= 0 && (mobileParty.PrisonRoster.TotalHeroes <= 0 || !mobileParty.PrisonRoster.GetTroopRoster().Exists((TroopRosterElement x) => x.Character != CharacterObject.PlayerCharacter && x.Character.HeroObject.MapFaction.IsAtWarWith(settlement.MapFaction)))))
28: return;
30: TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
31: foreach (TroopRosterElement item in mobileParty.PrisonRoster.GetTroopRoster())
33: if (!item.Character.IsHero || (!item.Character.IsPlayerCharacter && item.Character.HeroObject.MapFaction.IsAtWarWith(settlement.MapFaction) && (!item.Character.HeroObject.Clan.HasBloodFeudWithPlayer || mobileParty.MapFaction == settlement.MapFaction)))
35: troopRoster.Add(item);
38: SellPrisonersAction.ApplyForSelectedPrisoners(mobileParty.Party, settlement.Party, troopRoster);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.RansomOfferCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 64-69
```text
64: private void DailyTickHero(Hero hero)
66: if (hero.IsPrisoner && hero.Clan != null && hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner.MapFaction != null && !hero.PartyBelongedToAsPrisoner.MapFaction.IsBanditFaction && hero != Hero.MainHero && hero.Clan.AliveLords.Count > 1 && hero.MapFaction != null)
68: ConsiderRansomPrisoner(hero);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.RansomOfferCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 110-115
```text
110: SetPrisonerFreeBarterable setPrisonerFreeBarterable = new SetPrisonerFreeBarterable(hero, captorClanOfPrisoner.Leader, hero.PartyBelongedToAsPrisoner, hero2);
111: if (setPrisonerFreeBarterable.GetValueForFaction(captorClanOfPrisoner) + setPrisonerFreeBarterable.GetValueForFaction(hero.Clan) > 0)
113: Campaign.Current.BarterManager.ExecuteAiBarter(captorClanOfPrisoner, hero.Clan, captorClanOfPrisoner.Leader, hero2, setPrisonerFreeBarterable);
```

## E26. AI prisoner recruitment bypasses volunteer slots

TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitPrisonersCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 49-54
```text
49: private void DailyTickAIMobileParty(MobileParty mobileParty)
51: if (mobileParty.IsMainParty || !mobileParty.IsLordParty || mobileParty.MapEvent != null)
53: return;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitPrisonersCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 77-88
```text
77: if (Campaign.Current.Models.PrisonerRecruitmentCalculationModel.ShouldPartyRecruitPrisoners(mobileParty.Party))
79: if (IsPrisonerRecruitable(mobileParty, characterObject, out var conformityNeeded))
81: int num3 = mobileParty.Party.PartySizeLimit - mobileParty.MemberRoster.TotalManCount;
82: int a = MathF.Min((num3 > 0) ? ((num3 > num2) ? num2 : num3) : 0, prisonRoster.GetElementNumber(characterObject));
83: int characterWage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(characterObject);
84: a = MathF.Min(a, mobileParty.GetAvailableWageBudget() / characterWage);
85: if (a > 0)
87: RecruitPrisonersAi(mobileParty, characterObject, a, conformityNeeded);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitPrisonersCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 111-118
```text
111: private void RecruitPrisonersAi(MobileParty mobileParty, CharacterObject troop, int num, int conformityCost)
113: mobileParty.PrisonRoster.GetElementNumber(troop);
114: mobileParty.PrisonRoster.GetElementXp(troop);
115: mobileParty.PrisonRoster.AddToCounts(troop, -num, insertAtFront: false, 0, -conformityCost * num);
116: mobileParty.MemberRoster.AddToCounts(troop, num);
117: CampaignEventDispatcher.Instance.OnTroopRecruited(mobileParty.LeaderHero, null, null, troop, num);
118: ApplyPrisonerRecruitmentEffects(mobileParty, troop, num);
```

## E27. Escape and release infrastructure

TaleWorlds.CampaignSystem.CampaignBehaviors.PrisonerReleaseCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 16-23
```text
17: CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
18: CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyHeroTick);
19: CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyTick);
20: CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeaceEvent);
21: CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, ClanChangedKingdom);
22: CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.PrisonerReleaseCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 244-249
```text
246: if (MBRandom.RandomFloat < bonuses.ResultNumber)
248: EndCaptivityAction.ApplyByEscape(hero);
```

## E28. Execution is not a universally symmetric NPC policy

TaleWorlds.CampaignSystem.CampaignBehaviors.ExecutionCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 274-299
```text
274: private void OnHeroTakenPrisoner(PartyBase capturer, Hero prisoner)
276: if (capturer == null || capturer.LeaderHero == null)
278: return;
280: if (prisoner == Hero.MainHero)
282: if (MBRandom.RandomFloat <= CalculatePlayerExecutionProbability(capturer.LeaderHero))
284: capturer.LeaderHero.SetHasMet();
285: _isMainHeroExecuted = true;
288: else if (capturer.LeaderHero.Clan == Clan.PlayerClan)
290: if (capturer.LeaderHero != Hero.MainHero && prisoner.Clan.HasBloodFeudWithPlayer)
292: _heroesPendingMapEventEndToBeExecuted.Add(prisoner);
295: else if (prisoner.Clan == Clan.PlayerClan && capturer.LeaderHero.Clan.HasBloodFeudWithPlayer)
297: ShowClanMemberCapturedByFeudedClanNotification(prisoner, capturer.LeaderHero.Clan);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.ExecutionCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 320-338
```text
320: private void DailyTickHero(Hero hero)
322: if (_pendingClanMemberSettlementExecutions.ContainsKey(hero))
324: (Clan, CampaignTime) tuple = _pendingClanMemberSettlementExecutions[hero];
325: if (tuple.Item2.IsPast)
327: Hero leader = tuple.Item1.Leader;
328: KillCharacterAction.ApplyByExecution(hero, leader);
331: else if (hero != Hero.MainHero && hero.Clan == Clan.PlayerClan && hero.PartyBelongedToAsPrisoner != null)
333: Hero hero2 = (hero.PartyBelongedToAsPrisoner.IsMobile ? hero.PartyBelongedToAsPrisoner.LeaderHero : hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan.Leader);
334: if (hero2 != null && MBRandom.RandomFloat <= CalculatePlayerClanMemberExecutionProbability(hero, hero2))
336: KillCharacterAction.ApplyByExecution(hero, hero2);
```

## E29. NPC alley turnover and player consequences

SandBox.CampaignBehaviors.AlleyCampaignBehavior | SandBox.dll | output lines 372-395
```text
372: private void TickAlleyOwnerships(Settlement settlement)
374: foreach (Hero notable in settlement.Notables)
376: if (!notable.IsGangLeader)
378: continue;
380: int count = notable.OwnedAlleys.Count;
381: float num = 0.02f - (float)count * 0.005f;
382: float num2 = (float)count * 0.005f;
383: if (MBRandom.RandomFloat < num)
385: settlement.Alleys.FirstOrDefault((Alley x) => x.State == Alley.AreaState.Empty)?.SetOwner(notable);
387: if (MBRandom.RandomFloat < num2 && !_playerOwnedCommonAreaData.Any((PlayerAlleyData x) => x.Alley.Settlement == settlement && x.UnderAttackBy?.Owner == notable))
389: notable.OwnedAlleys.GetRandomElement()?.SetOwner(null);
391: if (!notable.IsHealthFull())
393: notable.Heal(10);
```

SandBox.CampaignBehaviors.AlleyCampaignBehavior | SandBox.dll | output lines 421-436
```text
421: private void OnAlleyClearedByPlayer(Alley alley)
423: ChangeRelationAction.ApplyPlayerRelation(alley.Owner, -5);
424: foreach (Hero notable in alley.Settlement.Notables)
426: if (!notable.IsGangLeader)
428: ChangeRelationAction.ApplyPlayerRelation(notable, 1);
431: PlayerAlleyData playerAlleyData = _playerOwnedCommonAreaData.FirstOrDefault((PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
432: if (playerAlleyData?.UnderAttackBy == alley)
434: playerAlleyData.UnderAttackBy = null;
436: alley.SetOwner(null);
```

TaleWorlds.CampaignSystem.GameComponents.DefaultAlleyModel | TaleWorlds.CampaignSystem.dll | output lines 304-311
```text
304: if (hero.GetSkillValue(DefaultSkills.Roguery) < 30)
306: return AlleyMemberAvailabilityDetail.NotEnoughRoguerySkill;
308: if (hero.GetTraitLevel(DefaultTraits.Mercy) > 0)
310: return AlleyMemberAvailabilityDetail.NotEnoughMercyTrait;
```

## E30. Crime rating is player-facing, not per-NPC criminal simulation

TaleWorlds.CampaignSystem.CampaignBehaviors.CrimeCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 28-44
```text
28: private void OnDailyTick()
30: foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
32: float dailyCrimeRatingChange = nonBanditFaction.DailyCrimeRatingChange;
33: if (!nonBanditFaction.IsEliminated && !dailyCrimeRatingChange.ApproximatelyEqualsTo(0f))
35: ChangeCrimeRatingAction.Apply(nonBanditFaction, dailyCrimeRatingChange, showNotification: false);
38: foreach (Kingdom item in Kingdom.All)
40: float dailyCrimeRatingChange2 = item.DailyCrimeRatingChange;
41: if (!item.IsEliminated && !dailyCrimeRatingChange2.ApproximatelyEqualsTo(0f))
43: ChangeCrimeRatingAction.Apply(item, dailyCrimeRatingChange2, showNotification: false);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CrimeCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 75-85
```text
75: return;
77: foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
79: if (!nonBanditFaction.IsEliminated)
81: ChangeCrimeRatingAction.Apply(nonBanditFaction, 0f - nonBanditFaction.MainHeroCrimeRating);
84: foreach (Kingdom item in Kingdom.All)
```

## E31. Caravan trade and replenishment are native

TaleWorlds.CampaignSystem.CampaignBehaviors.CaravansCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 571-576
```text
571: private void DailyTickHero(Hero hero)
573: if (hero != Hero.MainHero && MBRandom.RandomFloat < 0.75f && Campaign.Current.Models.CaravanModel.CanHeroCreateCaravan(hero))
575: SpawnCaravan(hero);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CaravansCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 681-701
```text
681: public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
683: Town town = settlement.Town;
684: if (Campaign.Current.GameStarted && mobileParty != null && town != null && mobileParty.IsCaravan && mobileParty.IsPartyTradeActive && mobileParty.IsActive)
686: if (mobileParty.DefaultBehavior == AiBehavior.MoveToNearestLandOrPort)
688: mobileParty.SetMoveModeHold();
690: if (mobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability)
692: AdjustConvoyShips(mobileParty, town);
693: RefillConvoyTroops(mobileParty);
695: if (Campaign.Current.GameStarted)
697: if (_tradeActionLogs.TryGetValue(mobileParty, out var value))
699: for (int num = value.Count - 1; num >= 0; num--)
701: TradeActionLog tradeActionLog = value[num];
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CaravansCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 1235-1255
```text
1235: private void BuyGoods(MobileParty caravanParty, Town town)
1237: List<(EquipmentElement, int)> list = new List<(EquipmentElement, int)>();
1238: float capacityFactor = CalculateCapacityFactor(caravanParty);
1239: float budgetFactor = CalculateBudgetFactor(caravanParty);
1240: RefreshTotalValueOfItemsAtCategoryForParty(caravanParty);
1241: MBList<ItemCategory> mBList = ItemCategories.All.OrderByDescending((ItemCategory x) => CalculateBuyValue(x, town, caravanParty, budgetFactor, capacityFactor)).ToMBList();
1242: int num = (caravanParty.HasNavalNavigationCapability ? 10 : 5);
1243: for (int num2 = 0; num2 < num; num2++)
1245: BuyCategory(caravanParty, town, mBList[num2], budgetFactor, capacityFactor, list);
1247: if (caravanParty.HasNavalNavigationCapability)
1249: BuyCategory(caravanParty, town, DefaultItemCategories.Grain, budgetFactor, capacityFactor, list);
1250: BuyCategory(caravanParty, town, DefaultItemCategories.Fish, budgetFactor, capacityFactor, list);
1252: else if ((float)(caravanParty.ItemRoster.NumberOfPackAnimals + caravanParty.ItemRoster.NumberOfLivestockAnimals) < (float)caravanParty.Party.NumberOfAllMembers * 2f && caravanParty.ItemRoster.NumberOfPackAnimals < caravanParty.Party.NumberOfAllMembers && _packAnimalCategoryIndex >= 0 && caravanParty.PartyTradeGold > 1000)
1254: BuyCategory(caravanParty, town, DefaultItemCategories.PackAnimal, budgetFactor, capacityFactor, list);
```

## E32. NPC caravan eligibility

TaleWorlds.CampaignSystem.GameComponents.DefaultCaravanModel | TaleWorlds.CampaignSystem.dll | output lines 25-40
```text
25: public override int GetPowerChangeAfterCaravanCreation(Hero hero, MobileParty caravanParty)
27: if (hero.Power >= 50f)
29: return -30;
31: return 0;
34: public override bool CanHeroCreateCaravan(Hero hero)
36: if (hero.IsMerchant && hero.PartyBelongedTo == null && hero.OwnedCaravans.Count((CaravanPartyComponent x) => !x.MobileParty.Ai.IsDisabled) == 0 && hero.IsActive && !hero.IsTemplate)
38: return hero.CanLeadParty();
40: return false;
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CaravansCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 495-505
```text
495: public void SpawnCaravan(Hero hero, bool initialSpawn = false)
497: bool flag = Campaign.Current.Models.CaravanModel.GetEliteCaravanSpawnChance(hero) > hero.RandomFloat();
498: PartyTemplateObject randomElementWithPredicate = ((MBReadOnlyList<PartyTemplateObject>)(flag ? hero.Culture.EliteCaravanPartyTemplates : hero.Culture.CaravanPartyTemplates)).GetRandomElementWithPredicate((Func<PartyTemplateObject, bool>)((PartyTemplateObject x) => x.ShipHulls.Count == 0 != hero.CurrentSettlement.HasPort));
499: bool isNaval = randomElementWithPredicate.ShipHulls.Any();
500: Settlement settlement = hero.HomeSettlement ?? hero.BornSettlement;
501: MobileParty caravanParty = CaravanPartyComponent.CreateCaravanParty(spawnSettlement: (settlement == null) ? Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement.HasPort == isNaval).Settlement : (settlement.IsTown ? settlement : ((!settlement.IsVillage) ? Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement.HasPort == isNaval).Settlement : (settlement.Village.TradeBound ?? Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Settlement.HasPort == isNaval).Settlement))), caravanOwner: hero, templateObject: randomElementWithPredicate, isInitialSpawn: initialSpawn, caravanLeader: null, caravanItems: null, isElite: flag);
502: if (!initialSpawn)
504: hero.AddPower(Campaign.Current.Models.CaravanModel.GetPowerChangeAfterCaravanCreation(hero, caravanParty));
```

## E33. NPC clan-member mobility and party usage

TaleWorlds.CampaignSystem.CampaignBehaviors.HeroSpawnCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 144-172
```text
144: private void OnNonBanditClanDailyTick(Clan clan)
146: TrySpawnHeroesAndParties(clan, isNewGame: false);
149: private void TrySpawnHeroesAndParties(Clan clan, bool isNewGame)
151: if (!clan.IsEliminated && clan != Clan.PlayerClan)
153: if (clan.IsMinorFaction)
155: SpawnMinorFactionHeroes(clan, firstTime: false);
157: ConsiderSpawningLordParties(clan, isNewGame);
161: private bool CanHeroMoveToAnotherSettlement(Hero hero)
163: if (hero.Clan != Clan.PlayerClan && !hero.IsTemplate && hero.IsAlive && !hero.IsNotable && !hero.IsHumanPlayerCharacter && !hero.IsPartyLeader && !hero.IsPrisoner && hero.HeroState != Hero.CharacterStates.Disabled && hero.GovernorOf == null && hero.PartyBelongedTo == null && !hero.IsWanderer && hero.PartyBelongedToAsPrisoner == null && hero.CharacterObject.Occupation != Occupation.Special && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && (hero.CurrentSettlement?.Town == null || (!hero.CurrentSettlement.Town.HasTournament && !hero.CurrentSettlement.IsUnderSiege)))
165: return hero.CanMoveToSettlement();
167: return false;
170: private float GetHeroPartyCommandScore(Hero hero)
172: return 3f * (float)hero.GetSkillValue(DefaultSkills.Tactics) + 2f * (float)hero.GetSkillValue(DefaultSkills.Leadership) + (float)hero.GetSkillValue(DefaultSkills.Scouting) + (float)hero.GetSkillValue(DefaultSkills.Steward) + (float)hero.GetSkillValue(DefaultSkills.OneHanded) + (float)hero.GetSkillValue(DefaultSkills.TwoHanded) + (float)hero.GetSkillValue(DefaultSkills.Polearm) + (float)hero.GetSkillValue(DefaultSkills.Riding) + ((hero.Clan.Leader == hero) ? 1000f : 0f) + ((hero.GovernorOf == null) ? 500f : 0f) + (float)(hero.IsNoncombatant ? (-5000) : 0);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.HeroSpawnCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 175-198
```text
175: private void ConsiderSpawningLordParties(Clan clan, bool isNewGame)
177: int partyLimitForTier = Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan, clan.Tier);
178: int count = clan.WarPartyComponents.Count;
179: if (count >= partyLimitForTier)
181: return;
183: int num = partyLimitForTier - count;
184: for (int i = 0; i < num; i++)
186: Hero bestAvailableCommander = GetBestAvailableCommander(clan);
187: if (bestAvailableCommander == null)
189: break;
191: float num2 = CalculateScoreToCreateParty(clan);
192: if (GetHeroPartyCommandScore(bestAvailableCommander) + num2 > 100f)
194: MobileParty mobileParty = SpawnLordParty(bestAvailableCommander, isNewGame);
195: if (mobileParty != null)
197: GiveInitialItemsToParty(mobileParty);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.CompanionRolesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 505-514
```text
505: if (Hero.OneToOneConversationHero != null && !Hero.OneToOneConversationHero.IsPartyLeader && (Hero.OneToOneConversationHero.IsPlayerCompanion || Hero.OneToOneConversationHero.Clan == Clan.PlayerClan) && Hero.OneToOneConversationHero.PartyBelongedTo != MobileParty.MainParty && (Hero.OneToOneConversationHero.PartyBelongedTo == null || !Hero.OneToOneConversationHero.PartyBelongedTo.IsCaravan))
507: if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && Hero.OneToOneConversationHero.GovernorOf == Settlement.CurrentSettlement.Town)
509: MBTextManager.SetTextVariable("COMPANION_REJOIN_LINE", "{=Z5zAok5G}I need to recall you to my party, and to stop governing this town.");
511: else
513: MBTextManager.SetTextVariable("COMPANION_REJOIN_LINE", "{=gR0ksbaQ}Get your things. I'd like you to rejoin the party.");
```

## E34. Player settlement services exist as explicit UI routes

TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerTownVisitCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 50-53
```text
50: campaignGameSystemStarter.AddGameMenuOption("town", "manage_production", "{=dgf6q4qB}Manage town", game_menu_town_manage_town_on_condition, null);
51: campaignGameSystemStarter.AddGameMenuOption("town", "manage_production_cheat", "{=zZ3GqbzC}Manage town (Cheat)", game_menu_town_manage_town_cheat_on_condition, null);
52: campaignGameSystemStarter.AddGameMenuOption("town", "recruit_volunteers", "{=E31IJyqs}Recruit troops", game_menu_town_recruit_troops_on_condition, game_menu_recruit_volunteers_on_consequence);
53: campaignGameSystemStarter.AddGameMenuOption("town", "trade", "{=GmcgoiGy}Trade", game_menu_trade_on_condition, game_menu_town_town_market_on_consequence);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerTownVisitCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 65-79
```text
65: campaignGameSystemStarter.AddGameMenu("town_keep", "{=!}{SETTLEMENT_INFO}", town_keep_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
66: campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_lords_hall_go_to_dungeon", "{=etjMHPjQ}Go to dungeon", game_menu_go_dungeon_on_condition, game_menu_go_dungeon_on_consequence);
67: campaignGameSystemStarter.AddGameMenuOption("town_keep", "leave_troops_to_garrison", "{=7J9KNFTz}Donate troops to garrison", game_menu_leave_troops_garrison_on_condition, game_menu_leave_troops_garrison_on_consequece);
68: campaignGameSystemStarter.AddGameMenuOption("town_keep", "manage_garrison", "{=QazTA60M}Manage garrison", game_menu_manage_garrison_on_condition, game_menu_manage_garrison_on_consequence);
69: campaignGameSystemStarter.AddGameMenuOption("town_keep", "open_stash", "{=xl4K9ecB}Open stash", game_menu_town_keep_open_stash_on_condition, game_menu_town_keep_open_stash_on_consequence);
70: campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_lords_hall", "{=dv2ZNazN}Go to the lord's hall", game_menu_town_keep_go_to_lords_hall_on_condition, game_menu_town_lordshall_on_consequence);
71: campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_lords_hall_cheat", "{=!}Go to the lord's hall (Cheat)", game_menu_castle_go_to_lords_hall_cheat_on_condition, game_menu_lordshall_cheat_on_consequence);
72: campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_castle_back", "{=qWAmxyYz}Back to town center", back_on_condition, delegate
74: GameMenu.SwitchToMenu("town");
75: }, isLeave: true);
76: campaignGameSystemStarter.AddGameMenu("town_keep_dungeon", "{=!}{PRISONER_INTRODUCTION}", town_keep_dungeon_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
77: campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_leave_prisoners", "{=kmsNUfbA}Donate prisoners", game_menu_castle_leave_prisoners_on_condition, game_menu_castle_leave_prisoners_on_consequence);
78: campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_manage_prisoners", "{=VXkL5Ysd}Manage prisoners", game_menu_castle_manage_prisoners_on_condition, game_menu_castle_manage_prisoners_on_consequence);
79: campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison", "{=UnQFawna}Enter the dungeon", game_menu_castle_enter_the_dungeon_on_condition, game_menu_town_dungeon_on_consequence);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerTownVisitCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 133-139
```text
133: campaignGameSystemStarter.AddGameMenu("castle", "{=!}{SETTLEMENT_INFO}", game_menu_castle_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
134: campaignGameSystemStarter.AddGameMenuOption("castle", "castle_prison", "{=esSm5V6t}Go to the dungeon", game_menu_castle_go_to_the_dungeon_on_condition, game_menu_keep_dungeon_on_consequence);
135: campaignGameSystemStarter.AddGameMenuOption("castle", "castle_prison_cheat", "{=pa7oiQb1}Go to the dungeon (Cheat)", game_menu_castle_go_to_dungeon_cheat_on_condition, game_menu_dungeon_cheat_on_consequence);
136: campaignGameSystemStarter.AddGameMenuOption("castle", "manage_garrison", "{=QazTA60M}Manage garrison", game_menu_manage_garrison_on_condition, game_menu_manage_garrison_on_consequence);
137: campaignGameSystemStarter.AddGameMenuOption("castle", "manage_production", "{=Ll1EJHXF}Manage castle", game_menu_manage_castle_on_condition, null);
138: campaignGameSystemStarter.AddGameMenuOption("castle", "open_stash", "{=xl4K9ecB}Open stash", game_menu_town_keep_open_stash_on_condition, game_menu_town_keep_open_stash_on_consequence);
139: campaignGameSystemStarter.AddGameMenuOption("castle", "leave_troops_to_garrison", "{=7J9KNFTz}Donate troops to garrison", game_menu_leave_troops_garrison_on_condition, game_menu_leave_troops_garrison_on_consequece);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.PlayerTownVisitCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 160-164
```text
160: campaignGameSystemStarter.AddGameMenu("village", "{=!}{SETTLEMENT_INFO}", game_menu_village_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
161: campaignGameSystemStarter.AddGameMenuOption("village", "recruit_volunteers", "{=E31IJyqs}Recruit troops", game_menu_recruit_volunteers_on_condition, game_menu_recruit_volunteers_on_consequence);
162: campaignGameSystemStarter.AddGameMenuOption("village", "trade", "{=VN4ctHIU}Buy products", game_menu_village_buy_good_on_condition, null);
163: campaignGameSystemStarter.AddGameMenuOption("village", "village_center", "{=U4azeSib}Take a walk through the lands", game_menu_village_village_center_on_condition, game_menu_village_village_center_on_consequence);
164: campaignGameSystemStarter.AddGameMenuOption("village", "village_wait", "{=zEoHYEUS}Wait here for some time", game_menu_wait_here_on_condition, game_menu_wait_village_on_consequence);
```

## E35. Player issue alternatives are not interchangeable AI APIs

TaleWorlds.CampaignSystem.Issues.IssueBase | TaleWorlds.CampaignSystem.dll | output lines 867-881
```text
867: public void CompleteIssueWithLordSolutionWithRefuseCounterOffer()
869: if (!TextObject.IsNullOrEmpty(LordSolutionCounterOfferRefuseLog))
871: AddLog(new JournalLog(CampaignTime.Now, LordSolutionCounterOfferRefuseLog));
873: ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -NeededInfluenceForLordSolution);
874: if (RewardGold > 0)
876: GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
878: LordSolutionConsequenceWithRefuseCounterOffer();
879: IssueFinalized();
880: CampaignEventDispatcher.Instance.OnIssueUpdated(this, IssueUpdateDetails.IssueFinishedWithSuccess, Hero.MainHero);
```

TaleWorlds.CampaignSystem.CampaignBehaviors.IssuesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 951-959
```text
951: private static bool IssueLordSolutionCondition()
953: IssueBase issueOwnersIssue = GetIssueOwnersIssue();
954: if (issueOwnersIssue.IssueOwner.CurrentSettlement != null)
956: return issueOwnersIssue.IssueOwner.CurrentSettlement.OwnerClan == Clan.PlayerClan;
958: return false;
```

## E36. Native notable support

TaleWorlds.CampaignSystem.CampaignBehaviors.NotablesCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 246-278
```text
246: private void UpdateNotableSupport(Hero notable)
248: if (notable.SupporterOf == null)
250: foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
252: if (nonBanditFaction.Leader != null && nonBanditFaction != Clan.PlayerClan)
254: int relation = notable.GetRelation(nonBanditFaction.Leader);
255: if (relation > 50)
257: float num = (float)(relation - 50) / 2000f;
258: if (MBRandom.RandomFloat < num)
260: notable.SupporterOf = nonBanditFaction;
265: return;
267: int relation2 = notable.GetRelation(notable.SupporterOf.Leader);
268: if (relation2 < 0 || MBRandom.RandomFloat < (50f - (float)relation2) / 500f)
270: bool num2 = notable.SupporterOf == Clan.PlayerClan;
271: notable.SupporterOf = null;
272: if (num2)
274: TextObject textObject = new TextObject("{=aaOIjHeP}{NOTABLE.NAME} no longer supports your clan as your relationship deteriorated too much.");
275: textObject.SetCharacterProperties("NOTABLE", notable.CharacterObject);
276: InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f)));
```

TaleWorlds.CampaignSystem.CampaignBehaviors.NotableSupportersCampaignBehavior | TaleWorlds.CampaignSystem.dll | output lines 117-126
```text
117: explanation = TextObject.GetEmpty();
118: return true;
121: private void notable_support_player_decision_accept_on_consequences()
123: int initialNotableSupporterCost = Campaign.Current.Models.NotablePowerModel.GetInitialNotableSupporterCost(Hero.OneToOneConversationHero);
124: Hero.OneToOneConversationHero.SupporterOf = Clan.PlayerClan;
125: GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, initialNotableSupporterCost);
126: ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 5);
```

## E37. Existing ClanAI coverage and overlap check

All 23 files listed in the manifest were inspected from the authoritative archive. The current ClanAI C# tree has no matches for: GetNextDailyBuilding, BuildingScoreCalculationModel, ChangeDefaultBuilding, FestivalAndGames, CompleteIssueWithAiLord, IssueFinishedByAILord, UpdateGovernorsOfClan. This is a scoped source finding; it does not certify arbitrary other mods or all native subscribers.

## E38. Documentation-only preservation

Byte-for-byte comparison against the base archive: 370 files under src/, Data/, Tests/, .github/ checked; changed=0.
No gameplay source, runtime dependency, save schema, installed DLL, campaign, or production configuration was changed by this audit.
No build rerun is claimed: this checkpoint adds documentation/evidence only. Phase 2-5 accepted build/runtime results remain historical evidence, not fresh Phase 6 proof.
