using System;
using System.IO;

namespace Swarm.CSharp.Utils
{
    public static class EnvLoader
    {
        public static void Load(string filePath = ".env")
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var fullPath = Path.GetFullPath(filePath);

            Logger.LogDebug($"Current Directory: {currentDirectory}");
            Logger.LogDebug($"Trying to load .env from: {fullPath}");

            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"Warning: {filePath} not found, using default values");
                // Try to load from solution root
                var solutionRoot = Path.GetFullPath(Path.Combine(currentDirectory, "../../../../../"));
                var solutionEnvPath = Path.Combine(solutionRoot, filePath);
                Logger.LogDebug($"Trying solution root: {solutionEnvPath}");

                if (File.Exists(solutionEnvPath))
                {
                    filePath = solutionEnvPath;
                    Logger.LogInformation($"Found .env in solution root: {filePath}");
                }
                else
                {
                    return;
                }
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                var parts = trimmedLine.Split('=', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim().Trim('"', '\'');
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public static string GetEnvVar(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }
    }
}