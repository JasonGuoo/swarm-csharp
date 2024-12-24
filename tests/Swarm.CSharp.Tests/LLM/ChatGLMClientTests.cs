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
public class ChatGLMClientTests : IAsyncLifetime
{
    private readonly ChatGLMClient _client;
    private readonly ILogger<ChatGLMClient> _logger;
    private readonly ITestOutputHelper _output;
    private const string ZHIPUAI_API_KEY = "ZHIPUAI_API_KEY";
    private const string ZHIPUAI_API_BASE = "ZHIPUAI_API_BASE";
    private const string ZHIPUAI_MODEL = "ZHIPUAI_MODEL";

    public ChatGLMClientTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Debug)
                .AddFilter("System", LogLevel.Debug)
                .AddFilter("Swarm.CSharp", LogLevel.Debug)
                .AddXUnit(output)
                .AddConsole(); // Add console output for better visibility during development
        });
        _logger = loggerFactory.CreateLogger<ChatGLMClient>();

        // Load environment variables from .env file if it exists
        var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        var envPath = Path.Combine(projectRoot, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        
        // Get configuration from environment variables
        var apiKey = Environment.GetEnvironmentVariable(ZHIPUAI_API_KEY);
        var baseUrl = Environment.GetEnvironmentVariable(ZHIPUAI_API_BASE) ?? "https://open.bigmodel.cn/api/paas/v4/";
        var model = Environment.GetEnvironmentVariable(ZHIPUAI_MODEL) ?? "glm-4-plus";

        // Print environment variables
        _output.WriteLine("\nEnvironment Variables:");
        _output.WriteLine($"{ZHIPUAI_API_KEY}: {(string.IsNullOrEmpty(apiKey) ? "not set" : "set")}");
        _output.WriteLine($"{ZHIPUAI_API_BASE}: {baseUrl ?? "not set"}");
        _output.WriteLine($"{ZHIPUAI_MODEL}: {model ?? "glm-4-flash"}");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(baseUrl))
        {
            _output.WriteLine($"\nWARNING: One or more required environment variables not set. All tests will be skipped.");
            _output.WriteLine($"Required variables: {ZHIPUAI_API_KEY}, {ZHIPUAI_API_BASE}");
            return;
        }
        
        _client = new ChatGLMClient(apiKey, baseUrl, model, 0.7f, _logger);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
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

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
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

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
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

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_InvalidApiKey_ThrowsException()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var baseUrl = Environment.GetEnvironmentVariable(ZHIPUAI_API_BASE) ?? "https://open.bigmodel.cn/api/paas/v4/";
        var model = Environment.GetEnvironmentVariable(ZHIPUAI_MODEL) ?? "glm-4-plus";
        var client = new ChatGLMClient("invalid-key", baseUrl, model, 0.7f, _logger);
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

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_SimplePrompt_ReturnsResponse_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "Say 'Hello!'" }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("assistant", response.Role);
        Assert.NotEmpty(response.Content);
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_InvalidApiKey_ThrowsException_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var invalidClient = new ChatGLMClient(
            "invalid-key",
            Environment.GetEnvironmentVariable(ZHIPUAI_API_BASE) ?? "https://open.bigmodel.cn/api/paas/v4/",
            Environment.GetEnvironmentVariable(ZHIPUAI_MODEL) ?? "glm-4-plus",
            0.7f,
            _logger);

        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "This should fail" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => invalidClient.GetCompletionAsync(messages));
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_EmptyMessages_ThrowsException_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetCompletionAsync(messages));
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_NullMessages_ThrowsException_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.GetCompletionAsync(null));
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_ChinesePrompt_ReturnsChineseResponse_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "用中文说'你好，世界！'" }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("你好", response.Content);
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetStreamingCompletionTokensAsync_ReturnsTokenStream_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "Count from 1 to 5 in Chinese." }
        };

        // Act
        var tokens = new List<string>();
        await foreach (var token in _client.GetStreamingCompletionTokensAsync(messages))
        {
            tokens.Add(token);
        }

        // Assert
        Assert.NotEmpty(tokens);
        var fullResponse = string.Concat(tokens);
        Assert.Contains("一", fullResponse);
        Assert.Contains("五", fullResponse);
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_WithSystemMessage_ReturnsFormattedResponse_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "system", Content = "You are a helpful assistant that speaks in a formal tone." },
            new Message { Role = "user", Content = "Hello" }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        var content = response.Content;
        Assert.True(content.Contains("Greetings") || content.Contains("pleasure") || content.Contains("assist"));
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_WithCodeGeneration_GeneratesCode_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message 
            { 
                Role = "user", 
                Content = "Write a Python function that checks if a string is a palindrome." 
            }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("def", response.Content);
        Assert.Contains("return", response.Content);
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_WithLongConversation_MaintainsContext_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "My name is Alice." },
            new Message { Role = "assistant", Content = "Hello Alice! Nice to meet you." },
            new Message { Role = "user", Content = "I live in Beijing." },
            new Message { Role = "assistant", Content = "That's great! Beijing is a fascinating city." },
            new Message { Role = "user", Content = "What's my name and where do I live?" }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("Alice", response.Content);
        Assert.Contains("Beijing", response.Content);
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_WithBilingualConversation_HandlesMultipleLanguages_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "Translate 'Hello, how are you?' to Chinese." },
            new Message { Role = "assistant", Content = "你好，你好吗？" },
            new Message { Role = "user", Content = "Now translate it back to English." }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        Assert.Contains("Hello", response.Content, StringComparison.OrdinalIgnoreCase);
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_PiratePrompt_ReturnsPirateLikeResponse_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "user", Content = "Tell me a pirate joke." }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Content);
        // Response should contain pirate-like language
        var content = response.Content.ToLower();
        Assert.True(content.Contains("arr") || content.Contains("matey") || content.Contains("ahoy"));
    }

    [SkipIfNoApiKeyFact("ZHIPUAI_API_KEY")]
    public async Task GetCompletionAsync_WithLongChineseConversation_MaintainsContext_Original()
    {
        if (_client == null)
        {
            _output.WriteLine($"Test skipped: Required environment variables not set");
            return;
        }

        // Arrange
        var messages = new List<IMessage>
        {
            new Message { Role = "system", Content = "你是一个有用的助手，说话像一个中国古代诗人。" },
            new Message { Role = "user", Content = "你叫什么名字？" },
            new Message { Role = "assistant", Content = "在下乃是诗词之灵，可唤我为'诗心'。" },
            new Message { Role = "user", Content = "你刚才说你叫什么名字？" }
        };

        // Act
        var response = await _client.GetCompletionAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("诗心", response.Content);
    }
}
