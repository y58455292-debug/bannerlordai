using System;
using System.Linq;
using System.Reflection;
using ClanAI;

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

    private static CivicProjectSelectionResult Eval(
        bool contextValid = true,
        bool isNpcTown = true,
        bool isConstructionIdle = true,
        bool hasFestival = true,
        float loyalty = 24.0f,
        float threshold = 25.0f)
    {
        return CivicProjectSelectionPolicy.Evaluate(
            contextValid,
            isNpcTown,
            isConstructionIdle,
            hasFestival,
            loyalty,
            threshold);
    }

    private static void Main()
    {
        Check(!Eval(contextValid: false).PreferFestivalAndGames,
            "invalid context passthrough");
        Check(Eval(contextValid: false).Reason ==
                CivicProjectPreferenceReason.InvalidContext,
            "invalid context reason");

        Check(!Eval(isNpcTown: false).PreferFestivalAndGames,
            "non NPC town passthrough");
        Check(Eval(isNpcTown: false).Reason ==
                CivicProjectPreferenceReason.NonNpcTown,
            "non NPC reason");

        Check(!Eval(isConstructionIdle: false).PreferFestivalAndGames,
            "active construction passthrough");
        Check(Eval(isConstructionIdle: false).Reason ==
                CivicProjectPreferenceReason.ConstructionActive,
            "active construction reason");

        Check(!Eval(hasFestival: false).PreferFestivalAndGames,
            "missing festival passthrough");
        Check(Eval(hasFestival: false).Reason ==
                CivicProjectPreferenceReason.FestivalUnavailable,
            "missing festival reason");

        Check(!Eval(loyalty: 26.0f, threshold: 25.0f)
                .PreferFestivalAndGames,
            "above threshold passthrough");
        Check(!Eval(loyalty: 25.0f, threshold: 25.0f)
                .PreferFestivalAndGames,
            "equal threshold passthrough");

        Check(Eval(loyalty: 24.999f, threshold: 25.0f)
                .PreferFestivalAndGames,
            "just below threshold prefers festival");
        Check(Eval(loyalty: 0.0f, threshold: 25.0f)
                .PreferFestivalAndGames,
            "very low loyalty prefers festival");

        Check(!Eval(loyalty: float.NaN)
                .PreferFestivalAndGames,
            "NaN loyalty passthrough");
        Check(Eval(loyalty: float.NaN).Reason ==
                CivicProjectPreferenceReason.InvalidLoyalty,
            "NaN loyalty reason");
        Check(!Eval(loyalty: float.PositiveInfinity)
                .PreferFestivalAndGames,
            "infinite loyalty passthrough");

        Check(!Eval(threshold: float.NaN)
                .PreferFestivalAndGames,
            "NaN threshold passthrough");
        Check(Eval(threshold: float.NaN).Reason ==
                CivicProjectPreferenceReason.InvalidThreshold,
            "NaN threshold reason");
        Check(!Eval(threshold: float.PositiveInfinity)
                .PreferFestivalAndGames,
            "infinite threshold passthrough");

        Check(Eval(loyalty: 59.999f, threshold: 60.0f)
                .PreferFestivalAndGames,
            "selected high rebellion threshold respected");
        Check(!Eval(loyalty: 60.0f, threshold: 60.0f)
                .PreferFestivalAndGames,
            "selected threshold equality strict");

        CivicProjectSelectionResult preferred =
            Eval();
        Check(preferred.Reason ==
                CivicProjectPreferenceReason.LowLoyaltyFestivalPreferred,
            "preference reason");

        MethodInfo evaluate =
            typeof(CivicProjectSelectionPolicy).GetMethod(
                "Evaluate",
                BindingFlags.Static |
                BindingFlags.NonPublic);
        ParameterInfo[] parameters =
            evaluate.GetParameters();

        Check(parameters.Length == 6,
            "policy has only six minimal inputs");

        string[] names =
            parameters.Select(x => x.Name).ToArray();

        Check(string.Join(",", names) ==
                "contextValid,isNpcTown,isConstructionIdle,hasFestivalAndGamesProject,loyalty,nativeRebelliousThreshold",
            "policy inputs contain no unrelated systems");

        Console.WriteLine(
            "PASS Phase 6 civic-project policy checks=" +
            _checks);
    }
}

[executed on device: DESKTOP-JO4B7VH (fd6618f4-5715-46b1-8665-68172ef15169)]