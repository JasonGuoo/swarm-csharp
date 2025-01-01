using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Swarm.CSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;
using System.Net.Http;
using System.Reflection;

namespace Swarm.CSharp.Tests.LLM.Providers
{
    public class ChatGLMClientIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ChatGLMClient _client;
        private readonly string _model;

        public ChatGLMClientIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            EnvLoader.Load();

            var apiKey = EnvLoader.GetEnvVar("ZHIPUAI_API_KEY");
            _model = EnvLoader.GetEnvVar("ZHIPUAI_MODEL");
            var baseUrl = EnvLoader.GetEnvVar("ZHIPUAI_API_BASE");

            Assert.False(string.IsNullOrEmpty(apiKey), "ZHIPUAI_API_KEY must be set in .env file");

            _client = new ChatGLMClient(
                apiKey: apiKey,
                model: "glm-4-flash",
                endpoint: baseUrl
            );
        }

        [Fact]
        public async Task ChatAsync_SimpleConversation_ReturnsValidResponse()
        {
            // Arrange
            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Say hello and introduce yourself briefly." }
                },
                Tools = new List<Tool>()
            };

            // Act
            var response = await _client.ChatAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Choices);
            Assert.Single(response.Choices);
            Assert.NotNull(response.Choices[0].Message.Content);
            Assert.Contains("hello", response.Choices[0].Message.Content.ToLower());

            _output.WriteLine($"Response: {response.Choices[0].Message.Content}");
        }

        [Fact]
        public async Task ChatAsync_FunctionCalling_WeatherExample()
        {
            // Arrange
            var functionSchema = new FunctionSchema
            {
                Name = "get_weather",
                Description = "Get the current weather in a given location",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["location"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The city and state, e.g. San Francisco, CA"
                        },
                        ["unit"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["enum"] = new[] { "celsius", "fahrenheit" }
                        }
                    },
                    ["required"] = new[] { "location" }
                }
            };

            var tool = new Tool { Function = functionSchema };

            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "What's the weather like in San Francisco?" }
                },
                Tools = new List<Tool> { tool }
            };

            // Act
            var response = await _client.ChatAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Choices);
            Assert.Single(response.Choices);

            var message = response.Choices[0].Message;
            Assert.NotNull(message.ToolCalls);
            Assert.Single(message.ToolCalls);

            var functionCall = message.ToolCalls[0].Function;
            Assert.Equal("get_weather", functionCall.Name);

            _output.WriteLine($"Function call: {functionCall.Name}");
            _output.WriteLine($"Arguments: {functionCall.Arguments}");
        }

        [Fact]
        public async Task ChatAsync_MultiTurnConversation_MaintainsContext()
        {
            // Arrange
            var messages = new List<Message>
            {
                new() { Role = "user", Content = "My name is Alice." },
                new() { Role = "assistant", Content = "Hello Alice! Nice to meet you. How can I help you today?" },
                new() { Role = "user", Content = "What's my name?" }
            };

            var request = new ChatRequest
            {
                Messages = messages,
                Tools = new List<Tool>()
            };

            // Act
            var response = await _client.ChatAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Choices);
            Assert.Single(response.Choices);
            Assert.Contains("Alice", response.Choices[0].Message.Content);

            _output.WriteLine($"Response: {response.Choices[0].Message.Content}");
        }

        [Fact]
        public async Task ChatAsync_LogFullRequest()
        {
            // Arrange
            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Say hi" }
                },
                Model = _model,
                Temperature = (float?)0.7,
                MaxTokens = 150,
                Tools = new List<Tool>()
            };

            _output.WriteLine("=== Test Configuration ===");
            _output.WriteLine($"Model from env: {_model}");
            _output.WriteLine($"Base URL from env: {EnvLoader.GetEnvVar("ZHIPUAI_API_BASE")}");

            // Act & Assert
            var response = await _client.ChatAsync(request);
            Assert.NotNull(response);
        }
    }
}