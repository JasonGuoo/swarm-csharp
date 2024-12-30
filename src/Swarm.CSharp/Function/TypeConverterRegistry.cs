using System;
using System.Collections.Generic;

namespace Swarm.CSharp.Function
{
    /// <summary>
    /// Registry for type converters.
    /// </summary>
    public class TypeConverterRegistry
    {
        private readonly Dictionary<Type, ITypeConverter> _converters = new();

        public TypeConverterRegistry()
        {
            RegisterDefaultConverters();
        }

        public void RegisterConverter(Type type, ITypeConverter converter)
        {
            _converters[type] = converter;
        }

        public ITypeConverter GetConverter(Type type)
        {
            return _converters.TryGetValue(type, out var converter) ? converter : null;
        }

        private void RegisterDefaultConverters()
        {
            // String converter
            RegisterConverter(typeof(string), new StringTypeConverter());

            // Number converters
            RegisterConverter(typeof(int), new IntegerTypeConverter());
            
            // Add more default converters as needed
        }

        private class StringTypeConverter : ITypeConverter
        {
            public bool CanConvert(Type from, Type to) => to == typeof(string);

            public object Convert(object value, Type targetType)
            {
                return value?.ToString();
            }

            public string ToJsonSchemaType(Type csharpType) => "string";
        }

        private class IntegerTypeConverter : ITypeConverter
        {
            public bool CanConvert(Type from, Type to)
            {
                return typeof(IConvertible).IsAssignableFrom(from) && to == typeof(int);
            }

            public object Convert(object value, Type targetType)
            {
                if (value is IConvertible convertible)
                {
                    return convertible.ToInt32(null);
                }
                return int.Parse(value.ToString());
            }

            public string ToJsonSchemaType(Type csharpType) => "integer";
        }
    }
}
