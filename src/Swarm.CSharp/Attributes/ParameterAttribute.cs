using System;

namespace Swarm.CSharp.Attributes;

/// <summary>
/// Specifies metadata for a parameter in a function that can be called by the LLM.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class ParameterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the parameter as it will be presented to the LLM.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the parameter does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the parameter. If not specified, it will be inferred from the C# type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets whether the parameter is required. Defaults to true.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterAttribute"/> class.
    /// </summary>
    public ParameterAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="description">The description of the parameter.</param>
    public ParameterAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
