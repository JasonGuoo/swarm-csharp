using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Utils;
using Swarm.CSharp.LLM.Models;

namespace Swarm.CSharp.LLM.Providers
{
    public class ChatGLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string DefaultEndpoint = "https://open.bigmodel.cn/api/paas/v3";

        public string Model { get; set; }

        public ChatGLMClient(string apiKey, string model = "glm-4-flash", string endpoint = DefaultEndpoint)
        {
            Model = model;
            _apiKey = apiKey;
            _httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                Logger.LogDebug($"ChatGLM Request: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/chat", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                Logger.LogDebug($"ChatGLM Response: {responseContent}");

                return JsonSerializer.Deserialize<ChatResponse>(responseContent);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error calling ChatGLM API");
                throw;
            }
        }

        public async IAsyncEnumerable<ChatResponse> StreamAsync(ChatRequest request)
        {
            request.Stream = true;
            var json = JsonSerializer.Serialize(request);
            Logger.LogDebug($"ChatGLM Stream Request: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = null;
            Stream stream = null;
            StreamReader reader = null;

            try
            {
                response = await _httpClient.PostAsync("/chat/stream", content);
                response.EnsureSuccessStatusCode();
                stream = await response.Content.ReadAsStreamAsync();
                reader = new StreamReader(stream);
                Logger.LogDebug("ChatGLM Stream initialized successfully");
            }
            catch (Exception e)
            {
                response?.Dispose();
                stream?.Dispose();
                reader?.Dispose();
                Logger.LogError(e, "Error initializing stream from ChatGLM API");
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

                        Logger.LogDebug($"ChatGLM Stream chunk received: {line}");
                        chunk = JsonSerializer.Deserialize<ChatResponse>(line);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error processing stream from ChatGLM API");
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
                throw new Exception("Failed to validate ChatGLM connection", e);
            }
        }
    }
}