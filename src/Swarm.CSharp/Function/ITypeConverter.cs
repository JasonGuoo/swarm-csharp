using System;

namespace Swarm.CSharp.Function
{
    /// <summary>
    /// Interface for type conversion between JSON and C# types.
    /// </summary>
    public interface ITypeConverter
    {
        /// <summary>
        /// Check if this converter can handle the conversion
        /// </summary>
        bool CanConvert(Type from, Type to);

        /// <summary>
        /// Convert a value to the target type
        /// </summary>
        object Convert(object value, Type targetType);

        /// <summary>
        /// Get the JSON Schema type for a C# type
        /// </summary>
        string ToJsonSchemaType(Type csharpType);
    }
}
