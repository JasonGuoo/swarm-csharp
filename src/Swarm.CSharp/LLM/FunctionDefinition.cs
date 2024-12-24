using System.Collections.Generic;
using System.Reflection;

namespace Swarm.CSharp.LLM;

/// <summary>
/// Represents a function definition that can be provided to the LLM.
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters of the function.
    /// </summary>
    public IList<FunctionParameter> Parameters { get; set; } = new List<FunctionParameter>();

    /// <summary>
    /// Gets or sets the method info of the function.
    /// </summary>
    public MethodInfo MethodInfo { get; set; } = null!;
}

/// <summary>
/// Represents a function parameter.
/// </summary>
public class FunctionParameter
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the parameter.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the parameter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is required.
    /// </summary>
    public bool Required { get; set; }
}
