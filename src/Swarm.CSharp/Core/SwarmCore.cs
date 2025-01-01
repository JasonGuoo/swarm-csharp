using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Function;
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
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private const string CtxVarsName = "context_variables";
        private const int DefaultMaxTurns = 10;
        private readonly FunctionInvoker _functionInvoker;

        public SwarmCore(ILLMClient client, ILogger<SwarmCore> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = new ErrorHandler();
            _contextManager = new ContextManager(logger);
            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _functionInvoker = new FunctionInvoker();
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
            _logger.LogDebug("Initial messages count: {Count}", messages?.Count ?? 0);
            _logger.LogDebug("Context variables: {@Variables}", contextVariables);

            try
            {
                IAgent activeAgent = agent;
                var context = _contextManager.InitializeContext(contextVariables);
                _logger.LogDebug("Initialized context with {Count} variables: {@Context}", context.Count, context);

                var history = new List<Message>(messages);
                int initLen = messages.Count;
                int turn = 0;

                while (history.Count - initLen < maxTurns && activeAgent != null)
                {
                    turn++;
                    _logger.LogDebug("Starting turn {Turn}/{MaxTurns} with agent {Agent}",
                        turn, maxTurns, activeAgent.GetType().Name);

                    try
                    {
                        if (debug)
                        {
                            _logger.LogDebug("Current history before completion:");
                            LogHistory(history);
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
                                _logger.LogDebug("Final response message: {Content}", responseMessage.Content);
                                history.Add(responseMessage);
                            }
                            break;
                        }

                        // Add the response message to history
                        if (completion.Choices?.Any() == true)
                        {
                            var responseMessage = completion.Choices[0].Message;
                            history.Add(responseMessage);
                            _logger.LogDebug("Added response with {Count} tool calls to history",
                                responseMessage.ToolCalls?.Length ?? 0);

                            // Handle any tool calls
                            if (HasToolCalls(completion))
                            {
                                _logger.LogDebug("Processing {Count} tool calls",
                                    responseMessage.ToolCalls?.Length ?? 0);

                                var results = await HandleToolCallsAsync(completion, activeAgent, history, context);
                                var toolCalls = responseMessage.ToolCalls;

                                // Process each result and add to history
                                for (int i = 0; i < results.Count; i++)
                                {
                                    var result = results[i];
                                    if (result is IAgent newAgent)
                                    {
                                        activeAgent = newAgent;
                                        _logger.LogDebug("Agent switched to: {NewAgent}",
                                            newAgent.GetType().Name);
                                    }

                                    var toolCall = toolCalls[i];
                                    var callMessage = new Message
                                    {
                                        Role = "tool",
                                        ToolCallId = toolCall.Id,
                                        ToolName = toolCall.Function.Name,
                                        Content = result.ToString()
                                    };
                                    _logger.LogDebug("Tool call result for {Tool}: {Result}",
                                        toolCall.Function.Name, result);
                                    history.Add(callMessage);
                                }
                            }
                        }

                        if (debug)
                        {
                            _logger.LogDebug("Updated history after processing response:");
                            LogHistory(history);
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
                _logger.LogDebug("Final context: {@Context}", context);
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

        private void LogHistory(List<Message> history)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var prettyJson = JsonSerializer.Serialize(history, options);
                _logger.LogDebug("Conversation History:\n{History}", prettyJson);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to log conversation history");
            }
        }

        private async Task<ChatRequest> BuildRequestAsync(IAgent agent, List<Message> history, Dictionary<string, object> context, string modelOverride = null)
        {
            var request = new ChatRequest
            {
                Model = modelOverride ?? _client.Model,
                Messages = history,
                Temperature = 0.7f
            };

            var toolChoice = agent.GetToolChoice();
            if (toolChoice != null)
            {
                switch (toolChoice)
                {
                    case ToolChoice.Required:
                        // Get all available functions
                        var functions = new List<FunctionSchema>();
                        var tools = agent.GetTools();

                        foreach (var tool in tools)
                        {
                            try
                            {
                                var function = tool["function"] as Dictionary<string, object>;
                                if (function != null)
                                {
                                    var schema = new FunctionSchema
                                    {
                                        Name = function["name"]?.ToString(),
                                        Description = function["description"]?.ToString(),
                                        Parameters = function["parameters"] as Dictionary<string, object>
                                    };
                                    functions.Add(schema);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger?.LogWarning("Failed to convert tool to function schema: {Tool}, Error: {Error}",
                                    JsonSerializer.Serialize(tool), e.Message);
                            }
                        }

                        if (!functions.Any())
                        {
                            throw new SwarmException("Tool choice is required but no tools are available");
                        }

                        request.Functions = functions;
                        request.FunctionCall = "auto";
                        break;

                    case ToolChoice.None:
                        // No functions needed
                        request.FunctionCall = "none";
                        break;

                    case ToolChoice.Auto:
                    default:
                        // Add functions but don't require their use
                        var autoFunctions = new List<FunctionSchema>();
                        var autoTools = agent.GetTools();

                        foreach (var tool in autoTools)
                        {
                            try
                            {
                                var function = tool["function"] as Dictionary<string, object>;
                                if (function != null)
                                {
                                    var schema = new FunctionSchema
                                    {
                                        Name = function["name"]?.ToString(),
                                        Description = function["description"]?.ToString(),
                                        Parameters = function["parameters"] as Dictionary<string, object>
                                    };
                                    autoFunctions.Add(schema);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger?.LogWarning("Failed to convert tool to function schema: {Tool}, Error: {Error}",
                                    JsonSerializer.Serialize(tool), e.Message);
                            }
                        }

                        if (autoFunctions.Any())
                        {
                            request.Functions = autoFunctions;
                            request.FunctionCall = "auto";
                        }
                        break;
                }
            }

            return request;
        }

        private async Task<List<object>> HandleToolCallsAsync(
            ChatResponse response,
            IAgent agent,
            List<Message> history,
            Dictionary<string, object> context)
        {
            var message = response.Choices[0].Message;
            var toolCalls = message.ToolCalls;

            _logger.LogDebug("Processing {Count} tool calls", toolCalls?.Length ?? 0);
            var results = new List<object>();

            foreach (var toolCall in toolCalls)
            {
                try
                {
                    var functionName = toolCall.Function.Name;
                    _logger.LogDebug("Executing tool call: {Function} with arguments: {Arguments}",
                        functionName, toolCall.Function.Arguments);

                    // Parse arguments
                    var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        toolCall.Function.Arguments,
                        _jsonSerializerOptions);

                    // Add context to arguments
                    var allArguments = new Dictionary<string, object>(
                        arguments.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (object)JsonSerializer.Deserialize(
                                kvp.Value.GetRawText(),
                                typeof(object),
                                _jsonSerializerOptions)
                        )
                    );
                    allArguments["context"] = context;

                    // Get method
                    var method = agent.FindFunction(functionName);
                    if (method == null)
                    {
                        throw new SwarmException($"Function {functionName} not found");
                    }

                    // Invoke function using FunctionInvoker
                    object result;
                    try
                    {
                        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                        {
                            result = await _functionInvoker.InvokeAsync(method, agent, allArguments);
                        }
                        else
                        {
                            result = _functionInvoker.Invoke(method, agent, allArguments);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new SwarmException($"Function {functionName} execution failed: {e.Message}", e);
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
                var request = await BuildRequestAsync(agent, history, context, modelOverride);

                if (debug)
                {
                    _logger.LogDebug("Request to LLM: {Request}",
                        JsonSerializer.Serialize(request, _jsonSerializerOptions));
                }

                var response = await _client.ChatAsync(request);

                if (debug)
                {
                    _logger.LogDebug("Response from LLM: {Response}",
                        JsonSerializer.Serialize(response, _jsonSerializerOptions));
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

            var error = completion.GetFieldValue<Dictionary<string, object>>("error");
            if (error != null && error.Count > 0)
            {
                string errorMessage = error.TryGetValue("message", out var message) ? message?.ToString() : "Unknown error";
                int errorCode = error.TryGetValue("code", out var code) && code != null ? Convert.ToInt32(code) : 0;

                if (error.TryGetValue("metadata", out var metadataObj) && metadataObj is Dictionary<string, object> metadata)
                {
                    string rawError = metadata.TryGetValue("raw", out var raw) ? raw?.ToString() : "";
                    string provider = metadata.TryGetValue("provider_name", out var providerName) ? providerName?.ToString() : "";
                    throw new SwarmException($"{errorMessage} (Code: {errorCode}, Provider: {provider}, Raw: {rawError})");
                }

                throw new SwarmException($"{errorMessage} (Code: {errorCode})");
            }
        }

        private bool HasToolCalls(ChatResponse response)
        {
            if (response?.Choices == null || response.Choices.Count == 0)
            {
                return false;
            }

            var message = response.Choices[0].Message;
            return message?.ToolCalls != null && message.ToolCalls.Length > 0;
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
