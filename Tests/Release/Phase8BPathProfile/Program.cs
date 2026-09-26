using System;
using System.IO;
using ClanAI;

internal static class Program
{
    private static int _failures;

    private static void Check(bool condition, string message)
    {
        if (condition)
            return;

        Console.WriteLine("FAIL " + message);
        _failures++;
    }

    private static void Main()
    {
        string module = Path.Combine(
            Path.GetTempPath(), "Bannerlord", "Modules", "ClanAI");
        string assembly = Path.Combine(
            module, "bin", "Win64_Shipping_Client", "ClanAI.dll");

        Check(
            ModuleRuntimePaths.ResolveModuleDirectory(assembly) ==
                Path.GetFullPath(module),
            "module root resolution");

        string config = ModuleRuntimePaths.Resolve(
            assembly, "Data", "DynastyMindSeed.cfg");
        string log = ModuleRuntimePaths.Resolve(
            assembly, "Logs", "clanai.log");
        Check(config == Path.Combine(module, "Data", "DynastyMindSeed.cfg"),
            "module-local data path");
        Check(log == Path.Combine(module, "Logs", "clanai.log"),
            "module-local log path");
        Check(ModuleRuntimePaths.Resolve(assembly, "..", "escape.txt") == null,
            "parent traversal rejected");
        Check(ModuleRuntimePaths.Resolve(assembly, Path.GetPathRoot(module)) == null,
            "rooted child rejected");
        Check(ModuleRuntimePaths.ResolveModuleDirectory("not-a-module.dll") == null,
            "invalid layout rejected");

        string status;
        Check(RuntimeProfile.Parse(null, out status) == ClanAIRuntimeProfile.Release &&
            status == "profile_missing_default_release",
            "missing profile defaults release");
        Check(RuntimeProfile.Parse(new string[] { "Profile=Unknown" }, out status) ==
            ClanAIRuntimeProfile.Release &&
            status == "invalid_profile_default_release",
            "invalid profile defaults release");
        Check(RuntimeProfile.Parse(new string[] { "Profile=Release" }, out status) ==
            ClanAIRuntimeProfile.Release,
            "explicit release");
        Check(RuntimeProfile.Parse(new string[] { "Profile=Evidence" }, out status) ==
            ClanAIRuntimeProfile.Evidence,
            "explicit evidence opt-in");

        if (_failures != 0)
            Environment.Exit(1);

        Console.WriteLine("PASS Phase 8B module-local path resolver");
        Console.WriteLine("PASS Phase 8B missing/invalid release-safe profile");
        Console.WriteLine("PASS Phase 8B explicit evidence-profile opt-in");
    }
}

