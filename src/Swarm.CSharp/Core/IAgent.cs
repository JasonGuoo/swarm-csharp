using System.Threading.Tasks;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a base agent interface that all agents must implement.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Gets the system prompt that defines the agent's role and behavior.
    /// </summary>
    string SystemPrompt { get; }

    /// <summary>
    /// Gets the tool choice mode for the agent.
    /// </summary>
    ToolChoice ToolChoiceMode { get; }

    /// <summary>
    /// Executes a request with the given context.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="context">The context for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IAgentResponse> ExecuteAsync(IAgentRequest request, IAgentContext context);
}
