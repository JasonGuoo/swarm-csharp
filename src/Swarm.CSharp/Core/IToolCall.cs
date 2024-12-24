namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a tool call made by the LLM.
/// </summary>
public interface IToolCall
{
    /// <summary>
    /// Gets the ID of the tool call.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the tool being called.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the arguments for the tool call in JSON format.
    /// </summary>
    string Arguments { get; }

    /// <summary>
    /// Gets the type of the tool call.
    /// </summary>
    string Type { get; }
}
