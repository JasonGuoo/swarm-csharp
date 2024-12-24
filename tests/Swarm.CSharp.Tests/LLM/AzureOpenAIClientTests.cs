using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.Core.Exceptions;
using Swarm.CSharp.LLM;
using Xunit;
using Xunit.Abstractions;
using DotNetEnv;
using Swarm.CSharp.Tests.Helpers;

namespace Swarm.CSharp.Tests.LLM;

[Collection("Sequential")]
[Trait("Category", "Integration")]
public class AzureOpenAIClientTests : IAsyncLifetime
{
    private readonly AzureOpenAIClient _client;
    private readonly ILogger<AzureOpenAIClient> _logger;
    private readonly ITestOutputHelper _output;
    private const string AZURE_OPENAI_API_KEY = "AZURE_OPENAI_API_KEY";
    private const string AZURE_OPENAI_ENDPOINT = "AZURE_OPENAI_ENDPOINT";
    private const string AZURE_OPENAI_DEPLOYMENT_ID = "AZURE_OPENAI_DEPLOYMENT_ID";
    private const string AZURE_OPENAI_MODEL = "gpt-4";

    public AzureOpenAIClientTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Debug)
                .AddFilter("System", LogLevel.Debug)
                .AddFilter("Swarm.CSharp", LogLevel.Debug)
                .AddXUnit(output);
        });
        _logger = loggerFactory.CreateLogger<AzureOpenAIClient>();

        // Load environment variables from .env file if it exists
        var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var envPath = Path.Combine(projectRoot, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        
        // Get configuration from environment variables
        var apiKey = Environment.GetEnvironmentVariable(AZURE_OPENAI_API_KEY);
        var endpoint = Environment.GetEnvironmentVariable(AZURE_OPENAI_ENDPOINT);
        var deploymentId = Environment.GetEnvironmentVariable(AZURE_OPENAI_DEPLOYMENT_ID);

        // Print environment variables
        _output.WriteLine("\nEnvironment Variables:");
        _output.WriteLine($"{AZURE_OPENAI_API_KEY}: {(string.IsNullOrEmpty(apiKey) ? "not set" : "set")}");
        _output.WriteLine($"{AZURE_OPENAI_ENDPOINT}: {endpoint ?? "not set"}");
        _output.WriteLine($"{AZURE_OPENAI_DEPLOYMENT_ID}: {deploymentId ?? "not set"}");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentId))
        {
            _output.WriteLine($"\nWARNING: One or more required environment variables not set. All tests will be skipped.");
            _output.WriteLine($"Required variables: {AZURE_OPENAI_API_KEY}, {AZURE_OPENAI_ENDPOINT}, {AZURE_OPENAI_DEPLOYMENT_ID}");
            return;
        }
        
        _client = new AzureOpenAIClient(endpoint, apiKey, deploymentId, AZURE_OPENAI_MODEL, 0.7f, _logger);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkipIfNoApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task GetCompletionAsync_SimplePrompt_ReturnsResponse()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("user", "What is your name?")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
        Assert.Equal("assistant", response.Role);
    }

    [SkipIfNoApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task GetCompletionAsync_NullMessages_ThrowsException()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.GetCompletionAsync(null));
    }

    [SkipIfNoApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task GetCompletionAsync_EmptyMessages_ThrowsException()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetCompletionAsync(new List<IMessage>()));
    }

    [SkipIfNoApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task GetCompletionAsync_InvalidApiKey_ThrowsException()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var endpoint = Environment.GetEnvironmentVariable(AZURE_OPENAI_ENDPOINT);
        var deploymentId = Environment.GetEnvironmentVariable(AZURE_OPENAI_DEPLOYMENT_ID);
        var client = new AzureOpenAIClient(endpoint, "invalid_api_key", deploymentId, AZURE_OPENAI_MODEL, 0.7f, _logger);
        var messages = new List<IMessage>
        {
            new Message("user", "What is your name?")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => client.GetCompletionAsync(messages));
    }

    [Fact(Skip = "This test is too expensive to run regularly")]
    public async Task GetCompletionAsync_WithLongConversation_MaintainsContext()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("user", "Let's play a game. I want you to remember a number between 1 and 10. Don't tell me the number, just confirm you've chosen one."),
            new Message("assistant", "I have chosen a number between 1 and 10 and will keep it in mind."),
            new Message("user", "Is your number greater than 5?")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
        Assert.Equal("assistant", response.Role);
        Assert.True(response.Content.ToLower().Contains("yes") || response.Content.ToLower().Contains("no"));
    }
}
