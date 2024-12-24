using System.Collections.Generic;
using Swarm.CSharp.Models.OpenAI;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a message in the conversation.
/// </summary>
public class Message : IMessage
{
    /// <inheritdoc/>
    public string Role { get; set; }

    /// <inheritdoc/>
    public string Content { get; set; }

    /// <inheritdoc/>
    public IReadOnlyList<IToolCall>? ToolCalls { get; set; }

    /// <inheritdoc/>
    public string? ToolName { get; set; }

    /// <inheritdoc/>
    public string? ToolCallId { get; set; }

    /// <inheritdoc/>
    public ChatCompletionFunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// Gets or sets the name associated with this message.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> class.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The content of the message.</param>
    /// <param name="toolName">The name of the tool, if this is a tool response.</param>
    /// <param name="toolCallId">The ID of the tool call this message is responding to.</param>
    /// <param name="toolCalls">The tool calls in the message, if any.</param>
    public Message(
        string role,
        string content,
        string? toolName = null,
        string? toolCallId = null,
        IReadOnlyList<IToolCall>? toolCalls = null)
    {
        Role = role;
        Content = content;
        ToolName = toolName;
        ToolCallId = toolCallId;
        ToolCalls = toolCalls;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> class.
    /// </summary>
    public Message()
    {
        Role = string.Empty;
        Content = string.Empty;
    }
}
