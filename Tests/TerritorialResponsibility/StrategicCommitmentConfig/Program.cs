using System;
using System.IO;
using ClanAI;

internal static class Program
{
    private static int _checks;
    private static int _failures;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (condition)
            return;

        Console.WriteLine("FAIL " + name);
        _failures++;
    }

    private static void ParseTests()
    {
        string status;

        var missing = StrategicCommitmentConfig.ParseMode(
            null,
            out status);
        Check(missing == StrategicCommitmentMode.Observe &&
              status == "mode_missing_default_observe",
            "null-config-defaults-observe");

        var comments = StrategicCommitmentConfig.ParseMode(
            new[] { "# comment", "   ", "Other=1" },
            out status);
        Check(comments == StrategicCommitmentMode.Observe &&
              status == "mode_missing_default_observe",
            "missing-mode-defaults-observe");
        var observe = StrategicCommitmentConfig.ParseMode(
            new[] { "Mode=Observe" },
            out status);
        Check(observe == StrategicCommitmentMode.Observe &&
              status == "configured_observe",
            "observe-mode-parses");

        var apply = StrategicCommitmentConfig.ParseMode(
            new[] { "Mode=Apply" },
            out status);
        Check(apply == StrategicCommitmentMode.Apply &&
              status == "configured_apply",
            "apply-mode-parses");

        var caseInsensitive = StrategicCommitmentConfig.ParseMode(
            new[] { " mode = apply " },
            out status);
        Check(caseInsensitive == StrategicCommitmentMode.Apply &&
              status == "configured_apply",
            "mode-parsing-case-insensitive");

        var invalid = StrategicCommitmentConfig.ParseMode(
            new[] { "Mode=Enabled" },
            out status);
        Check(invalid == StrategicCommitmentMode.Observe &&
              status == "invalid_mode_default_observe",
            "invalid-mode-defaults-observe");

        var unknownBefore = StrategicCommitmentConfig.ParseMode(
            new[] { "Other=Apply", "Mode=Observe" },
            out status);
        Check(unknownBefore == StrategicCommitmentMode.Observe &&
              status == "configured_observe",
            "unknown-key-does-not-enable-apply");
    }
    private static void PathTests()
    {
        string assembly =
            @"C:\Games\Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll";
        string expected =
            @"C:\Games\Bannerlord\Modules\ClanAI\Data\StrategicCommitment.cfg";

        string resolved =
            StrategicCommitmentConfig.ResolveConfigPath(
                assembly);

        Check(
            string.Equals(
                resolved,
                expected,
                StringComparison.OrdinalIgnoreCase),
            "module-local-config-path");

        Check(
            string.IsNullOrEmpty(
                StrategicCommitmentConfig.ResolveConfigPath(
                    null)),
            "null-assembly-path-safe-observe");

        Check(
            string.IsNullOrEmpty(
                StrategicCommitmentConfig.ResolveConfigPath(
                    @"C:\Games\ClanAI.dll")),
            "unexpected-layout-safe-observe");

        Check(
            resolved != null &&
            resolved.IndexOf(
                @"D:\BannerlordAIResearch",
                StringComparison.OrdinalIgnoreCase) < 0,
            "resolved-path-has-no-development-root");

        Check(
            resolved != null &&
            resolved.EndsWith(
                Path.Combine(
                    "Data",
                    StrategicCommitmentConfig.ConfigFileName),
                StringComparison.OrdinalIgnoreCase),
            "config-resolves-under-module-data");
        Check(
            StrategicCommitmentConfig.Mode ==
                StrategicCommitmentMode.Observe &&
            StrategicCommitmentConfig.Status ==
                "missing_default_observe",
            "unresolved-test-layout-defaults-observe");
    }

    private static int Main()
    {
        ParseTests();
        PathTests();

        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL Strategic Commitment config tests failures=" +
                _failures +
                " checks=" + _checks);
            return 1;
        }

        Console.WriteLine(
            "PASS Strategic Commitment config tests checks=" + _checks);
        Console.WriteLine(
            "module-local path and missing/invalid Observe defaults preserved");
        return 0;
    }
}