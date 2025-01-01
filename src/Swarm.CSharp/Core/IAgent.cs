namespace Swarm.CSharp.Core;

/// <summary>
/// Interface for agents that can process messages and use tools.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Get the system prompt for this agent.
    /// </summary>
    /// <param name="context">The context for this message</param>
    /// <returns>The system prompt</returns>
    string GetSystemPrompt(Dictionary<string, object> context);

    /// <summary>
    /// Get the tool choice mode for this agent.
    /// </summary>
    ToolChoice GetToolChoice();

    /// <summary>
    /// Get the list of tools available to this agent.
    /// </summary>
    List<Dictionary<string, object>> GetTools();

    /// <summary>
    /// Get the function specification for a given function name.
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <returns>The function specification</returns>
    Dictionary<string, object> GetFunctionSpec(string name);

    /// <summary>
    /// Find a function by its name.
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <returns>The function method info</returns>
    System.Reflection.MethodInfo FindFunction(string name);

    /// <summary>
    /// Build a function specification from a MethodInfo.
    /// </summary>
    /// <param name="method">The MethodInfo to build the spec from</param>
    /// <returns>The function specification</returns>
    Dictionary<string, object> BuildFunctionSpec(System.Reflection.MethodInfo method);
}
