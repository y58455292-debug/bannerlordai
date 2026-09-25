using System;
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

    private static void Main()
    {
        Assembly clanAI =
            typeof(LocalManpowerVolunteerModel).Assembly;

        Type patchType = clanAI.GetType(
            "ClanAI.LocalBanditControlPatch",
            throwOnError: true);

        MethodInfo resolver = patchType.GetMethod(
            "ResolveTargetMethod",
            BindingFlags.Static |
            BindingFlags.NonPublic);

        Check(resolver != null,
            "resolver exists");

        MethodInfo target = (MethodInfo)
            resolver.Invoke(null, null);

        Check(target != null,
            "target resolves on supported binary");
        Check(
            target.DeclaringType.FullName ==
            "TaleWorlds.CampaignSystem.CampaignBehaviors.BanditSpawnCampaignBehavior",
            "exact behavior type");
        Check(
            target.Name ==
            "GetSpawnChanceInSettlement",
            "exact method name");
        Check(target.IsPrivate,
            "target remains private");
        Check(!target.IsStatic,
            "target remains instance method");
        Check(target.ReturnType == typeof(float),
            "target return type float");

        ParameterInfo[] parameters =
            target.GetParameters();

        Check(parameters.Length == 1,
            "target has one parameter");
        Check(
            parameters[0].ParameterType.FullName ==
            "TaleWorlds.CampaignSystem.Settlements.Settlement",
            "target parameter is Settlement");

        MethodInfo postfix = patchType.GetMethod(
            "SpawnWeightPostfix",
            BindingFlags.Static |
            BindingFlags.NonPublic);

        Check(postfix != null,
            "postfix exists");
        Check(postfix.ReturnType == typeof(void),
            "postfix cannot skip original");

        ParameterInfo[] postfixParameters =
            postfix.GetParameters();

        Check(postfixParameters.Length == 2,
            "postfix has settlement and result only");
        Check(
            postfixParameters[0].Name == "__0" &&
            postfixParameters[0].ParameterType.FullName ==
            "TaleWorlds.CampaignSystem.Settlements.Settlement",
            "postfix binds first native argument");
        Check(
            postfixParameters[1].Name == "__result" &&
            postfixParameters[1].ParameterType ==
            typeof(float).MakeByRefType(),
            "postfix modifies result by ref");

        Console.WriteLine(
            "PASS Phase 5 private patch resolution checks=" +
            _checks);
    }
}
