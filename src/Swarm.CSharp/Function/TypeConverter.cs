using System;

namespace Swarm.CSharp.Function;

/// <summary>
/// Converts values between different types.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TTarget">The target type.</typeparam>
public class TypeConverter<TSource, TTarget>
{
    private readonly Func<TSource, TTarget> _converter;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeConverter{TSource, TTarget}"/> class.
    /// </summary>
    /// <param name="converter">The conversion function.</param>
    public TypeConverter(Func<TSource, TTarget> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <summary>
    /// Converts a value from the source type to the target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public TTarget Convert(TSource value) => _converter(value);
}
