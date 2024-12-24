using System.Text.Json.Serialization;

namespace Swarm.CSharp.Models.OpenAI;

/// <summary>
/// Represents a function that can be called by the chat completion.
/// </summary>
public class ChatCompletionFunction
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments of the function.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters of the function.
    /// </summary>
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}
