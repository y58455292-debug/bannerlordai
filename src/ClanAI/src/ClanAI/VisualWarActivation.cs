using System;
using System.IO;

namespace ClanAI
{
    internal static class VisualWarActivation
    {
        internal const string MarkerFileName =
            "ENABLE_VISUAL_WAR_LAB.txt";

        internal static string ResolveEnablePath(
            string assemblyLocation)
        {
            if (string.IsNullOrWhiteSpace(assemblyLocation))
                return null;

            try
            {
                string binaryDirectory =
                    Path.GetDirectoryName(
                        Path.GetFullPath(assemblyLocation));

                if (string.IsNullOrEmpty(binaryDirectory))
                    return null;

                DirectoryInfo platformDirectory =
                    new DirectoryInfo(binaryDirectory);
                DirectoryInfo binDirectory =
                    platformDirectory.Parent;
                DirectoryInfo moduleDirectory =
                    binDirectory == null
                        ? null
                        : binDirectory.Parent;

                if (binDirectory == null ||
                    moduleDirectory == null ||
                    !string.Equals(
                        binDirectory.Name,
                        "bin",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return Path.Combine(
                    moduleDirectory.FullName,
                    "Data",
                    MarkerFileName);
            }
            catch
            {
                return null;
            }
        }
    }
}