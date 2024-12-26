using System;

namespace Swarm.CSharp.LLM.Helpers
{
    public static class Utils
    {
        public static string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return string.Empty;

            return apiKey.Length <= 4
                ? apiKey
                : $"{apiKey[..4]}...";
        }

        // Other utility methods can be added here
    }
}