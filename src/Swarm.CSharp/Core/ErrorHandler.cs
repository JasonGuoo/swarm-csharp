using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core.Exceptions;

namespace Swarm.CSharp.Core;

/// <summary>
/// Handles errors and exceptions in the Swarm framework.
/// </summary>
public class ErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;
    private readonly Dictionary<string, Func<Exception, Task<IAgentResponse>>> _errorHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandler"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for error logging.</param>
    public ErrorHandler(ILogger<ErrorHandler>? logger = null)
    {
        _logger = logger ?? new LoggerFactory().CreateLogger<ErrorHandler>();
        _errorHandlers = new Dictionary<string, Func<Exception, Task<IAgentResponse>>>();
        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Handles an exception and returns an appropriate response.
    /// </summary>
    /// <param name="ex">The exception to handle.</param>
    /// <returns>An agent response.</returns>
    public async Task<IAgentResponse> HandleErrorAsync(Exception ex)
    {
        _logger.LogError(ex, "Error occurred during agent execution");

        var errorCode = ex is SwarmException swarmEx ? swarmEx.ErrorCode : "UNKNOWN_ERROR";
        if (_errorHandlers.TryGetValue(errorCode, out var handler))
        {
            return await handler(ex);
        }

        return await HandleUnknownErrorAsync(ex);
    }

    /// <summary>
    /// Registers a custom error handler for a specific error code.
    /// </summary>
    /// <param name="errorCode">The error code to handle.</param>
    /// <param name="handler">The handler function.</param>
    public void RegisterErrorHandler(string errorCode, Func<Exception, Task<IAgentResponse>> handler)
    {
        _errorHandlers[errorCode] = handler;
    }

    private void RegisterDefaultHandlers()
    {
        _errorHandlers["TOOL_NOT_FOUND"] = HandleToolNotFoundError;
        _errorHandlers["INVALID_ARGUMENTS"] = HandleInvalidArgumentsError;
        _errorHandlers["CONTEXT_ERROR"] = HandleContextError;
        _errorHandlers["LLM_ERROR"] = HandleLLMError;
    }

    private async Task<IAgentResponse> HandleToolNotFoundError(Exception ex)
    {
        var message = "I encountered an error with the requested tool. " +
                     "The tool you're trying to use is not available. " +
                     "Please try a different approach or ask for available tools.";

        return new AgentResponse(message, Array.Empty<IMessage>(), new Dictionary<string, object>());
    }

    private async Task<IAgentResponse> HandleInvalidArgumentsError(Exception ex)
    {
        var message = "I couldn't process the tool call because the arguments were invalid. " +
                     "Please check the required parameters and try again.";

        return new AgentResponse(message, Array.Empty<IMessage>(), new Dictionary<string, object>());
    }

    private async Task<IAgentResponse> HandleContextError(Exception ex)
    {
        var message = "There was an error accessing or updating the context. " +
                     "Some information might be temporarily unavailable.";

        return new AgentResponse(message, Array.Empty<IMessage>(), new Dictionary<string, object>());
    }

    private async Task<IAgentResponse> HandleLLMError(Exception ex)
    {
        var message = "I encountered an error communicating with the language model. " +
                     "This might be due to connectivity issues or rate limiting. " +
                     "Please try again in a moment.";

        return new AgentResponse(message, Array.Empty<IMessage>(), new Dictionary<string, object>());
    }

    private async Task<IAgentResponse> HandleUnknownErrorAsync(Exception ex)
    {
        var message = "I encountered an unexpected error. " +
                     "Please try again or contact support if the issue persists.";

        return new AgentResponse(message, Array.Empty<IMessage>(), new Dictionary<string, object>());
    }
}
