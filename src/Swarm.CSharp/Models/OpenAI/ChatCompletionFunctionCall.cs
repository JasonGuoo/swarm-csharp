using System.Text.Json.Serialization;

namespace Swarm.CSharp.Models.OpenAI;

/// <summary>
/// Represents a function call in a chat completion response.
/// </summary>
public class ChatCompletionFunctionCall
{
    /// <summary>
    /// Gets or sets the name of the function to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the function.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}
