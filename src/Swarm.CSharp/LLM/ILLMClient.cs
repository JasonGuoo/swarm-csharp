using System.Threading.Tasks;
using Swarm.CSharp.LLM.Models;

namespace Swarm.CSharp.LLM
{
    /// <summary>
    /// Base interface for LLM providers
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Send a chat completion request
        /// </summary>
        Task<ChatResponse> ChatAsync(ChatRequest request);

        /// <summary>
        /// Send a streaming chat completion request
        /// </summary>
        Task<Stream> ChatStreamAsync(ChatRequest request);
    }
}