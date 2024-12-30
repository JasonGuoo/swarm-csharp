using System;

namespace Swarm.CSharp.Function.Attributes
{
    /// <summary>
    /// Annotation for specifying parameter metadata.
    /// The parameter name and type will be automatically derived from the method parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ParameterAttribute : Attribute
    {
        /// <summary>
        /// Description of what the parameter does.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Default value for the parameter if not provided.
        /// Should be a string representation of the value compatible with the parameter type.
        /// </summary>
        public string DefaultValue { get; }

        public ParameterAttribute(string description, string defaultValue = "")
        {
            Description = description;
            DefaultValue = defaultValue;
        }
    }
}
