using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.Models.OpenAI;

namespace Swarm.CSharp.LLM;

/// <summary>
/// Azure OpenAI Service client implementation.
/// Provides access to OpenAI models hosted on Azure.
/// </summary>
public class AzureOpenAIClient : OpenAIClient
{
    private readonly string _deploymentId;
    private readonly string _apiVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIClient"/> class.
    /// </summary>
    /// <param name="endpoint">The Azure OpenAI endpoint URL.</param>
    /// <param name="apiKey">The Azure OpenAI API key.</param>
    /// <param name="deploymentId">The deployment ID.</param>
    /// <param name="model">The model to use.</param>
    /// <param name="temperature">The temperature for responses.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="httpClient">Optional HTTP client for testing.</param>
    public AzureOpenAIClient(
        string endpoint,
        string apiKey,
        string deploymentId,
        string model = "gpt-4",
        float temperature = 0.7f,
        ILogger<AzureOpenAIClient>? logger = null,
        HttpClient? httpClient = null)
        : base(endpoint?.TrimEnd('/') + "/" ?? throw new ArgumentNullException(nameof(endpoint)), 
               apiKey ?? throw new ArgumentNullException(nameof(apiKey)), 
               null, 
               model ?? throw new ArgumentNullException(nameof(model)), 
               temperature, 
               logger, 
               httpClient)
    {
        _deploymentId = deploymentId ?? throw new ArgumentNullException(nameof(deploymentId));
        _apiVersion = "2023-12-01-preview";
    }

    /// <inheritdoc/>
    protected override string GetEndpointUrl(bool isStreaming = false) =>
        $"openai/deployments/{_deploymentId}/chat/completions?api-version={_apiVersion}";

    /// <inheritdoc/>
    protected override HttpClient CreateHttpClient(string baseUrl)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        client.DefaultRequestHeaders.Add("api-key", ApiKey);
        return client;
    }
}
