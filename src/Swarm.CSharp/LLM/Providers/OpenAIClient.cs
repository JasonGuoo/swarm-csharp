using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Swarm.CSharp.LLM.Helpers;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.Utils;


namespace Swarm.CSharp.LLM.Providers;

public class OpenAIClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string DefaultBaseUrl = "https://api.openai.com/v1";

    public string Model { get; set; }

    public OpenAIClient(string apiKey, string model = "gpt-3.5-turbo", string? baseUrl = null)
    {
        Model = model;
        _apiKey = apiKey;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl ?? DefaultBaseUrl) };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        try
        {
            // Set default values if not specified
            request.Model ??= Model;
            request.Stream ??= false;
            request.Temperature ??= 0.7f;
            request.MaxTokens ??= 8192;

            var json = JsonSerializer.Serialize(request);
            Logger.LogDebug($"OpenAI Request: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var fullUrl = Utilities.CombineUrls(_httpClient.BaseAddress.ToString(), "chat/completions");
            Logger.LogDebug($"Full URL: {fullUrl}");
            var response = await _httpClient.PostAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            Logger.LogDebug($"OpenAI Response: {responseContent}");

            return JsonSerializer.Deserialize<ChatResponse>(responseContent);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error calling OpenAI API");
            throw;
        }
    }

    public async IAsyncEnumerable<ChatResponse> StreamAsync(ChatRequest request)
    {
        request.Stream = true;
        var json = JsonSerializer.Serialize(request);
        Logger.LogDebug($"OpenAI Stream Request: {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = null;
        Stream stream = null;
        StreamReader reader = null;

        try
        {
            response = await _httpClient.PostAsync("/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();
            stream = await response.Content.ReadAsStreamAsync();
            reader = new StreamReader(stream);
            Logger.LogDebug("OpenAI Stream initialized successfully");
        }
        catch (Exception e)
        {
            response?.Dispose();
            stream?.Dispose();
            reader?.Dispose();
            Logger.LogError(e, "Error initializing stream from OpenAI API");
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
                    if (string.IsNullOrEmpty(line) || line == "data: [DONE]") continue;

                    if (line.StartsWith("data: "))
                    {
                        line = line.Substring(6);
                        Logger.LogDebug($"OpenAI Stream chunk received: {line}");
                        chunk = JsonSerializer.Deserialize<ChatResponse>(line);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error processing stream from OpenAI API");
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
            throw new Exception("Failed to validate OpenAI connection", e);
        }
    }
}