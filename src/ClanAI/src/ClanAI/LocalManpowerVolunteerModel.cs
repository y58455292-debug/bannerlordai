using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Phase 4B-v1 selected-model wrapper. It delegates all native volunteer
    // behavior unchanged except empty-slot daily production probability.
    public sealed class LocalManpowerVolunteerModel
        : VolunteerModel
    {
        private readonly VolunteerModel _inner;

        public VolunteerModel InnerModel
        {
            get { return _inner; }
        }

        public LocalManpowerVolunteerModel(
            VolunteerModel inner)
        {
            if (inner == null)
                throw new ArgumentNullException("inner");

            _inner = inner;
        }

        public override int MaxVolunteerTier
        {
            get { return _inner.MaxVolunteerTier; }
        }

        public override int MaximumIndexHeroCanRecruitFromHero(
            Hero buyerHero,
            Hero sellerHero,
            int useValueAsRelation = -101)
        {
            return _inner.MaximumIndexHeroCanRecruitFromHero(
                buyerHero,
                sellerHero,
                useValueAsRelation);
        }

        public override int MaximumIndexGarrisonCanRecruitFromHero(
            Settlement settlement,
            Hero sellerHero)
        {
            return _inner.MaximumIndexGarrisonCanRecruitFromHero(
                settlement,
                sellerHero);
        }

        public override float GetDailyVolunteerProductionProbability(
            Hero hero,
            int index,
            Settlement settlement)
        {
            float nativeProbability =
                _inner.GetDailyVolunteerProductionProbability(
                    hero,
                    index,
                    settlement);

            CharacterObject volunteer;
            bool slotIsEmpty;
            bool slotKnown =
                TryGetSlotState(
                    hero,
                    index,
                    out volunteer,
                    out slotIsEmpty);

            if (!slotKnown)
            {
                LocalManpowerProbabilityResult passthrough =
                    LocalManpowerProbabilityPolicy.Evaluate(
                        nativeProbability,
                        false,
                        false,
                        LocalManpowerPopulationBand.Unknown,
                        false,
                        0.0f,
                        false);

                LocalManpowerRuntimeTelemetry.ObserveEvaluation(
                    hero,
                    index,
                    settlement,
                    false,
                    false,
                    nativeProbability,
                    passthrough);

                return passthrough.FinalProbability;
            }

            if (!slotIsEmpty)
            {
                LocalManpowerProbabilityResult passthrough =
                    LocalManpowerProbabilityPolicy.Evaluate(
                        nativeProbability,
                        false,
                        true,
                        LocalManpowerPopulationBand.Unknown,
                        false,
                        0.0f,
                        false);

                int maxVolunteerTier =
                    _inner.MaxVolunteerTier;

                bool nativeUpgradeEligible =
                    TroopQualityVolunteerEligibility
                        .IsNativeUpgradeEligible(
                            volunteer,
                            maxVolunteerTier);

                if (!TroopQualityProbabilityPolicy
                        .ShouldApplyToOccupiedSlot(
                            slotKnown,
                            slotIsEmpty,
                            nativeUpgradeEligible))
                {
                    LocalManpowerRuntimeTelemetry.ObserveEvaluation(
                        hero,
                        index,
                        settlement,
                        true,
                        false,
                        nativeProbability,
                        passthrough);

                    return passthrough.FinalProbability;
                }

                return EvaluateOccupiedQualitySlot(
                    settlement,
                    nativeProbability);
            }

            return EvaluateEmptySlot(
                hero,
                index,
                settlement,
                nativeProbability);
        }

        private static float EvaluateEmptySlot(
            Hero hero,
            int index,
            Settlement settlement,
            float nativeProbability)
        {
            LocalManpowerPopulationBand populationBand;
            if (!TryGetPopulationBand(
                    settlement,
                    out populationBand))
            {
                LocalManpowerProbabilityResult passthrough =
                    LocalManpowerProbabilityPolicy.Evaluate(
                        nativeProbability,
                        true,
                        false,
                        LocalManpowerPopulationBand.Unknown,
                        false,
                        0.0f,
                        false);

                LocalManpowerRuntimeTelemetry.ObserveEvaluation(
                    hero,
                    index,
                    settlement,
                    true,
                    true,
                    nativeProbability,
                    passthrough);

                return passthrough.FinalProbability;
            }

            bool hasSecurity;
            float security;
            ResolveSecurity(
                settlement,
                out hasSecurity,
                out security);

            bool contextValid =
                !hasSecurity ||
                LocalManpowerProbabilityPolicy.IsFinite(
                    security);

            LocalManpowerProbabilityResult result =
                LocalManpowerProbabilityPolicy.Evaluate(
                    nativeProbability,
                    true,
                    contextValid,
                    populationBand,
                    hasSecurity,
                    security,
                    IsAcuteDisruption(settlement));

            LocalManpowerRuntimeTelemetry.ObserveEvaluation(
                hero,
                index,
                settlement,
                true,
                true,
                nativeProbability,
                result);

            return result.FinalProbability;
        }

        private static float EvaluateOccupiedQualitySlot(
            Settlement settlement,
            float nativeProbability)
        {
            LocalManpowerPopulationBand populationBand;
            if (!TryGetPopulationBand(
                    settlement,
                    out populationBand))
            {
                return TroopQualityProbabilityPolicy.Evaluate(
                    nativeProbability,
                    false,
                    LocalManpowerPopulationBand.Unknown,
                    false,
                    0.0f,
                    false).FinalProbability;
            }

            bool hasSecurity;
            float security;
            ResolveSecurity(
                settlement,
                out hasSecurity,
                out security);

            bool contextValid =
                !hasSecurity ||
                LocalManpowerProbabilityPolicy.IsFinite(
                    security);

            return TroopQualityProbabilityPolicy.Evaluate(
                nativeProbability,
                contextValid,
                populationBand,
                hasSecurity,
                security,
                IsAcuteDisruption(settlement))
                .FinalProbability;
        }

        public override CharacterObject GetBasicVolunteer(
            Hero hero)
        {
            return _inner.GetBasicVolunteer(hero);
        }

        public override bool CanHaveRecruits(
            Hero hero)
        {
            return _inner.CanHaveRecruits(hero);
        }

        private static bool TryGetSlotState(
            Hero hero,
            int index,
            out CharacterObject volunteer,
            out bool slotIsEmpty)
        {
            volunteer = null;
            slotIsEmpty = false;

            if (hero == null ||
                hero.VolunteerTypes == null ||
                index < 0 ||
                index >= hero.VolunteerTypes.Length)
            {
                return false;
            }

            slotIsEmpty =
                hero.VolunteerTypes[index] == null;

            volunteer =
                hero.VolunteerTypes[index];

            return true;
        }

        private static bool TryGetPopulationBand(
            Settlement settlement,
            out LocalManpowerPopulationBand band)
        {
            band = LocalManpowerPopulationBand.Unknown;

            if (settlement == null)
                return false;

            if (settlement.IsTown &&
                settlement.Town != null)
            {
                return TryMapProsperityLevel(
                    settlement.Town.GetProsperityLevel(),
                    out band);
            }

            if (settlement.IsVillage &&
                settlement.Village != null)
            {
                return TryMapProsperityLevel(
                    settlement.Village.GetProsperityLevel(),
                    out band);
            }

            return false;
        }

        private static bool TryMapProsperityLevel(
            SettlementComponent.ProsperityLevel level,
            out LocalManpowerPopulationBand band)
        {
            switch (level)
            {
                case SettlementComponent.ProsperityLevel.Low:
                    band = LocalManpowerPopulationBand.Low;
                    return true;
                case SettlementComponent.ProsperityLevel.Mid:
                    band = LocalManpowerPopulationBand.Mid;
                    return true;
                case SettlementComponent.ProsperityLevel.High:
                    band = LocalManpowerPopulationBand.High;
                    return true;
                default:
                    band = LocalManpowerPopulationBand.Unknown;
                    return false;
            }
        }

        private static void ResolveSecurity(
            Settlement settlement,
            out bool hasSecurity,
            out float security)
        {
            hasSecurity = false;
            security = 0.0f;

            if (settlement == null)
                return;

            if (settlement.IsTown &&
                settlement.Town != null)
            {
                security = settlement.Town.Security;
                hasSecurity = true;
                return;
            }

            if (settlement.IsVillage &&
                settlement.Village != null)
            {
                Settlement bound =
                    settlement.Village.Bound;

                if (bound != null &&
                    bound.Town != null)
                {
                    security = bound.Town.Security;
                    hasSecurity = true;
                }
            }
        }

        private static bool IsAcuteDisruption(
            Settlement settlement)
        {
            return settlement != null &&
                (settlement.IsUnderRaid ||
                 settlement.IsUnderSiege);
        }
    }
}
