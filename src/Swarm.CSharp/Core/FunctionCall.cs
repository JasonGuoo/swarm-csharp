namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a function call made by the LLM.
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Gets or sets the name of the function to call.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the function.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
}
