using System;
using ClanAI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

internal sealed class FakeVolunteerModel : VolunteerModel
{
    internal int MaxTierReads;
    internal int HeroIndexCalls;
    internal int GarrisonIndexCalls;
    internal int ProductionCalls;
    internal int BasicVolunteerCalls;
    internal int CanHaveCalls;

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
        return 0.4375f;
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
        var inner = new FakeVolunteerModel();
        var wrapper = new LocalManpowerVolunteerModel(inner);

        Check(object.ReferenceEquals(wrapper.InnerModel, inner),
            "wrapper preserves exact inner reference");

        Check(wrapper.MaxVolunteerTier == 4,
            "max tier delegated");
        Check(inner.MaxTierReads == 1,
            "max tier delegated once");

        Check(wrapper.MaximumIndexHeroCanRecruitFromHero(
                  null, null, 17) == 3,
            "hero recruit index delegated");
        Check(inner.HeroIndexCalls == 1,
            "hero recruit index called once");

        Check(wrapper.MaximumIndexGarrisonCanRecruitFromHero(
                  null, null) == 2,
            "garrison recruit index delegated");
        Check(inner.GarrisonIndexCalls == 1,
            "garrison recruit index called once");

        Check(wrapper.GetBasicVolunteer(null) == null,
            "basic volunteer delegated");
        Check(inner.BasicVolunteerCalls == 1,
            "basic volunteer called once");

        Check(wrapper.CanHaveRecruits(null),
            "can have recruits delegated");
        Check(inner.CanHaveCalls == 1,
            "can have recruits called once");

        float production =
            wrapper.GetDailyVolunteerProductionProbability(
                null, 0, null);
        Check(SameBits(production, 0.4375f),
            "invalid context exact native production passthrough");
        Check(inner.ProductionCalls == 1,
            "inner production called exactly once per evaluation");

        Console.WriteLine(
            "PASS Phase 4B VolunteerModel delegation checks=" +
            _checks);
    }
}
