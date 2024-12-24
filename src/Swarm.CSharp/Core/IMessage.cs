using System.Collections.Generic;
using Swarm.CSharp.Models.OpenAI;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a message in the conversation.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    string Role { get; set; }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    string Content { get; set; }

    /// <summary>
    /// Gets or sets the tool calls in the message, if any.
    /// </summary>
    IReadOnlyList<IToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the name of the tool, if this is a tool response message.
    /// </summary>
    string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the tool call ID this message is responding to, if any.
    /// </summary>
    string? ToolCallId { get; set; }

    /// <summary>
    /// Gets or sets the function call associated with this message.
    /// </summary>
    ChatCompletionFunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// Gets or sets the name associated with this message.
    /// </summary>
    string? Name { get; set; }
}
