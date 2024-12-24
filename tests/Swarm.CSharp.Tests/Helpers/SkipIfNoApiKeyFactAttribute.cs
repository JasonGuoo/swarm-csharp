using System;
using System.IO;
using DotNetEnv;
using Xunit;

namespace Swarm.CSharp.Tests.Helpers;

public class SkipIfNoApiKeyFactAttribute : FactAttribute
{
    private static bool _envLoaded = false;
    private static readonly object _lock = new object();

    public SkipIfNoApiKeyFactAttribute(string apiKeyName)
    {
        lock (_lock)
        {
            if (!_envLoaded)
            {
                var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
                var envPath = Path.Combine(projectRoot, "../.env");
                
                if (File.Exists(envPath))
                {
                    Env.Load(envPath);
                    Console.WriteLine($"Loaded environment variables from {envPath}");
                }
                _envLoaded = true;
            }
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(apiKeyName)))
        {
            Skip = $"{apiKeyName} environment variable not set";
        }
    }
}
