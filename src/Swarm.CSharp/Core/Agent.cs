using System.Reflection;
using Swarm.CSharp.Function.Attributes;

namespace Swarm.CSharp.Core;

/// <summary>
/// Abstract base class for AI agents in the Swarm framework.
/// 
/// An Agent defines the capabilities and behavior of an AI assistant through:
/// 1. A system prompt that sets the agent's role and guidelines
/// 2. A set of tools (functions) that the agent can use
/// 3. A tool choice mode that determines how tools are selected
/// 
/// Tools are discovered automatically through method annotations:
/// - Use [FunctionSpec] to define a tool
/// - Use [Parameter] to define tool parameters
/// - A special parameter named 'context' of type Dictionary&lt;string, object&gt; can be added 
///   without [Parameter] annotation to access the current execution context
/// 
/// Example:
/// <code>
/// public class WeatherAgent : Agent 
/// {
///     [FunctionSpec("Get weather for location")]
///     public Result GetWeather(
///         [Parameter("City name")] string city,
///         Dictionary&lt;string, object&gt; context) 
///     {
///         // Implementation with access to context
///     }
/// }
/// </code>
/// </summary>
public abstract class Agent : IAgent
{
    /// <summary>
    /// Returns the system prompt that defines this agent's role and capabilities.
    /// 
    /// The system prompt should:
    /// 1. Define the agent's role and expertise
    /// 2. List key capabilities and available tools
    /// 3. Specify any constraints or guidelines
    /// </summary>
    /// <param name="context">The current execution context</param>
    /// <returns>A system prompt string</returns>
    public abstract string GetSystemPrompt(Dictionary<string, object> context);

    /// <summary>
    /// Returns the tool choice mode for this agent.
    /// 
    /// Tool choice modes:
    /// - Auto: Agent automatically decides when to use tools
    /// - None: Agent never uses tools
    /// - Required: Agent must use a tool for each response
    /// </summary>
    /// <returns>The tool choice mode</returns>
    public virtual ToolChoice GetToolChoice() => ToolChoice.Auto;

    /// <summary>
    /// Discovers and returns all available tools from this agent using reflection.
    /// 
    /// Tools are discovered by scanning for methods annotated with [FunctionSpec].
    /// For each annotated method:
    /// 1. Name and description are extracted from [FunctionSpec]
    /// 2. Parameters are extracted from [Parameter] annotations
    /// 3. A tool specification is built for LLM consumption
    /// </summary>
    /// <returns>List of tool specifications</returns>
    public virtual List<Dictionary<string, object>> GetTools()
    {
        var tools = new List<Dictionary<string, object>>();
        var methods = GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<FunctionSpecAttribute>() != null);

        foreach (var method in methods)
        {
            var spec = BuildFunctionSpec(method);
            if (spec != null)
            {
                tools.Add(new Dictionary<string, object>
                {
                    ["function"] = spec
                });
            }
        }

        return tools;
    }

    /// <summary>
    /// Build function specification from method annotations.
    /// </summary>
    public virtual Dictionary<string, object> BuildFunctionSpec(MethodInfo method)
    {
        var attr = method.GetCustomAttribute<FunctionSpecAttribute>();
        if (attr == null) return null;

        var parameters = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType == typeof(Dictionary<string, object>)) continue;

            var paramAttr = param.GetCustomAttribute<ParameterAttribute>();
            if (paramAttr == null) continue;

            parameters[param.Name] = new Dictionary<string, object>
            {
                ["type"] = GetJsonType(param.ParameterType),
                ["description"] = paramAttr.Description
            };

            if (!param.HasDefaultValue)
            {
                required.Add(param.Name);
            }
        }

        return new Dictionary<string, object>
        {
            ["name"] = method.Name.ToLowerInvariant(),
            ["description"] = attr.Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = parameters,
                ["required"] = required
            }
        };
    }

    private string GetJsonType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double)) return "number";
        if (type == typeof(bool)) return "boolean";
        return "string"; // Default to string for other types
    }

    /// <summary>
    /// Find a function by name and get its specification.
    /// </summary>
    public virtual Dictionary<string, object> GetFunctionSpec(string name)
    {
        return GetTools().Find(t => ((Dictionary<string, object>)t["function"])["name"].ToString() == name)?["function"] as Dictionary<string, object>;
    }

    /// <summary>
    /// Find a function method by name.
    /// </summary>
    public virtual MethodInfo FindFunction(string name)
    {
        return GetType().GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<FunctionSpecAttribute>() != null &&
                               m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
