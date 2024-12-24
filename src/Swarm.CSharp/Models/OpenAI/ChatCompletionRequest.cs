using System.Collections.Generic;
using System.Text.Json.Serialization;
using Swarm.CSharp.Core;

namespace Swarm.CSharp.Models.OpenAI;

/// <summary>
/// Represents a request to the OpenAI Chat Completion API.
/// </summary>
public class ChatCompletionRequest
{
    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the messages to generate chat completions for.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ChatCompletionMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the functions available to call.
    /// </summary>
    [JsonPropertyName("functions")]
    public List<ChatCompletionFunction>? Functions { get; set; }

    /// <summary>
    /// Gets or sets the sampling temperature to use.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stream the response.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets how to choose tools.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public ToolChoice ToolChoice { get; set; }
}
