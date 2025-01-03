using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Swarm.CSharp.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Swarm.CSharp.Tests.LLM.Providers
{
    public class OllamaClientIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly OllamaClient _client;
        private readonly string _model;

        public OllamaClientIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Load environment variables
            EnvLoader.Load();

            var apiKey = EnvLoader.GetEnvVar("OLLAMA_API_KEY");
            _model = EnvLoader.GetEnvVar("OLLAMA_MODEL");
            var baseUrl = EnvLoader.GetEnvVar("OLLAMA_API_BASE");

            Assert.False(string.IsNullOrEmpty(apiKey), "OLLAMA_API_KEY must be set in .env file");

            // Create logger that writes to test output
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddProvider(new XUnitLoggerProvider(_output));
            });
            var logger = loggerFactory.CreateLogger<OllamaClient>();

            _client = new OllamaClient(apiKey, _model, baseUrl);
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
                }
            };

            // Act
            var response = await _client.ChatAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Choices);
            Assert.Single(response.Choices);
            Assert.NotNull(response.Choices[0].Message.Content);

            _output.WriteLine($"Response: {response.Choices[0].Message.Content}");
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
                Messages = messages
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
                Temperature = (float?)0.7
            };

            _output.WriteLine("=== Test Configuration ===");
            _output.WriteLine($"Model from env: {_model}");
            _output.WriteLine($"Base URL from env: {EnvLoader.GetEnvVar("OLLAMA_API_BASE")}");

            // Act & Assert
            var response = await _client.ChatAsync(request);
            Assert.NotNull(response);
        }
    }
}