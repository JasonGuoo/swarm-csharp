using System;

namespace Swarm.CSharp.Function.Attributes
{
    /// <summary>
    /// Annotation for specifying function metadata for LLM function calls.
    /// The function name will be automatically derived from the method name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionSpecAttribute : Attribute
    {
        /// <summary>
        /// Description of what the function does.
        /// </summary>
        public string Description { get; }

        public FunctionSpecAttribute(string description)
        {
            Description = description;
        }
    }
}
