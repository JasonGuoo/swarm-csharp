using System;

namespace Swarm.CSharp.Attributes;

/// <summary>
/// Specifies that a method represents a function that can be called by the LLM.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FunctionSpecAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the function as it will be presented to the LLM.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the function does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionSpecAttribute"/> class.
    /// </summary>
    public FunctionSpecAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionSpecAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="description">The description of the function.</param>
    public FunctionSpecAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
