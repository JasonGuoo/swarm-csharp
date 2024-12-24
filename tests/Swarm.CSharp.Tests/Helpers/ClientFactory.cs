using Microsoft.Extensions.Logging;
using Swarm.CSharp.LLM;

namespace Swarm.CSharp.Tests.Helpers;

public static class ClientFactory
{
    public static OpenAIClient CreateOpenAIClient<T>(ILogger<T> logger, string? apiKey = null, string? baseUrl = null, string? model = null)
        where T : OpenAIClient
    {
        const float temperature = 0.7f;

        return new OpenAIClient(apiKey ?? "", model, null, baseUrl, temperature, logger as ILogger<OpenAIClient>);
    }

    public static AzureOpenAIClient CreateAzureOpenAIClient<T>(ILogger<T> logger, string? apiKey = null, string? endpoint = null, string? deploymentId = null)
        where T : AzureOpenAIClient
    {
        const string model = "gpt-4";
        const float temperature = 0.7f;

        return new AzureOpenAIClient(endpoint, apiKey, deploymentId, model, temperature, logger as ILogger<AzureOpenAIClient>);
    }

    public static ChatGLMClient CreateChatGLMClient<T>(ILogger<T> logger, string? apiKey = null, string? baseUrl = null, string? model = null)
        where T : ChatGLMClient
    {
        const float temperature = 0.7f;

        return new ChatGLMClient(apiKey, baseUrl, model, temperature, logger as ILogger<ChatGLMClient>);
    }

    public static OllamaClient CreateOllamaClient<T>(ILogger<T> logger, string? baseUrl = null, string? model = null)
        where T : OllamaClient
    {
        model ??= "llama2";
        const float temperature = 0.7f;

        return new OllamaClient(baseUrl, model, temperature, logger as ILogger<OllamaClient>);
    }
}
