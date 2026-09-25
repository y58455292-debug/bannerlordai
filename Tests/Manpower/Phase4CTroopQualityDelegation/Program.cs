using System;
using System.Reflection;
using ClanAI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

internal sealed class QualityFakeVolunteerModel : VolunteerModel
{
    internal int MaxTierReads;
    internal int HeroIndexCalls;
    internal int GarrisonIndexCalls;
    internal int ProductionCalls;
    internal int BasicVolunteerCalls;
    internal int CanHaveCalls;
    internal float ProductionValue = 0.4375f;

    public override int MaxVolunteerTier
    {
        get
        {
            MaxTierReads++;
            return 4;
        }
    }

    public override int MaximumIndexHeroCanRecruitFromHero(
        Hero buyerHero,
        Hero sellerHero,
        int useValueAsRelation = -101)
    {
        HeroIndexCalls++;
        return 3;
    }

    public override int MaximumIndexGarrisonCanRecruitFromHero(
        Settlement settlement,
        Hero sellerHero)
    {
        GarrisonIndexCalls++;
        return 2;
    }

    public override float GetDailyVolunteerProductionProbability(
        Hero hero,
        int index,
        Settlement settlement)
    {
        ProductionCalls++;
        return ProductionValue;
    }

    public override CharacterObject GetBasicVolunteer(
        Hero hero)
    {
        BasicVolunteerCalls++;
        return null;
    }

    public override bool CanHaveRecruits(
        Hero hero)
    {
        CanHaveCalls++;
        return true;
    }
}

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition)
            throw new Exception("FAIL " + name);
    }

    private static bool SameBits(float a, float b)
    {
        return BitConverter.SingleToInt32Bits(a) ==
            BitConverter.SingleToInt32Bits(b);
    }

    private static void Main()
    {
        var inner = new QualityFakeVolunteerModel();
        var wrapper = new LocalManpowerVolunteerModel(inner);

        Check(object.ReferenceEquals(wrapper.InnerModel, inner),
            "wrapper preserves exact selected inner reference");

        Check(wrapper.MaxVolunteerTier == 4,
            "max volunteer tier delegates");
        Check(inner.MaxTierReads == 1,
            "max volunteer tier delegate count");

        Check(wrapper.MaximumIndexHeroCanRecruitFromHero(
                  null, null, 17) == 3,
            "hero access delegates");
        Check(inner.HeroIndexCalls == 1,
            "hero access delegate count");

        Check(wrapper.MaximumIndexGarrisonCanRecruitFromHero(
                  null, null) == 2,
            "garrison access delegates");
        Check(inner.GarrisonIndexCalls == 1,
            "garrison access delegate count");

        Check(wrapper.GetBasicVolunteer(null) == null,
            "basic volunteer delegates");
        Check(inner.BasicVolunteerCalls == 1,
            "basic volunteer delegate count");

        Check(wrapper.CanHaveRecruits(null),
            "can have recruits delegates");
        Check(inner.CanHaveCalls == 1,
            "can have recruits delegate count");

        // Invalid slot/context: native-safe passthrough, one inner call.
        inner.ProductionCalls = 0;
        float invalid =
            wrapper.GetDailyVolunteerProductionProbability(
                null, 0, null);
        Check(SameBits(invalid, inner.ProductionValue),
            "invalid slot exact normal native passthrough");
        Check(inner.ProductionCalls == 1,
            "invalid slot inner production exactly once");

        // Empty slot: must not consult MaxVolunteerTier / Phase 4C eligibility.
        var emptyHero = new Hero();
        emptyHero.VolunteerTypes = new CharacterObject[6];
        inner.ProductionCalls = 0;
        inner.MaxTierReads = 0;
        float empty =
            wrapper.GetDailyVolunteerProductionProbability(
                emptyHero, 0, null);
        Check(SameBits(empty, inner.ProductionValue),
            "empty invalid context remains phase4b native-safe passthrough");
        Check(inner.ProductionCalls == 1,
            "empty slot inner production exactly once");
        Check(inner.MaxTierReads == 0,
            "empty slot does not consult quality max tier");

        // Occupied but no UpgradeTargets: exact native passthrough.
        var occupiedHero = new Hero();
        occupiedHero.VolunteerTypes = new CharacterObject[6];
        occupiedHero.VolunteerTypes[0] = new CharacterObject();
        inner.ProductionCalls = 0;
        inner.MaxTierReads = 0;
        float occupied =
            wrapper.GetDailyVolunteerProductionProbability(
                occupiedHero, 0, null);
        Check(SameBits(occupied, inner.ProductionValue),
            "occupied nonupgradeable exact native passthrough");
        Check(inner.ProductionCalls == 1,
            "occupied nonupgradeable inner production exactly once");
        Check(inner.MaxTierReads == 1,
            "occupied eligibility reads selected inner max tier once");

        // Compiled assembly exposes the pure branch selector used by wrapper.
        Type qualityPolicy = typeof(LocalManpowerVolunteerModel)
            .Assembly.GetType(
                "ClanAI.TroopQualityProbabilityPolicy",
                throwOnError: true);
        MethodInfo shouldApply = qualityPolicy.GetMethod(
            "ShouldApplyToOccupiedSlot",
            BindingFlags.Static | BindingFlags.NonPublic);
        Check(shouldApply != null,
            "quality occupied branch selector exists");

        bool selected = (bool)shouldApply.Invoke(
            null,
            new object[] { true, false, true });
        Check(selected,
            "occupied upgrade eligible selects phase4c quality branch");

        bool emptyRejected = (bool)shouldApply.Invoke(
            null,
            new object[] { true, true, true });
        Check(!emptyRejected,
            "empty slot rejected by phase4c branch selector");

        bool noneligibleRejected = (bool)shouldApply.Invoke(
            null,
            new object[] { true, false, false });
        Check(!noneligibleRejected,
            "occupied nonupgradeable rejected by phase4c branch selector");

        Console.WriteLine(
            "PASS Phase 4C VolunteerModel branch/delegation checks=" +
            _checks);
    }
}
