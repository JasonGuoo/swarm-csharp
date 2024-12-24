namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a request to be processed by an agent.
/// </summary>
public class AgentRequest : IAgentRequest
{
    /// <inheritdoc/>
    public string Content { get; }

    /// <inheritdoc/>
    public int MaxTurns { get; }

    /// <inheritdoc/>
    public bool Stream { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRequest"/> class.
    /// </summary>
    /// <param name="content">The content of the request.</param>
    /// <param name="maxTurns">The maximum number of turns for this request.</param>
    /// <param name="stream">Whether to stream the response.</param>
    public AgentRequest(string content, int maxTurns = 10, bool stream = false)
    {
        Content = content;
        MaxTurns = maxTurns;
        Stream = stream;
    }
}
