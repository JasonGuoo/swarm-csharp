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
public class OpenAIClientTests : IAsyncLifetime
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIClient> _logger;
    private readonly ITestOutputHelper _output;
    private const string OPENAI_API_KEY = "OPENAI_API_KEY";
    private const string OPENAI_BASE_URL = "OPENAI_BASE_URL";
    private const string OPENAI_MODEL = "OPENAI_MODEL";

    public OpenAIClientTests(ITestOutputHelper output)
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
        _logger = loggerFactory.CreateLogger<OpenAIClient>();

        // Load environment variables from .env file if it exists
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        Console.WriteLine($"Base directory: {baseDirectory}");
        _output.WriteLine($"Base directory: {baseDirectory}");
        
        var projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
        Console.WriteLine($"Project root: {projectRoot}");
        _output.WriteLine($"Project root: {projectRoot}");
        
        var envPath = Path.Combine(projectRoot, "../.env");
        Console.WriteLine($"Looking for .env file at: {envPath}");
        _output.WriteLine($"Looking for .env file at: {envPath}");
        
        if (File.Exists(envPath))
        {
            Console.WriteLine(".env file found, loading...");
            _output.WriteLine(".env file found, loading...");
            Env.Load(envPath);
            
            // Print all environment variables we care about
            var envVars = new[] { OPENAI_API_KEY, OPENAI_BASE_URL, OPENAI_MODEL };
            foreach (var key in envVars)
            {
                var value = Environment.GetEnvironmentVariable(key);
                Console.WriteLine($"{key}: {(string.IsNullOrEmpty(value) ? "not set" : "set")}");
                _output.WriteLine($"{key}: {(string.IsNullOrEmpty(value) ? "not set" : "set")}");
            }
        }
        else 
        {
            Console.WriteLine(".env file not found");
            _output.WriteLine(".env file not found");
        }
        
        // Get configuration from environment variables
        var apiKey = Environment.GetEnvironmentVariable(OPENAI_API_KEY);
        var baseUrl = Environment.GetEnvironmentVariable(OPENAI_BASE_URL) ?? "https://api.openai.com/v1/";
        var model = Environment.GetEnvironmentVariable(OPENAI_MODEL) ?? "gpt-4";

        _output.WriteLine($"\nLoaded OpenAI configuration:");
        _output.WriteLine($"API Key: {(string.IsNullOrEmpty(apiKey) ? "not set" : "[REDACTED]")}");
        _output.WriteLine($"Base URL: {baseUrl}");
        _output.WriteLine($"Model: {model}\n");

        // Print environment variables
        _output.WriteLine("\nEnvironment Variables:");
        _output.WriteLine($"{OPENAI_API_KEY}: {(string.IsNullOrEmpty(apiKey) ? "not set" : "set")}");
        _output.WriteLine($"{OPENAI_BASE_URL}: {baseUrl ?? "not set"}");
        _output.WriteLine($"{OPENAI_MODEL}: {model ?? "not set"}");

        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine($"\nWARNING: {OPENAI_API_KEY} environment variable not set. All tests will be skipped.");
            return;
        }
        
        _client = new OpenAIClient(apiKey, model, null, baseUrl, temperature: 0.7f, logger: _logger);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_NullMessages_ThrowsException()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.GetCompletionAsync(null));
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_EmptyMessages_ThrowsException()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetCompletionAsync(new List<IMessage>()));
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_SimplePrompt_ReturnsResponse()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
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

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_ChinesePrompt_ReturnsChineseResponse()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("user", "用中文说'你好，世界！'")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("你好", response.Content);
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_WithSystemMessage_ReturnsFormattedResponse()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("system", "You are a helpful assistant that speaks in a formal tone."),
            new Message("user", "Hello")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        var content = response.Content;
        Assert.True(content.Contains("Greetings") || content.Contains("pleasure") || content.Contains("assist"));
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_WithCodeGeneration_GeneratesCode()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("user", "Write a Python function that checks if a string is a palindrome.")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("def", response.Content);
        Assert.Contains("return", response.Content);
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_WithBilingualConversation_HandlesMultipleLanguages()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("user", "Translate 'Hello, how are you?' to Chinese."),
            new Message("assistant", "你好，你好吗？"),
            new Message("user", "Now translate it back to English.")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_PiratePrompt_ReturnsPirateLikeResponse()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("user", "Tell me a pirate joke.")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        // Response should contain pirate-related terms
        var content = response.Content.ToLower();
        Assert.True(
            content.Contains("arr") || 
            content.Contains("matey") || 
            content.Contains("ahoy") ||
            content.Contains("pirate") ||
            content.Contains("ship") ||
            content.Contains("patch") ||
            content.Contains("treasure") ||
            content.Contains("sail")
        );
    }

    [SkipIfNoApiKeyFact("OPENAI_API_KEY")]
    public async Task GetCompletionAsync_WithLongChineseConversation_MaintainsContext()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message("system", "你是一个有用的助手，说话像一个中国古代诗人。"),
            new Message("user", "你叫什么名字？"),
            new Message("assistant", "在下乃是诗词之灵，可唤我为'诗心'。"),
            new Message("user", "你刚才说你叫什么名字？")
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("诗心", response.Content);
    }

    [Fact(Skip = "This test is too expensive to run regularly")]
    public async Task GetCompletionAsync_WithLongConversation_MaintainsContext()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: {OPENAI_API_KEY} environment variable not set");
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
