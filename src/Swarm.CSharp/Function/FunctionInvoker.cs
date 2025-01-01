using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Swarm.CSharp.Function.Attributes;

namespace Swarm.CSharp.Function
{
    /// <summary>
    /// Handles dynamic function invocation with parameter conversion and validation.
    /// </summary>
    public class FunctionInvoker
    {
        private readonly JsonSerializer _jsonSerializer;
        private readonly TypeConverterRegistry _typeConverterRegistry;

        public FunctionInvoker()
        {
            _jsonSerializer = JsonSerializer.CreateDefault();
            _typeConverterRegistry = new TypeConverterRegistry();
        }

        /// <summary>
        /// Invokes a function with the given parameters
        /// </summary>
        public object Invoke(MethodInfo method, object target, IDictionary<string, object> parameters)
        {
            // Validate function annotation
            var spec = method.GetCustomAttribute<FunctionSpecAttribute>();
            if (spec == null)
            {
                throw new ArgumentException("Method must be annotated with FunctionSpecAttribute");
            }

            // Process parameters
            var args = ProcessParameters(method, parameters);

            // Invoke the method
            try
            {
                var result = method.Invoke(target, args);

                // Handle async methods
                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();

                    // If it's a Task<T>, return the result
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                return result;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is ArgumentException)
                {
                    throw e.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Invokes a function asynchronously with the given parameters
        /// </summary>
        public async Task<object> InvokeAsync(MethodInfo method, object target, IDictionary<string, object> parameters)
        {
            // Validate function annotation
            var spec = method.GetCustomAttribute<FunctionSpecAttribute>();
            if (spec == null)
            {
                throw new ArgumentException("Method must be annotated with FunctionSpecAttribute");
            }

            // Process parameters
            var args = ProcessParameters(method, parameters);

            // Invoke the method
            try
            {
                var result = method.Invoke(target, args);

                // Handle async methods
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);

                    // If it's a Task<T>, return the result
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                return result;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is ArgumentException)
                {
                    throw e.InnerException;
                }
                throw;
            }
        }

        private object[] ProcessParameters(MethodInfo method, IDictionary<string, object> parameters)
        {
            var methodParams = method.GetParameters();
            var args = new object[methodParams.Length];

            for (var i = 0; i < methodParams.Length; i++)
            {
                var param = methodParams[i];
                var annotation = param.GetCustomAttribute<ParameterAttribute>();

                // Special handling for context parameter
                if (param.Name == "context" &&
                    param.ParameterType == typeof(Dictionary<string, object>))
                {
                    args[i] = parameters["context"];
                    continue;
                }

                var paramName = param.Name;
                var paramType = param.ParameterType;
                parameters.TryGetValue(paramName, out var rawValue);

                // Handle missing required parameters
                if (rawValue == null)
                {
                    if (annotation != null && !string.IsNullOrEmpty(annotation.DefaultValue))
                    {
                        rawValue = annotation.DefaultValue;
                    }
                    else if (!paramType.IsValueType)
                    {
                        // Allow null for reference types unless explicitly marked as required
                        args[i] = null;
                        continue;
                    }
                    else if (annotation == null)
                    {
                        // Skip parameters without Parameter attribute
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Required parameter '{paramName}' of type '{paramType.Name}' is missing");
                    }
                }

                try
                {
                    // Convert parameter to the correct type
                    args[i] = ConvertValue(rawValue, paramType);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Failed to convert parameter '{paramName}' from '{rawValue}' to type '{paramType.Name}': {e.Message}");
                }
            }

            return args;
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                if (targetType.IsValueType)
                {
                    throw new ArgumentException($"Cannot convert null to value type {targetType}");
                }
                return null;
            }

            // Handle arrays to collection conversion
            if (typeof(IEnumerable).IsAssignableFrom(targetType) && value.GetType().IsArray)
            {
                var elementType = targetType.GetElementType() ?? targetType.GetGenericArguments().FirstOrDefault();
                if (elementType != null)
                {
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    foreach (var item in (Array)value)
                    {
                        list.Add(ConvertValue(item, elementType));
                    }
                    return list;
                }
            }

            // Try to find a converter
            var converter = _typeConverterRegistry.GetConverter(targetType);
            if (converter != null)
            {
                return converter.Convert(value, targetType);
            }

            // Handle string to number conversion
            if (typeof(IConvertible).IsAssignableFrom(targetType) && value is string strValue)
            {
                try
                {
                    return System.Convert.ChangeType(strValue, targetType);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException($"Cannot convert string '{strValue}' to {targetType.Name}", e);
                }
            }

            // Default conversion using JSON.NET
            try
            {
                return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), targetType);
            }
            catch (JsonException e)
            {
                throw new ArgumentException(
                    $"Cannot convert value of type '{value.GetType().Name}' to target type '{targetType.Name}'", e);
            }
        }
    }
}
