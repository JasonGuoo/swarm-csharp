namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a tool call made by the LLM.
/// </summary>
public class ToolCall : IToolCall
{
    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Arguments { get; }

    /// <inheritdoc/>
    public string Type => "function";

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCall"/> class.
    /// </summary>
    /// <param name="id">The ID of the tool call.</param>
    /// <param name="name">The name of the tool being called.</param>
    /// <param name="arguments">The arguments for the tool call in JSON format.</param>
    public ToolCall(string id, string name, string arguments)
    {
        Id = id;
        Name = name;
        Arguments = arguments;
    }
}
