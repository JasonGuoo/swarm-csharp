using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.Core.Exceptions;

namespace Swarm.CSharp.Function;

/// <summary>
/// Invokes functions dynamically with parameter conversion.
/// </summary>
public class FunctionInvoker
{
    private readonly TypeConverterRegistry _converterRegistry;
    private readonly ILogger<FunctionInvoker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvoker"/> class.
    /// </summary>
    /// <param name="converterRegistry">The type converter registry to use.</param>
    /// <param name="logger">Optional logger.</param>
    public FunctionInvoker(TypeConverterRegistry? converterRegistry = null, ILogger<FunctionInvoker>? logger = null)
    {
        _converterRegistry = converterRegistry ?? TypeConverterRegistry.Default();
        _logger = logger ?? new LoggerFactory().CreateLogger<FunctionInvoker>();
    }

    /// <summary>
    /// Invokes a function on an object with the given parameters.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="parameters">The parameters to pass.</param>
    /// <returns>The result of the function call.</returns>
    public async Task<object?> InvokeAsync(object target, string methodName, IDictionary<string, object> parameters)
    {
        var method = FindMethod(target.GetType(), methodName);
        if (method == null)
        {
            throw new SwarmException($"Method {methodName} not found on {target.GetType().Name}", "TOOL_NOT_FOUND");
        }

        var convertedArgs = ConvertParameters(method, parameters);
        
        try
        {
            var result = method.Invoke(target, convertedArgs);
            if (result is Task task)
            {
                await task;
                return task.GetType().GetProperty("Result")?.GetValue(task);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking method {MethodName}", methodName);
            throw new SwarmException($"Error invoking {methodName}: {ex.Message}", "INVOCATION_ERROR", ex);
        }
    }

    private MethodInfo? FindMethod(Type type, string methodName)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
    }

    private object?[] ConvertParameters(MethodInfo method, IDictionary<string, object> parameters)
    {
        var methodParams = method.GetParameters();
        var args = new object?[methodParams.Length];

        for (var i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            if (parameters.TryGetValue(param.Name!, out var value))
            {
                try
                {
                    var convertMethod = typeof(TypeConverterRegistry)
                        .GetMethod(nameof(TypeConverterRegistry.ConvertValue))!
                        .MakeGenericMethod(param.ParameterType);
                    args[i] = convertMethod.Invoke(_converterRegistry, new[] { value });
                }
                catch (Exception ex)
                {
                    throw new SwarmException(
                        $"Error converting parameter {param.Name} to type {param.ParameterType.Name}: {ex.Message}",
                        "INVALID_ARGUMENTS",
                        ex);
                }
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                throw new SwarmException(
                    $"Required parameter {param.Name} not provided",
                    "INVALID_ARGUMENTS");
            }
        }

        return args;
    }
}
