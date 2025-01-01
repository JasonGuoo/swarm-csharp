using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Xunit;
using Swarm.CSharp.Tests.Helpers;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.Tests.LLM.Providers
{
    public class OllamaClientTests
    {
        private readonly Mock<ILogger<OllamaClient>> _loggerMock;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        public OllamaClientTests()
        {
            EnvLoader.Load();

            _apiKey = EnvLoader.GetEnvVar("OLLAMA_API_KEY", "test-key");
            _model = EnvLoader.GetEnvVar("OLLAMA_MODEL", "llama2");
            _baseUrl = EnvLoader.GetEnvVar("OLLAMA_API_BASE", "http://localhost:11434");

            _loggerMock = new Mock<ILogger<OllamaClient>>();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private OllamaClient CreateClient(HttpMessageHandler handler)
        {
            return new OllamaClient(
                endpoint: _baseUrl,
                apiKey: _apiKey,
                model: _model
            );
        }

        [Fact]
        public async Task ChatAsync_SuccessfulResponse_ReturnsChatResponse()
        {
            // Arrange
            var mockResponse = new ChatResponse(
                id: "test-id",
                objectName: "chat.completion",
                created: (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model: "llama2",
                choices: new List<Choice>
                {
                    new Choice(
                        index: 0,
                        message: new Message { Role = "assistant", Content = "Test response" },
                        finishReason: "stop"
                    )
                },
                usage: new Usage
                {
                    PromptTokens = 10,
                    CompletionTokens = 20,
                    TotalTokens = 30
                }
            );

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse, _jsonOptions))
                });

            var client = CreateClient(mockHandler.Object);
            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Hello" }
                }
            };

            // Act
            var response = await client.ChatAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(mockResponse.Id, response.Id);
            Assert.Equal(mockResponse.Model, response.Model);
            Assert.NotNull(response.Choices);
            Assert.Single(response.Choices);
            Assert.NotNull(response.Choices[0].Message);
            Assert.Equal("Test response", response.Choices[0].Message!.Content);
        }

        [Fact]
        public async Task ChatAsync_ApiError_ThrowsException()
        {
            // Arrange
            var errorResponse = new { error = "Model not found" };
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent(JsonSerializer.Serialize(errorResponse))
                });

            var client = CreateClient(mockHandler.Object);
            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Hello" }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => client.ChatAsync(request));
        }

        [Fact]
        public void Constructor_ValidatesParameters()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OllamaClient(apiKey: null!));
            Assert.Throws<ArgumentException>(() => new OllamaClient(apiKey: string.Empty));

            // Valid construction should not throw
            var client = new OllamaClient("test-key");
            Assert.NotNull(client);
        }
    }
}