using System.Collections.Generic;

namespace Swarm.CSharp.Core;

/// <summary>
/// Represents a result from a function call, including any context updates.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class Result<T>
{
    private readonly Dictionary<string, object> _contextUpdates;

    /// <summary>
    /// Gets the value of the result.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the context updates associated with this result.
    /// </summary>
    public IReadOnlyDictionary<string, object> ContextUpdates => _contextUpdates;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the result.</param>
    public Result(T value)
    {
        Value = value;
        _contextUpdates = new Dictionary<string, object>();
    }

    /// <summary>
    /// Adds a context update to the result.
    /// </summary>
    /// <param name="key">The key of the context variable.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The current result instance for method chaining.</returns>
    public Result<T> WithContextUpdate(string key, object value)
    {
        _contextUpdates[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple context updates to the result.
    /// </summary>
    /// <param name="updates">The dictionary of updates to apply.</param>
    /// <returns>The current result instance for method chaining.</returns>
    public Result<T> WithContextUpdates(IDictionary<string, object> updates)
    {
        foreach (var (key, value) in updates)
        {
            _contextUpdates[key] = value;
        }
        return this;
    }
}
