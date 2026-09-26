using System;
using System.IO;
using System.Reflection;

namespace ClanAI
{
    internal static class ModuleRuntimePaths
    {
        internal static string ResolveModuleDirectory(
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

                return moduleDirectory.FullName;
            }
            catch
            {
                return null;
            }
        }

        internal static string Resolve(
            string assemblyLocation,
            params string[] relativeParts)
        {
            string moduleDirectory =
                ResolveModuleDirectory(assemblyLocation);

            if (string.IsNullOrEmpty(moduleDirectory) ||
                relativeParts == null)
            {
                return null;
            }

            try
            {
                string path = moduleDirectory;
                foreach (string part in relativeParts)
                {
                    if (string.IsNullOrWhiteSpace(part) ||
                        Path.IsPathRooted(part) ||
                        part.IndexOf("..", StringComparison.Ordinal) >= 0)
                    {
                        return null;
                    }

                    path = Path.Combine(path, part);
                }

                string fullPath = Path.GetFullPath(path);
                string root = Path.GetFullPath(moduleDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                return fullPath.StartsWith(
                    root,
                    StringComparison.OrdinalIgnoreCase)
                        ? fullPath
                        : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Current(
            params string[] relativeParts)
        {
            return Resolve(
                typeof(ModuleRuntimePaths)
                    .GetTypeInfo().Assembly.Location,
                relativeParts);
        }

        internal static string Data(string fileName)
        {
            return Current("Data", fileName);
        }

        internal static string Log(string fileName)
        {
            return Current("Logs", fileName);
        }

        internal static string LogDirectory(string directoryName)
        {
            return Current("Logs", directoryName);
        }
    }
}

