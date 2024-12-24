using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.Core.Exceptions;
using Swarm.CSharp.Models.Ollama;
using Swarm.CSharp.Models.OpenAI;

namespace Swarm.CSharp.LLM;

/// <summary>
/// Ollama API client implementation.
/// Provides access to locally hosted LLM models through Ollama.
/// </summary>
public class OllamaClient : BaseLLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _modelValidated;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaClient"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <param name="model">The model to use.</param>
    /// <param name="temperature">The temperature for responses.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="httpClient">Optional HTTP client for testing.</param>
    public OllamaClient(
        string baseUrl = "http://localhost:11434/",
        string model = "llama2",
        float temperature = 0.7f,
        ILogger<OllamaClient>? logger = null,
        HttpClient? httpClient = null)
        : base(model, temperature, logger)
    {
        _baseUrl = baseUrl;
        _httpClient = httpClient ?? CreateHttpClient(baseUrl);
    }

    private HttpClient CreateHttpClient(string baseUrl)
    {
        return new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
    }

    private async Task ValidateModelAsync(CancellationToken cancellationToken)
    {
        if (_modelValidated)
        {
            return;
        }

        _modelValidated = true;
    }

    /// <inheritdoc/>
    public override async Task<IMessage> GetCompletionAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        CancellationToken cancellationToken = default)
    {
        ValidateMessages(messages);
        await ValidateModelAsync(cancellationToken);

        try
        {
            var request = new
            {
                model = Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                stream = false,
                options = new
                {
                    temperature = Temperature
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/chat", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (!completion.TryGetProperty("message", out var message))
            {
                throw new ApiException("Invalid response from Ollama");
            }

            return new Message
            {
                Role = message.GetProperty("role").GetString() ?? "assistant",
                Content = message.GetProperty("content").GetString() ?? string.Empty
            };
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"API request failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<string> GetStreamingCompletionTokensAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateMessages(messages);
        await ValidateModelAsync(cancellationToken);

        var request = new
        {
            model = Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            stream = true,
            options = new
            {
                temperature = Temperature
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var streamResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(line);
            if (streamResponse?.Choices == null || !streamResponse.Choices.Any())
            {
                continue;
            }

            var choice = streamResponse.Choices[0];
            var tokenContent = choice.Message.Content;
            if (tokenContent != null)
            {
                yield return tokenContent;
            }
        }
    }
}
