namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a request to be processed by an agent.
/// </summary>
public interface IAgentRequest
{
    /// <summary>
    /// Gets the content of the request.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the maximum number of turns for this request.
    /// </summary>
    int MaxTurns { get; }

    /// <summary>
    /// Gets whether to stream the response.
    /// </summary>
    bool Stream { get; }
}
