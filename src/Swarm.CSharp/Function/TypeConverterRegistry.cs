using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swarm.CSharp.Function;

/// <summary>
/// Registry for type converters.
/// </summary>
public class TypeConverterRegistry
{
    private readonly ConcurrentDictionary<(Type Source, Type Target), object> _converters = new();

    /// <summary>
    /// Registers a converter for a type pair.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="converter">The converter function.</param>
    public void Register<TSource, TTarget>(Func<TSource, TTarget> converter)
    {
        _converters[(typeof(TSource), typeof(TTarget))] = new TypeConverter<TSource, TTarget>(converter);
    }

    /// <summary>
    /// Gets a converter for a type pair.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The converter.</returns>
    public TypeConverter<TSource, TTarget>? GetConverter<TSource, TTarget>()
    {
        if (_converters.TryGetValue((typeof(TSource), typeof(TTarget)), out var converter))
        {
            return (TypeConverter<TSource, TTarget>)converter;
        }
        return null;
    }

    /// <summary>
    /// Converts a value to the target type.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public TTarget? ConvertValue<TTarget>(object? value)
    {
        if (value == null)
        {
            return default;
        }

        var sourceType = value.GetType();
        var targetType = typeof(TTarget);

        // Direct assignment if types match
        if (targetType.IsAssignableFrom(sourceType))
        {
            return (TTarget)value;
        }

        // Try registered converter
        if (_converters.TryGetValue((sourceType, targetType), out var converter))
        {
            var method = converter.GetType().GetMethod("Convert");
            return (TTarget)method.Invoke(converter, new[] { value });
        }

        // Handle JSON conversions
        if (value is JsonNode jsonNode)
        {
            return jsonNode.Deserialize<TTarget>();
        }

        // Handle dictionary to object conversion
        if (value is IDictionary<string, object> dict)
        {
            var json = JsonSerializer.Serialize(dict);
            return JsonSerializer.Deserialize<TTarget>(json);
        }

        // Try basic type conversion
        try
        {
            return (TTarget)Convert.ChangeType(value, targetType);
        }
        catch
        {
            throw new InvalidOperationException($"No converter registered for {sourceType} to {targetType}");
        }
    }

    /// <summary>
    /// Gets the default type converter registry with common conversions.
    /// </summary>
    /// <returns>A pre-configured registry.</returns>
    public static TypeConverterRegistry Default()
    {
        var registry = new TypeConverterRegistry();

        // Register common conversions
        registry.Register<string, int>(int.Parse);
        registry.Register<string, long>(long.Parse);
        registry.Register<string, double>(double.Parse);
        registry.Register<string, bool>(bool.Parse);
        registry.Register<int, string>(x => x.ToString());
        registry.Register<long, string>(x => x.ToString());
        registry.Register<double, string>(x => x.ToString());
        registry.Register<bool, string>(x => x.ToString());

        // Register dictionary to object conversion
        registry.Register<Dictionary<string, object>, object>(dict =>
        {
            var targetType = Type.GetType(dict["__type"]?.ToString() ?? throw new InvalidOperationException("No __type specified"));
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(dict), targetType);
        });

        return registry;
    }
}
