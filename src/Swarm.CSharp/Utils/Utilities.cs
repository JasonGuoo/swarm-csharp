using System;

namespace Swarm.CSharp.Utils
{
    public static class Utilities
    {
        public static string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return string.Empty;

            return apiKey.Length <= 4
                ? apiKey
                : $"{apiKey[..4]}...";
        }

        public static string CombineUrls(string baseUrl, string path)
        {
            // Remove trailing slash from base URL if present
            baseUrl = baseUrl.TrimEnd('/');

            // Remove leading slash from path if present
            path = path.TrimStart('/');

            // Combine with a single slash
            return $"{baseUrl}/{path}";
        }

        // Other utility methods can be added here
    }
}