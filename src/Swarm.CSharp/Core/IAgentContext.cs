using System.Collections.Generic;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents the context for agent operations.
/// </summary>
public interface IAgentContext
{
    /// <summary>
    /// Gets or sets a value in the context.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <returns>The value associated with the key.</returns>
    object? this[string key] { get; set; }

    /// <summary>
    /// Gets all variables in the context.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>
    /// Gets the message history.
    /// </summary>
    IList<IMessage> History { get; }

    /// <summary>
    /// Updates multiple context variables at once.
    /// </summary>
    /// <param name="updates">The dictionary of updates to apply.</param>
    void Update(IDictionary<string, object> updates);

    /// <summary>
    /// Clears all variables from the context.
    /// </summary>
    void Clear();
}
