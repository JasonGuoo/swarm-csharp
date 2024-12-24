using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Swarm.CSharp.Attributes;
using Swarm.CSharp.Core;
using Swarm.CSharp.LLM;

namespace Swarm.CSharp.Agents;

/// <summary>
/// Base implementation of an agent that provides common functionality.
/// </summary>
public abstract class BaseAgent : IAgent
{
    private readonly ILLMClient _llmClient;
    private readonly IList<FunctionDefinition> _functions;

    /// <inheritdoc/>
    public abstract string SystemPrompt { get; }

    /// <inheritdoc/>
    public virtual ToolChoice ToolChoiceMode => ToolChoice.Auto;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAgent"/> class.
    /// </summary>
    /// <param name="llmClient">The LLM client to use.</param>
    protected BaseAgent(ILLMClient llmClient)
    {
        _llmClient = llmClient;
        _functions = DiscoverFunctions();
    }

    /// <inheritdoc/>
    public virtual async Task<IAgentResponse> ExecuteAsync(IAgentRequest request, IAgentContext context)
    {
        // Add system message if not present
        if (!context.History.Any() || context.History[0].Role != "system")
        {
            context.History.Insert(0, new Message("system", SystemPrompt));
        }

        // Add user request
        context.History.Add(new Message("user", request.Content));

        var response = request.Stream
            ? await HandleStreamingResponse(context)
            : await HandleSingleResponse(context);

        return response;
    }

    private async Task<IAgentResponse> HandleSingleResponse(IAgentContext context)
    {
        var response = await _llmClient.GetCompletionAsync(
            context.History.ToList(),
            _functions,
            ToolChoiceMode);

        context.History.Add(response);

        if (response.ToolCalls != null)
        {
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await ExecuteToolCallAsync(toolCall, context);
                context.History.Add(new Message("tool", result.ToString(), toolCall.Name, toolCall.Id));
            }
        }

        return new AgentResponse(response.Content, context.History.ToList(), context.Variables);
    }

    private async Task<IAgentResponse> HandleStreamingResponse(IAgentContext context)
    {
        var messages = new List<IMessage>();
        var lastContent = string.Empty;

        var response = await _llmClient.GetStreamingCompletionAsync(
            context.History.ToList(),
            _functions,
            ToolChoiceMode);

        messages.Add(response);
        lastContent = response.Content ?? string.Empty;

        if (response.ToolCalls != null)
        {
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await ExecuteToolCallAsync(toolCall, context);
                var toolMessage = new Message("tool", result?.ToString() ?? string.Empty, toolCall.Name, toolCall.Id);
                messages.Add(toolMessage);
                context.History.Add(toolMessage);
            }
        }

        return new AgentResponse(lastContent, messages, context.Variables);
    }

    protected virtual async Task<IMessage> HandleResponse(IAgentContext context)
    {
        var messages = new List<IMessage>();
        var lastContent = string.Empty;

        var response = await _llmClient.GetCompletionAsync(
            context.History.ToList(),
            _functions,
            ToolChoiceMode);

        messages.Add(response);
        lastContent = response.Content ?? string.Empty;

        if (response.ToolCalls != null)
        {
            foreach (var toolCall in response.ToolCalls)
            {
                var result = await ExecuteToolCallAsync(toolCall, context);
                var toolMessage = new Message("tool", result?.ToString() ?? string.Empty, toolCall.Name, toolCall.Id);
                messages.Add(toolMessage);
                context.History.Add(toolMessage);
            }
        }

        var agentResponse = new AgentResponse(lastContent, messages, context.Variables);
        return (IMessage)agentResponse;
    }

    private async Task<object?> ExecuteToolCallAsync(IToolCall toolCall, IAgentContext context)
    {
        var function = _functions.FirstOrDefault(f => f.Name == toolCall.Name);
        if (function == null)
        {
            throw new InvalidOperationException($"Function '{toolCall.Name}' not found.");
        }

        var methodInfo = function.MethodInfo;
        var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.Arguments);

        var parameterValues = new object?[methodInfo.GetParameters().Length];
        var parameterInfos = methodInfo.GetParameters();

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var parameterInfo = parameterInfos[i];
            if (parameters?.TryGetValue(parameterInfo.Name!, out var value) == true)
            {
                parameterValues[i] = JsonSerializer.Deserialize(value.GetRawText(), parameterInfo.ParameterType);
            }
            else
            {
                parameterValues[i] = Type.GetTypeCode(parameterInfo.ParameterType) == TypeCode.Boolean
                    ? false
                    : null;
            }
        }

        var result = methodInfo.Invoke(this, parameterValues);
        return result is Task task
            ? await GetTaskResult(task)
            : result;
    }

    private static async Task<object?> GetTaskResult(Task task)
    {
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private IList<FunctionDefinition> DiscoverFunctions()
    {
        var functions = new List<FunctionDefinition>();
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var functionSpec = method.GetCustomAttribute<FunctionSpecAttribute>();
            if (functionSpec == null)
            {
                continue;
            }

            var parameters = new List<FunctionParameter>();
            var methodParameters = method.GetParameters();

            foreach (var parameter in methodParameters)
            {
                var parameterAttribute = parameter.GetCustomAttribute<ParameterAttribute>();
                if (parameterAttribute == null)
                {
                    continue;
                }

                parameters.Add(new FunctionParameter
                {
                    Name = parameter.Name!,
                    Type = parameterAttribute.Type,
                    Description = parameterAttribute.Description,
                    Required = !parameter.IsOptional
                });
            }

            functions.Add(new FunctionDefinition
            {
                Name = functionSpec.Name ?? method.Name,
                Description = functionSpec.Description,
                Parameters = parameters,
                MethodInfo = method
            });
        }

        return functions;
    }
}
