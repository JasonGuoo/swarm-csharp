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
using Swarm.CSharp.LLM.Helpers;

namespace Swarm.CSharp.LLM.Providers
{
    public class OllamaClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaClient>? _logger;
        private readonly string _defaultModel;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _baseUrl;

        public OllamaClient(
            string apiKey,
            string model = "llama2",
            string baseUrl = "http://localhost:11434",
            HttpClient? httpClient = null,
            ILogger<OllamaClient>? logger = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(apiKey);

            _logger = logger;
            _defaultModel = model;
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _logger?.LogInformation("Initializing Ollama client with API key: {ApiKey}", Utils.MaskApiKey(apiKey));
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request)
        {
            try
            {
                // Log initial state
                _logger?.LogInformation("=== Request Details ===");
                _logger?.LogInformation("Base URL: {BaseUrl}", _baseUrl);
                _logger?.LogInformation("Default Model: {Model}", _defaultModel);

                if (string.IsNullOrEmpty(request.Model))
                {
                    request.Model = _defaultModel;
                    _logger?.LogInformation("Using default model: {Model}", _defaultModel);
                }

                // Convert to Ollama format
                var ollamaRequest = new
                {
                    model = request.Model,
                    messages = request.Messages,
                    stream = request.Stream,
                    options = new
                    {
                        temperature = request.Temperature ?? 0.7
                    }
                };

                var requestJson = JsonSerializer.Serialize(ollamaRequest, _jsonOptions);
                _logger?.LogInformation("Request Body: {Request}", requestJson);

                var completeUrl = $"{_baseUrl}/api/chat";
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

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Ollama API returned {response.StatusCode}: {responseContent}");
                }

                return JsonSerializer.Deserialize<ChatResponse>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in Ollama chat completion");
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

                var completeUrl = $"{_baseUrl}/api/chat";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, completeUrl)
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
                _logger?.LogError(ex, "Error in Ollama chat stream");
                throw;
            }
        }
    }
}