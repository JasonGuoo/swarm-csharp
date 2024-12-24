using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core.Exceptions;

namespace Swarm.CSharp.Core;

/// <summary>
/// Manages context and state for agent interactions.
/// </summary>
public class ContextManager
{
    private readonly ILogger<ContextManager> _logger;
    private readonly ConcurrentDictionary<string, IAgentContext> _contexts;
    private readonly SemaphoreSlim _lock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextManager"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for context operations.</param>
    public ContextManager(ILogger<ContextManager>? logger = null)
    {
        _logger = logger ?? new LoggerFactory().CreateLogger<ContextManager>();
        _contexts = new ConcurrentDictionary<string, IAgentContext>();
        _lock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Gets or creates a context for the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The context for the session.</returns>
    public IAgentContext GetOrCreateContext(string sessionId)
    {
        return _contexts.GetOrAdd(sessionId, _ => new AgentContext());
    }

    /// <summary>
    /// Updates a context with new variables.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="updates">The updates to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateContextAsync(string sessionId, IDictionary<string, object> updates)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_contexts.TryGetValue(sessionId, out var context))
            {
                throw new SwarmException($"Context not found for session {sessionId}", "CONTEXT_ERROR");
            }

            context.Update(updates);
            _logger.LogDebug("Updated context for session {SessionId} with {Count} variables", sessionId, updates.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets a variable from the context.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="key">The variable key.</param>
    /// <returns>The variable value.</returns>
    public T? GetVariable<T>(string sessionId, string key)
    {
        if (!_contexts.TryGetValue(sessionId, out var context))
        {
            throw new SwarmException($"Context not found for session {sessionId}", "CONTEXT_ERROR");
        }

        var value = context[key];
        if (value == null)
        {
            return default;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting context variable {Key} to type {Type}", key, typeof(T).Name);
            throw new SwarmException($"Error converting context variable {key} to type {typeof(T).Name}", "CONTEXT_ERROR", ex);
        }
    }

    /// <summary>
    /// Clears a context.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    public void ClearContext(string sessionId)
    {
        if (_contexts.TryGetValue(sessionId, out var context))
        {
            context.Clear();
            _logger.LogDebug("Cleared context for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Removes a context.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    public void RemoveContext(string sessionId)
    {
        if (_contexts.TryRemove(sessionId, out _))
        {
            _logger.LogDebug("Removed context for session {SessionId}", sessionId);
        }
    }
}
