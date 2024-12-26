using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.LLM.Models;

namespace Swarm.CSharp.LLM.Providers
{
    public class OpenAIClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIClient>? _logger;
        private readonly string _defaultModel;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly double? _defaultTemperature;
        private readonly int? _defaultMaxTokens;
        private readonly string _baseUrl;

        public OpenAIClient(
            string apiKey,
            string model = "gpt-4o-mini",
            string baseUrl = "https://api.openai.com/v1",
            string? organizationId = null,
            double? temperature = null,
            int? maxTokens = null,
            HttpClient? httpClient = null,
            ILogger<OpenAIClient>? logger = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(apiKey);

            _logger = logger;
            _defaultModel = model;
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            if (!string.IsNullOrEmpty(organizationId))
            {
                _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", organizationId);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _defaultTemperature = temperature;
            _defaultMaxTokens = maxTokens;
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request)
        {
            try
            {
                // Log initial state
                _logger?.LogInformation("=== Request Details ===");
                _logger?.LogInformation("Base URL: {BaseUrl}", _baseUrl);
                _logger?.LogInformation("Default Model: {Model}", _defaultModel);

                // Log request parameters
                if (string.IsNullOrEmpty(request.Model))
                {
                    request.Model = _defaultModel;
                    _logger?.LogInformation("Using default model: {Model}", _defaultModel);
                }
                if (!request.Temperature.HasValue)
                {
                    request.Temperature = _defaultTemperature;
                    _logger?.LogInformation("Using default temperature: {Temperature}", _defaultTemperature);
                }
                if (!request.MaxTokens.HasValue)
                {
                    request.MaxTokens = _defaultMaxTokens;
                    _logger?.LogInformation("Using default max tokens: {MaxTokens}", _defaultMaxTokens);
                }

                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                _logger?.LogInformation("Request Body: {Request}", requestJson);

                // Log all request headers
                _logger?.LogInformation("Default Headers:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    _logger?.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }

                // Create complete URL
                var completeUrl = $"{_baseUrl}/chat/completions";

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, completeUrl)
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
                    Headers =
                    {
                        Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
                    }
                };

                // Log final request details
                _logger?.LogInformation("=== Final Request ===");
                _logger?.LogInformation("Method: {Method}", httpRequest.Method);
                _logger?.LogInformation("Complete URL: {Url}", completeUrl);
                _logger?.LogInformation("Content Headers:");
                foreach (var header in httpRequest.Content!.Headers)
                {
                    _logger?.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
                _logger?.LogInformation("Request Headers:");
                foreach (var header in httpRequest.Headers)
                {
                    _logger?.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger?.LogInformation("Response Status: {Status}", response.StatusCode);
                _logger?.LogInformation("Response Headers:");
                foreach (var header in response.Headers)
                {
                    _logger?.LogInformation("{Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
                _logger?.LogInformation("Response Content: {Response}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"OpenAI API returned {response.StatusCode}: {responseContent}");
                }

                return JsonSerializer.Deserialize<ChatResponse>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OpenAI chat completion");
                throw;
            }
        }

        public async Task<Stream> ChatStreamAsync(ChatRequest request)
        {
            try
            {
                request.Stream = true;
                if (string.IsNullOrEmpty(request.Model))
                {
                    request.Model = _defaultModel;
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(request, _jsonOptions),
                        Encoding.UTF8,
                        "application/json"),
                    Headers =
                    {
                        Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
                    }
                };

                var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in OpenAI chat stream");
                throw;
            }
        }
    }
}