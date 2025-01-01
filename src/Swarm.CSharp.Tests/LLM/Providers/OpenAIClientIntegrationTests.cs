using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Swarm.CSharp.Utils;
using Xunit;
using Xunit.Abstractions;
using System.Net.Http;
using System.Reflection;

namespace Swarm.CSharp.Tests.LLM.Providers
{
    public class OpenAIClientIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly OpenAIClient _client;
        private readonly string _model;

        public OpenAIClientIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Load environment variables
            EnvLoader.Load();

            var apiKey = EnvLoader.GetEnvVar("OPENAI_API_KEY");
            _model = EnvLoader.GetEnvVar("OPENAI_MODEL");
            var baseUrl = EnvLoader.GetEnvVar("OPENAI_BASE_URL");

            Assert.False(string.IsNullOrEmpty(apiKey), "OPENAI_API_KEY must be set in .env file");

            // Create logger that writes to test output
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddProvider(new XUnitLoggerProvider(_output));
            });
            var logger = loggerFactory.CreateLogger<OpenAIClient>();

            _client = new OpenAIClient(apiKey, _model, baseUrl);
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
        public async Task ChatAsync_ComplexFunctionCalling_EmailExample()
        {
            // Arrange
            var tool = new Tool
            {
                Function = new FunctionSchema
                {
                    Name = "send_email",
                    Description = "Send an email to a recipient",
                    Parameters = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["to"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "Email address of the recipient"
                            },
                            ["subject"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "Subject line of the email"
                            },
                            ["body"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "Content of the email"
                            },
                            ["priority"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["enum"] = new[] { "high", "normal", "low" }
                            }
                        },
                        ["required"] = new[] { "to", "subject", "body" }
                    }
                }
            };

            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Send an urgent email to bob@example.com about the server being down." }
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
            Assert.Equal("send_email", functionCall.Name);

            _output.WriteLine($"Function call: {functionCall.Name}");
            _output.WriteLine($"Arguments: {functionCall.Arguments}");
        }

        [Fact]
        public async Task ChatAsync_LogRequestResponse()
        {
            // Arrange
            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Say hi" }
                },
                Tools = new List<Tool>()
            };

            // Log request headers
            _output.WriteLine("Request Headers:");
            foreach (var header in _client.GetType()
                .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(_client) as HttpClient
                is HttpClient client ? client.DefaultRequestHeaders : null)
            {
                _output.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // Act
            var response = await _client.ChatAsync(request);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task ChatAsync_BasicTest()
        {
            // Arrange
            var request = new ChatRequest
            {
                Messages = new List<Message>
                {
                    new() { Role = "user", Content = "Say hi" }
                },
                Tools = new List<Tool>()
            };

            var baseAddress = (_client.GetType()
                .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(_client) as HttpClient)?.BaseAddress;

            _output.WriteLine($"Using Model: {_model}");
            _output.WriteLine($"Using Base URL: {baseAddress}");

            // Act & Assert
            var response = await _client.ChatAsync(request);
            Assert.NotNull(response);
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
                Model = "openai/gpt-4o-mini",
                Temperature = (float?)0.7,
                MaxTokens = 150,
                Tools = new List<Tool>()
            };

            _output.WriteLine("=== Test Configuration ===");
            _output.WriteLine($"Model from env: {_model}");
            _output.WriteLine($"Base URL from env: {EnvLoader.GetEnvVar("OPENAI_BASE_URL")}");

            // Act & Assert
            var response = await _client.ChatAsync(request);
            Assert.NotNull(response);
        }
    }

    // Logger provider for xUnit test output
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
            => new XUnitLogger(_testOutputHelper, categoryName);

        public void Dispose() { }
    }

    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = $"{_categoryName} [{logLevel}] {formatter(state, exception)}";
            if (exception != null)
                message += $"\n{exception}";
            _testOutputHelper.WriteLine(message);
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}