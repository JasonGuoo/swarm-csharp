using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swarm.CSharp.Function.Attributes;
using Swarm.CSharp.LLM;
using Swarm.CSharp.LLM.Models;

namespace Swarm.CSharp.Core
{
    /// <summary>
    /// Main orchestrator for the Swarm framework.
    /// </summary>
    public class SwarmCore
    {
        private readonly ILogger<SwarmCore> _logger;
        private readonly ILLMClient _client;
        private readonly ErrorHandler _errorHandler;
        private readonly ContextManager _contextManager;
        private readonly JsonSerializer _jsonSerializer;
        private const string CtxVarsName = "context_variables";
        private const int DefaultMaxTurns = 10;

        public SwarmCore(ILLMClient client, ILogger<SwarmCore> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = new ErrorHandler();
            _contextManager = new ContextManager(logger);
            _jsonSerializer = JsonSerializer.CreateDefault();
        }

        /// <summary>
        /// Run agent with messages and context
        /// </summary>
        public async Task<SwarmResponse> RunAsync(
            IAgent agent,
            List<Message> messages,
            Dictionary<string, object> contextVariables,
            string modelOverride = null,
            bool stream = false,
            bool debug = false,
            int maxTurns = DefaultMaxTurns)
        {
            _logger.LogInformation("Starting Swarm execution with agent: {Agent}, stream: {Stream}, maxTurns: {MaxTurns}",
                agent.GetType().Name, stream, maxTurns);

            try
            {
                IAgent activeAgent = agent;
                var context = _contextManager.InitializeContext(contextVariables);
                _logger.LogDebug("Initialized context with {Count} variables", context.Count);

                var history = new List<Message>(messages);
                int initLen = messages.Count;
                int turn = 0;

                while (history.Count - initLen < maxTurns && activeAgent != null)
                {
                    turn++;
                    _logger.LogDebug("Starting turn {Turn}/{MaxTurns}", turn, maxTurns);

                    try
                    {
                        if (debug)
                        {
                            _logger.LogDebug("Current history before completion:");
                            PrintHistory(history);
                        }

                        var completion = await GetChatCompletionAsync(
                            activeAgent, history, context, modelOverride, stream, debug);

                        CheckResponseError(completion);

                        // Break if no tool calls - task is complete
                        if (!HasToolCalls(completion))
                        {
                            _logger.LogDebug("No tool calls in response, ending turn");
                            // Add the response message to history
                            if (completion.Choices?.Any() == true)
                            {
                                var responseMessage = completion.Choices[0].Message;
                                history.Add(responseMessage);
                            }
                            break;
                        }

                        // Add the response message to history
                        if (completion.Choices?.Any() == true)
                        {
                            var responseMessage = completion.Choices[0].Message;
                            history.Add(responseMessage);

                            // Handle any tool calls
                            if (HasToolCalls(completion))
                            {
                                var results = await HandleToolCallsAsync(completion, activeAgent, history, context);
                                var toolCalls = responseMessage.ToolCalls;

                                // Process each result and add to history
                                for (int i = 0; i < results.Count; i++)
                                {
                                    var result = results[i];
                                    if (result is IAgent newAgent)
                                    {
                                        activeAgent = newAgent;
                                    }

                                    var toolCall = toolCalls[i];
                                    var callMessage = new Message
                                    {
                                        Role = "tool",
                                        ToolCallId = toolCall.Id,
                                        ToolName = toolCall.Function.Name,
                                        Content = result.ToString()
                                    };
                                    history.Add(callMessage);
                                }
                            }
                        }

                        if (debug)
                        {
                            _logger.LogDebug("Updated history after processing response:");
                            PrintHistory(history);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error during turn {Turn}: {Message}", turn, e.Message);
                        return new SwarmResponse
                        {
                            History = history,
                            ActiveAgent = activeAgent,
                            Context = context
                        };
                    }
                }

                _logger.LogInformation("Swarm execution completed successfully after {Turn} turns", turn);
                return new SwarmResponse
                {
                    History = history,
                    ActiveAgent = activeAgent,
                    Context = context
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Fatal error in Swarm execution: {Message}", e.Message);
                throw new SwarmException("Fatal error in Swarm execution", e);
            }
        }

        private void PrintHistory(List<Message> history)
        {
            try
            {
                var prettyJson = JsonConvert.SerializeObject(history, Formatting.Indented);
                _logger.LogDebug("Conversation History:\n{History}", prettyJson);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error printing history: {Message}", e.Message);
            }
        }

        private ChatRequest BuildRequest(IAgent agent, List<Message> history, Dictionary<string, object> context, string modelOverride)
        {
            var instructions = agent.GetSystemPrompt(context);

            // Build base request
            var request = new ChatRequest
            {
                Model = modelOverride ?? agent.DefaultModel,
                Messages = BuildMessages(instructions, history)
            };

            // Handle tool choice according to the workflow
            if (agent.ToolChoice != null)
            {
                switch (agent.ToolChoice)
                {
                    case ToolChoice.Auto:
                        // Use all available functions
                        var tools = agent.Tools;
                        var functions = new List<FunctionSchema>();

                        if (tools != null)
                        {
                            foreach (var tool in tools)
                            {
                                try
                                {
                                    if (tool.TryGetValue("function", out var functionObj) && 
                                        functionObj is Dictionary<string, object> function)
                                    {
                                        functions.Add(new FunctionSchema
                                        {
                                            Name = function["name"].ToString(),
                                            Description = function["description"].ToString(),
                                            Parameters = (Dictionary<string, object>)function["parameters"]
                                        });
                                    }
                                }
                                catch (Exception e)
                                {
                                    _logger.LogWarning(e, "Failed to convert tool to function schema: {Tool}", tool);
                                }
                            }
                        }

                        if (functions.Any())
                        {
                            request.Functions = functions;
                            request.FunctionCall = "auto";
                        }
                        break;

                    case ToolChoice.None:
                        // Explicitly disable function calling
                        request.FunctionCall = "none";
                        break;
                }
            }

            return request;
        }

        private List<Message> BuildMessages(string instructions, List<Message> history)
        {
            var messages = new List<Message>
            {
                new Message
                {
                    Role = "system",
                    Content = instructions
                }
            };

            messages.AddRange(history);
            return messages;
        }

        private bool HasToolCalls(ChatResponse response)
        {
            return response.Choices?.FirstOrDefault()?.Message?.ToolCalls?.Any() == true;
        }

        private async Task<List<object>> HandleToolCallsAsync(
            ChatResponse response,
            IAgent agent,
            List<Message> history,
            Dictionary<string, object> context)
        {
            var message = response.Choices[0].Message;
            var toolCalls = message.ToolCalls;

            _logger.LogDebug("Processing {Count} tool calls", toolCalls.Length);
            var results = new List<object>();

            foreach (var toolCall in toolCalls)
            {
                try
                {
                    var functionName = toolCall.Function.Name;
                    _logger.LogDebug("Executing tool call: {Function}", functionName);

                    // Get function spec and validate
                    var functionSpec = agent.GetFunctionSpec(functionName);
                    if (functionSpec == null)
                    {
                        throw new SwarmException($"Function not found: {functionName}");
                    }

                    // Parse arguments
                    var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        toolCall.Function.Arguments);

                    // Get method and parameters
                    var method = agent.FindFunction(functionName);
                    var methodParams = method.GetParameters();
                    if (methodParams == null || !methodParams.Any())
                    {
                        throw new SwarmException($"No parameters defined for function: {functionName}");
                    }

                    // Prepare arguments with default values
                    var args = new object[methodParams.Length];
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        var methodParam = methodParams[i];
                        var paramAttribute = methodParam.GetCustomAttribute<ParameterAttribute>();

                        // Check specifically for 'context' parameter
                        if (methodParam.Name == "context" &&
                            methodParam.ParameterType == typeof(Dictionary<string, object>))
                        {
                            args[i] = context;
                            continue;
                        }

                        if (paramAttribute == null)
                        {
                            // Skip parameters without ParameterAttribute
                            continue;
                        }

                        var paramName = methodParam.Name;
                        arguments.TryGetValue(paramName, out var value);

                        if (value == null && !string.IsNullOrEmpty(paramAttribute.DefaultValue))
                        {
                            // Use default value if provided
                            value = ConvertArgument(paramAttribute.DefaultValue, methodParam.ParameterType);
                        }
                        else if (value == null)
                        {
                            throw new SwarmException($"Required parameter missing: {paramName}");
                        }

                        args[i] = ConvertArgument(value, methodParam.ParameterType);
                    }

                    // Invoke the function and store result
                    var result = method.Invoke(agent, args);
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                        if (task.GetType().IsGenericType)
                        {
                            var resultProperty = task.GetType().GetProperty("Result");
                            result = resultProperty?.GetValue(task);
                        }
                    }
                    results.Add(result);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error executing tool call {Function}: {Message}",
                        toolCall.Function.Name, e.Message);
                    throw new SwarmException("Tool call execution failed", e);
                }
            }

