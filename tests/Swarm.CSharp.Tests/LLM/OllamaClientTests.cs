using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.Core.Exceptions;
using Swarm.CSharp.LLM;
using Xunit;
using Xunit.Abstractions;

namespace Swarm.CSharp.Tests.LLM;

[Collection("Sequential")]
[Trait("Category", "Integration")]
public class OllamaClientTests : IAsyncLifetime
{
    private readonly OllamaClient _client;
    private readonly ILogger<OllamaClient> _logger;
    private readonly ITestOutputHelper _output;
    private const string DEFAULT_BASE_URL = "http://localhost:11434";
    private const string DEFAULT_MODEL = "phi";

    public OllamaClientTests(ITestOutputHelper output)
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
        _logger = loggerFactory.CreateLogger<OllamaClient>();

        // Print environment variables
        _output.WriteLine("\nUsing default Ollama configuration:");
        _output.WriteLine($"Base URL: {DEFAULT_BASE_URL}");
        _output.WriteLine($"Model: {DEFAULT_MODEL}");
        
        _client = new OllamaClient(DEFAULT_BASE_URL, DEFAULT_MODEL, 0.7f, _logger);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetCompletionAsync_SimplePrompt_ReturnsResponse()
    {
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

    [Fact]
    public async Task GetCompletionAsync_NullMessages_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.GetCompletionAsync(null));
    }

    [Fact]
    public async Task GetCompletionAsync_EmptyMessages_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetCompletionAsync(new List<IMessage>()));
    }

    [Fact]
    public async Task GetCompletionAsync_InvalidBaseUrl_ThrowsException()
    {
        // Arrange
        var client = new OllamaClient("http://invalid-url:11434", DEFAULT_MODEL, 0.7f, _logger);
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
