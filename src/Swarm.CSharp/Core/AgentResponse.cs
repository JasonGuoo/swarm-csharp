using System.Collections.Generic;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a response from an agent.
/// </summary>
public class AgentResponse : IAgentResponse
{
    /// <inheritdoc/>
    public string Content { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IMessage> Messages { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> ContextUpdates { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentResponse"/> class.
    /// </summary>
    /// <param name="content">The content of the response.</param>
    /// <param name="messages">The messages generated during the response.</param>
    /// <param name="contextUpdates">The context updates that occurred.</param>
    public AgentResponse(
        string content,
        IReadOnlyList<IMessage> messages,
        IReadOnlyDictionary<string, object> contextUpdates)
    {
        Content = content;
        Messages = messages;
        ContextUpdates = contextUpdates;
    }
}
