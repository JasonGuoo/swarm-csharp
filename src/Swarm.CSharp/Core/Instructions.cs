namespace Swarm.CSharp.Core;

/// <summary>
/// Provides common instruction templates for agents.
/// </summary>
public static class Instructions
{
    /// <summary>
    /// Gets a basic instruction template for an agent.
    /// </summary>
    /// <param name="role">The role of the agent.</param>
    /// <param name="expertise">The areas of expertise.</param>
    /// <returns>A formatted instruction string.</returns>
    public static string GetBasicTemplate(string role, string expertise) =>
$@"You are a {role} with expertise in {expertise}.

Your capabilities:
1. You can use tools by calling functions
2. You maintain context between interactions
3. You can delegate to other agents when needed

Guidelines:
1. Be direct and efficient in your responses
2. Use appropriate tools when necessary
3. Stay focused on your area of expertise
4. Ask for clarification when needed";

    /// <summary>
    /// Gets an instruction template for a specialized agent.
    /// </summary>
    /// <param name="role">The role of the agent.</param>
    /// <param name="expertise">The areas of expertise.</param>
    /// <param name="constraints">Any specific constraints or rules.</param>
    /// <returns>A formatted instruction string.</returns>
    public static string GetSpecializedTemplate(string role, string expertise, string constraints) =>
$@"You are a specialized {role} with deep expertise in {expertise}.

Your capabilities:
1. You can use tools by calling functions
2. You maintain context between interactions
3. You can delegate to other agents when needed

Constraints and Rules:
{constraints}

Guidelines:
1. Be direct and efficient in your responses
2. Use appropriate tools when necessary
3. Stay within your defined constraints
4. Ask for clarification when needed";
}
