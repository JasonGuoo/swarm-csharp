using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.Models.OpenAI;

/// <summary>
/// Represents a message in a chat completion request or response.
/// </summary>
public class ChatCompletionMessage
{
    /// <summary>
    /// Gets or sets the role of the message author.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the author.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the function call details.
    /// </summary>
    [JsonPropertyName("function_call")]
    public ChatCompletionFunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// Gets or sets the tool calls in the message.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public IReadOnlyList<ChatCompletionToolCall>? ToolCalls { get; set; }
}
