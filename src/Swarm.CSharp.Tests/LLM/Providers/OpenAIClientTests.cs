using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Xunit;
using Swarm.CSharp.Tests.Helpers;

namespace Swarm.CSharp.Tests.LLM.Providers
{
    public class OpenAIClientTests
    {
        private readonly Mock<ILogger<OpenAIClient>> _loggerMock;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        public OpenAIClientTests()
        {
            // Load environment variables
            EnvLoader.Load();

            _apiKey = EnvLoader.GetEnvVar("OPENAI_API_KEY", "test-key");
            _model = EnvLoader.GetEnvVar("OPENAI_MODEL", "gpt-4o-mini");
            _baseUrl = EnvLoader.GetEnvVar("OPENAI_BASE_URL", "https://api.openai.com/v1");

            _loggerMock = new Mock<ILogger<OpenAIClient>>();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private OpenAIClient CreateClient(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };
            return new OpenAIClient(_apiKey, _model, httpClient: httpClient, logger: _loggerMock.Object);
        }

        [Fact]
        public async Task ChatAsync_SuccessfulResponse_ReturnsChatResponse()
        {
            // Arrange
            var mockResponse = new ChatResponse
            {
                Id = "test-id",
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = "gpt-4",
                Choices = new List<ChatResponse.Choice>
                {
                    new()
                    {
                        Index = 0,
                        Message = new Message { Role = "assistant", Content = "Test response" },
                        FinishReason = "stop"
                    }
                },
                Usage = new Usage
                {
                    PromptTokens = 10,
                    CompletionTokens = 20,
                    TotalTokens = 30
                }
            };

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
            Assert.Single(response.Choices);
            Assert.Equal("Test response", response.Choices[0].Message.Content);
        }

        [Fact]
        public async Task ChatAsync_ApiError_ThrowsException()
        {
            // Arrange
            var errorResponse = new { error = new { message = "Invalid API key", type = "invalid_request_error" } };
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
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
        public async Task Constructor_ValidatesParameters()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OpenAIClient(apiKey: null!));
            Assert.Throws<ArgumentException>(() => new OpenAIClient(apiKey: string.Empty));

            // Valid construction should not throw
            var client = new OpenAIClient("test-key");
            Assert.NotNull(client);
        }

        [Fact]
        public async Task ChatAsync_SetsDefaultModel_WhenModelNotProvided()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            HttpRequestMessage? capturedRequest = null;

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
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
            await client.ChatAsync(request);

            // Assert
            Assert.NotNull(capturedRequest);
            var content = await capturedRequest.Content!.ReadAsStringAsync();
            var requestObj = JsonSerializer.Deserialize<ChatRequest>(content, _jsonOptions);
            Assert.Equal("gpt-4o-mini", requestObj?.Model);
        }
    }
}