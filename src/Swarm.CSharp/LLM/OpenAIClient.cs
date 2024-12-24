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
using Swarm.CSharp.Models.OpenAI;

namespace Swarm.CSharp.LLM;

/// <summary>
/// OpenAI API client implementation.
/// </summary>
public class OpenAIClient : BaseLLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    /// <summary>
    /// Gets the API key used for authentication.
    /// </summary>
    protected string ApiKey { get; }

    /// <summary>
    /// Gets the organization ID.
    /// </summary>
    protected string? Organization { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIClient"/> class.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="organization">Optional organization ID.</param>
    /// <param name="model">The model to use.</param>
    /// <param name="baseUrl">Optional base URL for the API.</param>
    /// <param name="temperature">The temperature for responses.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="httpClient">Optional HTTP client for testing.</param>
    public OpenAIClient(
        string apiKey,
        string? model = null,
        string? organization = null,
        string? baseUrl = null,
        float temperature = 0.7f,
        ILogger<OpenAIClient>? logger = null,
        HttpClient? httpClient = null)
        : base(model ?? "gpt-4", temperature, logger)
    {
        _baseUrl = baseUrl ?? "https://api.openai.com/v1/";
        ApiKey = apiKey;
        Organization = organization;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        if (!string.IsNullOrEmpty(Organization))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", Organization);
        }
    }

    /// <summary>
    /// Gets the endpoint URL for the API.
    /// </summary>
    /// <param name="isStreaming">Whether the request is for streaming.</param>
    /// <returns>The endpoint URL.</returns>
    protected virtual string GetEndpointUrl(bool isStreaming = false)
    {
        var baseUrl = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
        return $"{baseUrl}chat/completions";
    }

    /// <summary>
    /// Creates an HTTP client with the appropriate headers.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <returns>The configured HTTP client.</returns>
    protected virtual HttpClient CreateHttpClient(string baseUrl)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        if (!string.IsNullOrEmpty(Organization))
        {
            client.DefaultRequestHeaders.Add("OpenAI-Organization", Organization);
        }

        return client;
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
            var request = new ChatCompletionRequest
            {
                Model = Model,
                Messages = messages.Select(m => new ChatCompletionMessage
                {
                    Role = m.Role.ToString(),
                    Content = m.Content,
                    Name = m.Name,
                    FunctionCall = m.FunctionCall != null ? new ChatCompletionFunctionCall 
                    { 
                        Name = m.FunctionCall.Name,
                        Arguments = m.FunctionCall.Arguments
                    } : null,
                    ToolCalls = m.ToolCalls?.Select(t => new ChatCompletionToolCall
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Function = new ChatCompletionFunction
                        {
                            Name = t.Name,
                            Arguments = t.Arguments
                        }
                    }).ToList()
                }).ToList(),
                Functions = functions?.Select(f => new ChatCompletionFunction
                {
                    Name = f.Name,
                    Description = f.Description,
                    Parameters = f.Parameters
                }).ToList(),
                Temperature = Temperature,
                Stream = false,
                ToolChoice = toolChoice
            };

            var json = JsonSerializer.Serialize(request);
            _logger?.LogInformation("Request to OpenAI API: {Request}", json);
            _logger?.LogInformation("Endpoint URL: {Url}", GetEndpointUrl());

            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(GetEndpointUrl(), content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogInformation("Response from OpenAI API: {Response}", responseJson);
            _logger?.LogInformation("Response Status Code: {StatusCode}", response.StatusCode);
            _logger?.LogInformation("Response Headers: {Headers}", string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));

            response.EnsureSuccessStatusCode();

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);

            if (completion?.Choices == null || !completion.Choices.Any())
            {
                throw new ApiException("No completion choices returned");
            }

            var choice = completion.Choices[0];
            var message = new Message(choice.Message.Role, choice.Message.Content ?? string.Empty);

            if (choice.Message.ToolCalls != null && choice.Message.ToolCalls.Any())
            {
                var toolCall = choice.Message.ToolCalls[0];
                message.FunctionCall = new ChatCompletionFunctionCall
                {
                    Name = toolCall.Function.Name,
                    Arguments = toolCall.Function.Arguments
                };
            }

            return message;
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

        var request = new ChatCompletionRequest
        {
            Model = Model,
            Messages = messages.Select(m => new ChatCompletionMessage
            {
                Role = m.Role.ToString(),
                Content = m.Content,
                Name = m.Name,
                FunctionCall = m.FunctionCall != null ? new ChatCompletionFunctionCall 
                { 
                    Name = m.FunctionCall.Name,
                    Arguments = m.FunctionCall.Arguments
                } : null,
                ToolCalls = m.ToolCalls?.Select(t => new ChatCompletionToolCall
                {
                    Id = t.Id,
                    Type = t.Type,
                    Function = new ChatCompletionFunction
                    {
                        Name = t.Name,
                        Arguments = t.Arguments
                    }
                }).ToList()
            }).ToList(),
            Functions = functions?.Select(f => new ChatCompletionFunction
            {
                Name = f.Name,
                Description = f.Description,
                Parameters = f.Parameters
            }).ToList(),
            Temperature = Temperature,
            Stream = true,
            ToolChoice = toolChoice
        };

        var json = JsonSerializer.Serialize(request);
        _logger?.LogInformation("Request to OpenAI API: {Request}", json);
        _logger?.LogInformation("Endpoint URL: {Url}", GetEndpointUrl(true));

        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _httpClient.PostAsync(GetEndpointUrl(true), content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger?.LogInformation("Response from OpenAI API: {Response}", responseJson);
        _logger?.LogInformation("Response Status Code: {StatusCode}", response.StatusCode);
        _logger?.LogInformation("Response Headers: {Headers}", string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));

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
