using System.Threading.Tasks;
using System.Collections.Generic;
using Swarm.CSharp.LLM.Models;

namespace Swarm.CSharp.LLM;

/// <summary>
/// Interface for interacting with Language Learning Models (LLMs).
/// </summary>
public interface ILLMClient
{
    /// <summary>
    /// The model to use for this client.
    /// </summary>
    string Model { get; set; }

    /// <summary>
    /// Send a chat request to the LLM service.
    /// </summary>
    /// <param name="request">The chat request</param>
    /// <returns>The chat response</returns>
    Task<ChatResponse> ChatAsync(ChatRequest request);

    /// <summary>
    /// Send a streaming chat request to the LLM service.
    /// </summary>
    /// <param name="request">The chat request</param>
    /// <returns>A stream of chat responses</returns>
    IAsyncEnumerable<ChatResponse> StreamAsync(ChatRequest request);

    /// <summary>
    /// Check if the client is properly configured and can connect to the service.
    /// </summary>
    Task ValidateConnectionAsync();
}