            return results;
        }

        private object ConvertArgument(object value, Type targetType)
        {
            if (value == null)
                return null;

            try
            {
                if (targetType == typeof(string))
                {
                    return value.ToString();
                }
                else if (targetType == typeof(int))
                {
                    return Convert.ToInt32(value);
                }
                else if (targetType == typeof(double))
                {
                    return Convert.ToDouble(value);
                }
                else if (targetType == typeof(bool))
                {
                    return Convert.ToBoolean(value);
                }

                // For complex types, use JSON.NET's conversion
                return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), targetType);
            }
            catch (Exception e)
            {
                throw new SwarmException($"Failed to convert argument to type {targetType.Name}", e);
            }
        }

        private async Task<ChatResponse> GetChatCompletionAsync(
            IAgent agent,
            List<Message> history,
            Dictionary<string, object> context,
            string modelOverride,
            bool stream,
            bool debug)
        {
            _logger.LogDebug("Getting chat completion from LLM");
            try
            {
                var request = BuildRequest(agent, history, context, modelOverride);

                if (debug)
                {
                    _logger.LogDebug("Request to LLM: {Request}",
                        JsonConvert.SerializeObject(request, Formatting.Indented));
                }

                var response = await _client.ChatAsync(request);

                if (debug)
                {
                    _logger.LogDebug("Response from LLM: {Response}",
                        JsonConvert.SerializeObject(response, Formatting.Indented));
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get chat completion: {Message}", e.Message);
                throw new SwarmException("Failed to get chat completion", e);
            }
        }

        private void CheckResponseError(ChatResponse completion)
        {
            if (completion == null)
            {
                throw new SwarmException("Received null ChatResponse");
            }

            if (completion.Error != null)
            {
                var error = completion.Error;
                _logger.LogError("LLM error from {Provider}: {Message} (code: {Code})\nRaw error: {Raw}",
                    error.Metadata?.ProviderName ?? "Unknown",
                    error.Message,
                    error.Code,
                    error.Metadata?.Raw ?? string.Empty);

                throw new SwarmException($"LLM error: {error.Message} (provider: {error.Metadata?.ProviderName ?? "Unknown"})");
            }
        }

        private class ContextManager
        {
            private readonly ILogger _logger;
            private readonly object _lock = new object();

            public ContextManager(ILogger logger)
            {
                _logger = logger;
            }

            public Dictionary<string, object> InitializeContext(Dictionary<string, object> initial)
            {
                lock (_lock)
                {
                    var context = new Dictionary<string, object>(initial);
                    ValidateContextUpdates(context);
                    CleanSensitiveData(context);
                    return context;
                }
            }

            public void UpdateContext(Dictionary<string, object> updates, Dictionary<string, object> context)
            {
                lock (_lock)
                {
                    ValidateContextUpdates(updates);
                    foreach (var kvp in updates)
                    {
                        context[kvp.Key] = kvp.Value;
                    }
                    CleanSensitiveData(context);
                    NotifyContextUpdated(context);
                }
            }

            private void ValidateContextUpdates(Dictionary<string, object> updates)
            {
                if (updates == null)
                    return;

                // Validate context size
                if (updates.Count > 100)
                {
                    throw new SwarmException($"Context update too large: {updates.Count} entries");
                }

                // Validate value types and sizes
                foreach (var (key, value) in updates)
                {
                    // Check key format
                    if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-zA-Z0-9_]+$"))
                    {
                        throw new SwarmException($"Invalid context key format: {key}");
                    }

                    // Check value type
                    if (value != null && !(value is string || value is ValueType || value is IDictionary<string, object> || value is IList<object>))
                    {
                        throw new SwarmException($"Invalid context value type for key: {key}");
                    }

                    // Check string length
                    if (value is string strValue && strValue.Length > 10000)
                    {
                        throw new SwarmException($"Context string value too long for key: {key}");
                    }
                }
            }

            private void CleanSensitiveData(Dictionary<string, object> context)
            {
                // Remove temporary and sensitive data if necessary
            }

            private void NotifyContextUpdated(Dictionary<string, object> context)
            {
                // Notify any observers of context changes
                // not implemented yet
            }
        }

        private class ErrorHandler
        {
            private Dictionary<string, object> _savedState;

            public void SaveState(Dictionary<string, object> context)
            {
                _savedState = new Dictionary<string, object>(context);
            }

            public void RestoreState(Dictionary<string, object> context)
            {
                if (_savedState != null)
                {
                    context.Clear();
                    foreach (var kvp in _savedState)
                    {
                        context[kvp.Key] = kvp.Value;
                    }
                }
            }

            public bool CanRetry(Exception e)
            {
                // Implement retry decision logic
                return false;
            }

            public bool NeedsFallback(Exception e)
            {
                // Implement fallback decision logic
                return false;
            }

            public IAgent GetFallbackAgent()
            {
                // Implement fallback agent creation
                return null;
            }
        }
    }

    public class SwarmException : Exception
    {
        public SwarmException(string message) : base(message)
        {
        }

        public SwarmException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class SwarmResponse
    {
        public List<Message> History { get; set; }
        public IAgent ActiveAgent { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }
}
