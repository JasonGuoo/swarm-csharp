using System.Text.Json.Serialization;

namespace Swarm.CSharp.Core;

/// <summary>
/// Specifies how tools should be chosen by the LLM.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ToolChoice
{
    /// <summary>
    /// Let the model decide whether to use tools.
    /// </summary>
    Auto,

    /// <summary>
    /// Do not use any tools.
    /// </summary>
    None
}
