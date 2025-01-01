using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.Utils;

namespace Swarm.CSharp.LLM.Providers;

public class OllamaClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private const string DefaultEndpoint = "http://localhost:11434";

    public string Model { get; set; }

    public OllamaClient(string? apiKey = null, string model = "llama3", string endpoint = DefaultEndpoint)
    {
        Model = model;
        _apiKey = apiKey;
        _httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            Logger.LogDebug($"Ollama Request: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            Logger.LogDebug($"Ollama Response: {responseContent}");

            return JsonSerializer.Deserialize<ChatResponse>(responseContent);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error calling Ollama API");
            throw;
        }
    }

    public async IAsyncEnumerable<ChatResponse> StreamAsync(ChatRequest request)
    {
        request.Stream = true;
        var json = JsonSerializer.Serialize(request);
        Logger.LogDebug($"Ollama Stream Request: {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = null;
        Stream stream = null;
        StreamReader reader = null;

        try
        {
            response = await _httpClient.PostAsync("/api/chat/stream", content);
            response.EnsureSuccessStatusCode();
            stream = await response.Content.ReadAsStreamAsync();
            reader = new StreamReader(stream);
            Logger.LogDebug("Ollama Stream initialized successfully");
        }
        catch (Exception e)
        {
            response?.Dispose();
            stream?.Dispose();
            reader?.Dispose();
            Logger.LogError(e, "Error initializing stream from Ollama API");
            throw;
        }

        using (response)
        using (stream)
        using (reader)
        {
            while (!reader.EndOfStream)
            {
                string line = null;
                ChatResponse chunk = null;

                try
                {
                    line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    Logger.LogDebug($"Ollama Stream chunk received: {line}");
                    chunk = JsonSerializer.Deserialize<ChatResponse>(line);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error processing stream from Ollama API");
                    throw;
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }

    public async Task ValidateConnectionAsync()
    {
        try
        {
            var request = new ChatRequest
            {
                Model = Model,
                Messages = new List<Message> { new Message { Role = "user", Content = "test" } }
            };
            await ChatAsync(request);
        }
        catch (Exception e)
        {
            throw new Exception("Failed to validate Ollama connection", e);
        }
    }
}