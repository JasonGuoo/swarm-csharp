using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core.Exceptions;
using Swarm.CSharp.LLM;

namespace Swarm.CSharp.Core;

/// <summary>
/// Main orchestrator for the Swarm framework.
/// </summary>
public class Swarm
{
    private readonly ILLMClient _client;
    private readonly ILogger<Swarm> _logger;
    private readonly ErrorHandler _errorHandler;
    private readonly ContextManager _contextManager;
    private const string CtxVarsName = "context_variables";
    private const int DefaultMaxTurns = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="Swarm"/> class.
    /// </summary>
    /// <param name="client">The LLM client to use.</param>
    /// <param name="logger">Optional logger for debugging.</param>
    public Swarm(ILLMClient client, ILogger<Swarm>? logger = null)
    {
        _client = client;
        _logger = logger ?? new LoggerFactory().CreateLogger<Swarm>();
        _errorHandler = new ErrorHandler(new LoggerFactory().CreateLogger<ErrorHandler>());
        _contextManager = new ContextManager(new LoggerFactory().CreateLogger<ContextManager>());
    }

    /// <summary>
    /// Runs an agent with the given request and context.
    /// </summary>
    /// <param name="agent">The agent to run.</param>
    /// <param name="request">The request to process.</param>
    /// <param name="sessionId">Optional session ID for context management.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The agent's response.</returns>
    public async Task<IAgentResponse> RunAsync(
        IAgent agent,
        IAgentRequest request,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        sessionId ??= Guid.NewGuid().ToString();
        var context = _contextManager.GetOrCreateContext(sessionId);

        _logger.LogInformation(
            "Starting Swarm execution with agent: {AgentType}, session: {SessionId}, stream: {Stream}, maxTurns: {MaxTurns}",
            agent.GetType().Name, sessionId, request.Stream, request.MaxTurns);

        try
        {
            var response = await agent.ExecuteAsync(request, context);
            
            if (response.ContextUpdates.Count > 0)
            {
                var updates = new Dictionary<string, object>(response.ContextUpdates);
                await _contextManager.UpdateContextAsync(sessionId, updates);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Swarm execution");
            return await _errorHandler.HandleErrorAsync(ex);
        }
    }

    /// <summary>
    /// Runs an agent with a simple text request.
    /// </summary>
    /// <param name="agent">The agent to run.</param>
    /// <param name="content">The request content.</param>
    /// <param name="sessionId">Optional session ID for context management.</param>
    /// <param name="stream">Whether to stream the response.</param>
    /// <param name="maxTurns">Maximum number of turns.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The agent's response.</returns>
    public Task<IAgentResponse> RunAsync(
        IAgent agent,
        string content,
        string? sessionId = null,
        bool stream = false,
        int maxTurns = DefaultMaxTurns,
        CancellationToken cancellationToken = default)
    {
        var request = new AgentRequest(content, maxTurns, stream);
        return RunAsync(agent, request, sessionId, cancellationToken);
    }

    /// <summary>
    /// Clears the context for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    public void ClearContext(string sessionId)
    {
        _contextManager.ClearContext(sessionId);
    }

    /// <summary>
    /// Removes a session and its context.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    public void RemoveSession(string sessionId)
    {
        _contextManager.RemoveContext(sessionId);
    }
}
