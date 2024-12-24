using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Swarm.CSharp.Core;
using Swarm.CSharp.Core.Exceptions;
using Swarm.CSharp.Models.OpenAI;

namespace Swarm.CSharp.LLM;

/// <summary>
/// ChatGLM API client implementation.
/// Provides access to Zhipu AI's ChatGLM models.
/// </summary>
public class ChatGLMClient : BaseLLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatGLMClient"/> class.
    /// </summary>
    /// <param name="apiKey">The ChatGLM API key.</param>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <param name="model">The model to use.</param>
    /// <param name="temperature">The temperature for responses.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="httpClient">Optional HTTP client for testing.</param>
    public ChatGLMClient(
        string apiKey,
        string baseUrl = "https://open.bigmodel.cn/api/paas/v4/",
        string model = "glm-4-flash",
        float temperature = 0.7f,
        ILogger<ChatGLMClient>? logger = null,
        HttpClient? httpClient = null)
        : base(model, temperature, logger)
    {
        _baseUrl = baseUrl;
        _apiKey = apiKey;
        _httpClient = httpClient ?? CreateHttpClient(baseUrl);
    }

    private HttpClient CreateHttpClient(string baseUrl)
    {
        var client = new HttpClient();

        // Ensure the base URL is properly formatted
        var baseUri = baseUrl.TrimEnd('/') + "/";
        if (!baseUri.StartsWith("http://") && !baseUri.StartsWith("https://"))
        {
            baseUri = "https://" + baseUri;
        }
        Logger?.LogDebug($"Creating HttpClient with base URL: {baseUri}");
        client.BaseAddress = new Uri(baseUri);

        // Use API key directly in Authorization header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return client;
    }

    protected virtual string GetEndpointUrl(bool isStreaming = false)
    {
        var baseUrl = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
        return $"{baseUrl}{(isStreaming ? "chat/stream" : "chat/completions")}";
    }

    /// <inheritdoc/>
    public override async Task<IMessage> GetCompletionAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        CancellationToken cancellationToken = default)
    {
        ValidateMessages(messages);

        try
        {
            var request = new
            {
                model = Model,
                messages = messages.Select(m => new 
                {
                    role = m.Role.ToString().ToLower(),
                    content = m.Content
                }).ToList(),
                tools = functions == null ? null : functions.Select(f => new
                {
                    type = "function",
                    function = new
                    {
                        name = f.Name,
                        description = f.Description,
                        parameters = f.Parameters
                    }
                }).ToList(),
                tool_choice = toolChoice switch
                {
                    ToolChoice.None => "none",
                    ToolChoice.Auto => "auto",
                    _ => JsonSerializer.Serialize(new { type = "function", function = new { name = toolChoice.ToString() } })
                },
                stream = false,
                temperature = Temperature
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(GetEndpointUrl(), content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger?.LogError($"ChatGLM API request failed with status {response.StatusCode}");
                Logger?.LogError($"Response content: {errorContent}");
                Logger?.LogError($"Response headers:");
                foreach (var header in response.Headers)
                {
                    Logger?.LogError($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger?.LogDebug("Response JSON: {ResponseJson}", responseJson);
            
            var completion = JsonSerializer.Deserialize<ChatGLMResponse>(responseJson);

            if (completion == null || completion.Choices == null || !completion.Choices.Any())
            {
                throw new ApiException("Invalid or empty response from API");
            }

            var choice = completion.Choices[0];
            if (choice.Message == null)
            {
                throw new ApiException("Message object missing in response");
            }

            // Handle tool calls
            if (choice.FinishReason == "tool_calls" && choice.Message.ToolCalls != null)
            {
                return new Message("assistant", string.Empty, toolCalls: choice.Message.ToolCalls);
            }

            // Handle regular message
            return new Message("assistant", choice.Message.Content ?? string.Empty);
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

        var request = new
        {
            model = Model,
            messages = messages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.Content
            }).ToList(),
            tools = functions == null ? null : functions.Select(f => new
            {
                type = "function",
                function = new
                {
                    name = f.Name,
                    description = f.Description,
                    parameters = f.Parameters
                }
            }).ToList(),
            tool_choice = toolChoice switch
            {
                ToolChoice.None => "none",
                ToolChoice.Auto => "auto",
                _ => JsonSerializer.Serialize(new { type = "function", function = new { name = toolChoice.ToString() } })
            },
            stream = true,
            temperature = Temperature
        };

        var json = JsonSerializer.Serialize(request);
        var requestContent = new StringContent(json, Encoding.UTF8);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(GetEndpointUrl(true), requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"API request failed: {ex.Message}", ex);
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            Logger?.LogDebug("Received line: {Line}", line);
            
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("data: [DONE]"))
            {
                continue;
            }

            var jsonLine = line;
            if (line.StartsWith("data: "))
            {
                jsonLine = line[6..].TrimStart();
                Logger?.LogDebug("Extracted JSON: {JsonLine}", jsonLine);
            }

            if (string.IsNullOrWhiteSpace(jsonLine))
            {
                continue;
            }

            ChatGLMResponse? streamResponse = null;
            try 
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                streamResponse = JsonSerializer.Deserialize<ChatGLMResponse>(jsonLine, options);
                Logger?.LogDebug("Deserialized response: {@Response}", streamResponse);
            }
            catch (JsonException ex)
            {
                // Skip malformed JSON lines
                Logger?.LogDebug(ex, "Failed to deserialize JSON line: {Line}", line);
                continue;
            }

            if (streamResponse?.Choices == null || streamResponse.Choices.Count == 0)
            {
                Logger?.LogDebug("No choices in response");
                continue;
            }

            var choice = streamResponse.Choices[0];
            if (choice.Message?.Content != null)
            {
                Logger?.LogDebug("Yielding message content: {Content}", choice.Message.Content);
                yield return choice.Message.Content;
            }
            else if (choice.Delta?.Content != null)
            {
                Logger?.LogDebug("Yielding delta content: {Content}", choice.Delta.Content);
                yield return choice.Delta.Content;
            }
            else
            {
                Logger?.LogDebug("No content in choice: {@Choice}", choice);
            }
        }
    }

    private class ChatGLMResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatGLMChoice>? Choices { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    private class ChatGLMChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public ChatGLMMessage? Message { get; set; }

        [JsonPropertyName("delta")]
        public ChatGLMDelta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class ChatGLMMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }

    private class ChatGLMDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class Usage
    {
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
