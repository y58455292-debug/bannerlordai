using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using ClanAI;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

internal sealed class FakeBuildingScoreCalculationModel
    : BuildingScoreCalculationModel
{
    private readonly Building _ordinary;
    private readonly Building _daily;

    internal int OrdinaryCalls;
    internal int DailyCalls;

    internal FakeBuildingScoreCalculationModel(
        Building ordinary,
        Building daily)
    {
        _ordinary = ordinary;
        _daily = daily;
    }

    public override Building GetNextBuilding(
        Town town)
    {
        OrdinaryCalls++;
        return _ordinary;
    }

    public override Building GetNextDailyBuilding(
        Town town)
    {
        DailyCalls++;
        return _daily;
    }
}

internal static class Program
{
    private static int _checks;

    private static void Check(
        bool condition,
        string name)
    {
        _checks++;
        if (!condition)
            throw new Exception("FAIL " + name);
    }

    private static Building UninitializedBuilding()
    {
        return (Building)RuntimeHelpers
            .GetUninitializedObject(typeof(Building));
    }

    private static Building SelectResult(
        Building nativeResult,
        Building festival,
        bool preferFestival)
    {
        MethodInfo method =
            typeof(CivicProjectBuildingScoreCalculationModel)
                .GetMethod(
                    "SelectResult",
                    BindingFlags.Static |
                    BindingFlags.NonPublic);

        Check(method != null,
            "selection helper resolved");

        return (Building)method.Invoke(
            null,
            new object[]
            {
                nativeResult,
                festival,
                preferFestival
            });
    }

    private static void Main()
    {
        Building ordinary = UninitializedBuilding();
        Building nativeDaily = UninitializedBuilding();
        Building festival = UninitializedBuilding();

        var inner =
            new FakeBuildingScoreCalculationModel(
                ordinary,
                nativeDaily);

        var wrapper =
            new CivicProjectBuildingScoreCalculationModel(
                inner);

        Check(object.ReferenceEquals(
                wrapper.InnerModel,
                inner),
            "wrapper preserves exact inner reference");

        Building ordinaryResult =
            wrapper.GetNextBuilding(null);
        Check(object.ReferenceEquals(
                ordinaryResult,
                ordinary),
            "ordinary building selection exact delegation");
        Check(inner.OrdinaryCalls == 1,
            "ordinary selection called once");

        Building dailyResult =
            wrapper.GetNextDailyBuilding(null);
        Check(object.ReferenceEquals(
                dailyResult,
                nativeDaily),
            "foreign inner daily exact passthrough");
        Check(inner.DailyCalls == 1,
            "inner daily selection called exactly once");

        Check(!CivicProjectBuildingScoreCalculationModel
                .SupportsInnerModel(inner),
            "foreign inner unsupported");

        var supportedInner =
            new DefaultBuildingScoreCalculationModel();
        Check(CivicProjectBuildingScoreCalculationModel
                .SupportsInnerModel(supportedInner),
            "audited default inner supported");

        Building passthrough =
            SelectResult(
                nativeDaily,
                festival,
                false);
        Check(object.ReferenceEquals(
                passthrough,
                nativeDaily),
            "default path exact native reference");

        Building preferred =
            SelectResult(
                nativeDaily,
                festival,
                true);
        Check(object.ReferenceEquals(
                preferred,
                festival),
            "special path exact existing festival reference");

        Building noFestival =
            SelectResult(
                nativeDaily,
                null,
                true);
        Check(object.ReferenceEquals(
                noFestival,
                nativeDaily),
            "missing festival conservative passthrough");

        bool threw = false;
        try
        {
            new CivicProjectBuildingScoreCalculationModel(
                null);
        }
        catch (ArgumentNullException)
        {
            threw = true;
        }

        Check(threw,
            "null inner rejected");

        Console.WriteLine(
            "PASS Phase 6 civic-project delegation checks=" +
            _checks);
    }
}

[executed on device: DESKTOP-JO4B7VH (fd6618f4-5715-46b1-8665-68172ef15169)]