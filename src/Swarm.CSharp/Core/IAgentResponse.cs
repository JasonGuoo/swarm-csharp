using System.Collections.Generic;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a response from an agent.
/// </summary>
public interface IAgentResponse
{
    /// <summary>
    /// Gets the final content of the response.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the list of messages generated during the response.
    /// </summary>
    IReadOnlyList<IMessage> Messages { get; }

    /// <summary>
    /// Gets any context updates that occurred during the response.
    /// </summary>
    IReadOnlyDictionary<string, object> ContextUpdates { get; }
}
