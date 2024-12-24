using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Swarm.CSharp.Core;

/// <summary>
/// Default implementation of IAgentContext that provides thread-safe context management.
/// </summary>
public class AgentContext : IAgentContext
{
    private readonly ConcurrentDictionary<string, object> _variables;
    private readonly List<IMessage> _history;

    /// <inheritdoc/>
    public object? this[string key]
    {
        get => _variables.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value == null)
            {
                _variables.TryRemove(key, out _);
            }
            else
            {
                _variables.AddOrUpdate(key, value, (_, _) => value);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Variables => _variables;

    /// <inheritdoc/>
    public IList<IMessage> History => _history;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContext"/> class.
    /// </summary>
    public AgentContext()
    {
        _variables = new ConcurrentDictionary<string, object>();
        _history = new List<IMessage>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContext"/> class with initial variables.
    /// </summary>
    /// <param name="variables">The initial variables to populate the context with.</param>
    public AgentContext(IDictionary<string, object> variables)
    {
        _variables = new ConcurrentDictionary<string, object>(variables);
        _history = new List<IMessage>();
    }

    /// <inheritdoc/>
    public void Update(IDictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            this[key] = value;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _variables.Clear();
    }
}
