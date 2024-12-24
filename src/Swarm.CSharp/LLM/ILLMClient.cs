using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Swarm.CSharp.Core;

namespace Swarm.CSharp.LLM;

/// <summary>
/// Interface for LLM (Language Learning Model) clients.
/// </summary>
public interface ILLMClient
{
    /// <summary>
    /// Gets a completion from the LLM.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="functions">Available functions.</param>
    /// <param name="toolChoice">How to choose tools.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The completion response.</returns>
    Task<IMessage> GetCompletionAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming completion from the LLM.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="functions">Available functions.</param>
    /// <param name="toolChoice">How to choose tools.</param>
    /// <param name="onToken">Callback for each token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The final completion response.</returns>
    Task<IMessage> GetStreamingCompletionAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        System.Action<string>? onToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming completion from the LLM as an async enumerable.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="functions">Available functions.</param>
    /// <param name="toolChoice">How to choose tools.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable of tokens.</returns>
    IAsyncEnumerable<string> GetStreamingCompletionTokensAsync(
        IList<IMessage> messages,
        IList<FunctionDefinition>? functions = null,
        ToolChoice toolChoice = ToolChoice.Auto,
        CancellationToken cancellationToken = default);
}
