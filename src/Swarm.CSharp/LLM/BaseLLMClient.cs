using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.Core.Exceptions;

namespace Swarm.CSharp.LLM;

/// <summary>
/// Base class for LLM clients providing common functionality.
/// </summary>
public abstract class BaseLLMClient : ILLMClient
{
    protected readonly ILogger<BaseLLMClient>? _logger;
    private readonly string _model;
    private float _temperature;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger<BaseLLMClient>? Logger => _logger;

    /// <summary>
    /// Gets the model identifier being used by this client.
    /// </summary>
    protected string Model => _model;

    /// <summary>
    /// Gets or sets the temperature for the model's responses.
    /// </summary>
    protected float Temperature
    {
        get => _temperature;
        set => _temperature = Math.Clamp(value, 0f, 2f);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseLLMClient"/> class.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <param name="temperature">The temperature for responses.</param>
    /// <param name="logger">Optional logger.</param>
    protected BaseLLMClient(string model, float temperature = 0.7f, ILogger<BaseLLMClient>? logger = null)
    {
        _model = model;
        Temperature = temperature;
        _logger = logger ?? new LoggerFactory().CreateLogger<BaseLLMClient>();
    }

    /// <inheritdoc/>
    public abstract Task<IMessage> GetCompletionAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual async Task<IMessage> GetStreamingCompletionAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        Action<string>? onToken = null,
        CancellationToken cancellationToken = default)
    {
        var content = new List<string>();
        await foreach (var token in GetStreamingCompletionTokensAsync(messages, functions, toolChoice, cancellationToken))
        {
            content.Add(token);
            onToken?.Invoke(token);
        }

        return new Message
        {
            Role = "assistant",
            Content = string.Concat(content)
        };
    }

    /// <inheritdoc/>
    public abstract IAsyncEnumerable<string> GetStreamingCompletionTokensAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates messages before sending to the LLM.
    /// </summary>
    /// <param name="messages">The messages to validate.</param>
    protected virtual void ValidateMessages(IList<IMessage> messages)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages), "Messages cannot be null");
        }

        if (messages.Count == 0)
        {
            throw new ArgumentException("Messages list cannot be empty", nameof(messages));
        }

        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message.Role))
            {
                throw new ArgumentException("Message role cannot be empty", nameof(messages));
            }
        }
    }

    /// <summary>
    /// Creates an async enumerable from a stream of tokens.
    /// </summary>
    /// <param name="getTokens">Function to get the next token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable of tokens.</returns>
    protected async IAsyncEnumerable<string> CreateTokenStream(
        Func<Task<string?>> getTokens,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var token = await getTokens();
            if (token == null)
            {
                yield break;
            }
            yield return token;
        }
    }

    /// <summary>
    /// Logs an error with context.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="context">Additional context.</param>
    protected void LogError(Exception ex, string context)
    {
        _logger?.LogError(ex, "Error in {Context}: {Message}", context, ex.Message);
    }
}
