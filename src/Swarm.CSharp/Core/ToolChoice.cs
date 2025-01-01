namespace Swarm.CSharp.Core;

/// <summary>
/// Specifies how tools should be used by the agent.
/// </summary>
public enum ToolChoice
{
    /// <summary>
    /// The agent should not use any tools.
    /// </summary>
    None,

    /// <summary>
    /// The agent should automatically decide whether to use tools.
    /// </summary>
    Auto,

    /// <summary>
    /// The agent must use tools.
    /// </summary>
    Required
}
