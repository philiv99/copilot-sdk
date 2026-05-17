#pragma warning disable CS8601 // Nullable reference warnings suppressed; null-coalescing operators and null checks ensure type safety
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using DomainSessionConfig = CopilotSdk.Api.Models.Domain.SessionConfig;
using DomainSystemMessageConfig = CopilotSdk.Api.Models.Domain.SystemMessageConfig;

namespace CopilotSdk.Api.Services;

/// <inheritdoc cref="IAgentOrchestrationService"/>
public class AgentOrchestrationService : IAgentOrchestrationService
{
    private static readonly TimeSpan ChildSessionPrepareTimeout = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Maximum time a single delegated task is allowed to run before the orchestrator
    /// gives up waiting for the child. Keeps the parent's <c>delegate_to_agent</c> tool
    /// from blocking forever if the child session deadlocks (e.g. a hung shell tool, a
    /// CLI process that never emits an idle event, etc.).
    /// </summary>
    private static readonly TimeSpan DelegationTimeout = TimeSpan.FromMinutes(15);

    private readonly CopilotClientManager _clientManager;
    private readonly IAgentTeamService _agentTeamService;
    private readonly SessionEventDispatcher _eventDispatcher;
    private readonly IPermissionPolicyService _permissionPolicyService;
    private readonly ILogger<AgentOrchestrationService> _logger;

    // Map: parentSessionId -> (agentId -> ChildSession)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ChildSession>> _childrenByParent = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _prepareLocks = new(StringComparer.OrdinalIgnoreCase);

    public AgentOrchestrationService(
        CopilotClientManager clientManager,
        IAgentTeamService agentTeamService,
        SessionEventDispatcher eventDispatcher,
        IPermissionPolicyService permissionPolicyService,
        ILogger<AgentOrchestrationService> logger)
    {
        _clientManager = clientManager;
        _agentTeamService = agentTeamService;
        _eventDispatcher = eventDispatcher;
        _permissionPolicyService = permissionPolicyService;
        _logger = logger;
    }

    public AIFunction BuildDelegateTool(string parentSessionId, IReadOnlyList<string> availableAgentIds, string parentModel)
    {
        var allowed = availableAgentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Where(id => !string.Equals(id, "orchestrator", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DelegateAIFunction(this, parentSessionId, parentModel, allowed, _logger);
    }

    public AIFunction BuildDelegationProbeTool(string parentSessionId)
    {
        return AIFunctionFactory.Create(
            InvokeProbeAsync,
            "delegation_probe",
            "Diagnostic only. Call this when asked to test delegation plumbing. It returns immediately if the backend host receives custom tool callbacks.");

        async Task<string> InvokeProbeAsync()
        {
            _logger.LogInformation("delegation_probe handler entered: parent={ParentSessionId}", parentSessionId);

            try
            {
                await _eventDispatcher.DispatchProgressAsync(
                    parentSessionId,
                    "Delegation probe reached the backend tool handler",
                    "delegation-probe",
                    isActive: false,
                    step: "probe-callback-received",
                    toolName: "delegation_probe").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to dispatch delegation_probe progress for {ParentSessionId}", parentSessionId);
            }

            return "delegation_probe ok: backend custom tool callback received.";
        }
    }

    /// <summary>
    /// Custom <see cref="AIFunction"/> implementation for <c>delegate_to_agent</c>.
    /// We use a hand-written subclass instead of <see cref="AIFunctionFactory"/> so we can:
    ///   1. Publish an explicit JSON schema with the exact required parameters
    ///      (<c>agent_id</c>, <c>task</c>) the model should emit.
    ///   2. Tolerate aliased / extra keys at invocation time. Claude has a strong prior
    ///      toward Claude Code's Task tool shape (<c>name</c>, <c>prompt</c>,
    ///      <c>subagent_type</c>, <c>description</c>, <c>mode</c>) and will sometimes
    ///      emit those instead. AIFunctionFactory's reflection-based binder treats any
    ///      schema mismatch as a missing-parameter error and the SDK then never invokes
    ///      the function - the parent turn waits forever for a tool result that never
    ///      arrives. By owning the binder we always invoke and always return a string.
    /// </summary>
    private sealed class DelegateAIFunction : AIFunction
    {
        private readonly AgentOrchestrationService _owner;
        private readonly string _parentSessionId;
        private readonly string _parentModel;
        private readonly string[] _allowed;
        private readonly string _allowedDescription;
        private readonly ILogger _logger;
        private readonly System.Text.Json.JsonElement _schema;
        private readonly string _description;

        public DelegateAIFunction(
            AgentOrchestrationService owner,
            string parentSessionId,
            string parentModel,
            string[] allowed,
            ILogger logger)
        {
            _owner = owner;
            _parentSessionId = parentSessionId;
            _parentModel = parentModel;
            _allowed = allowed;
            _logger = logger;
            _allowedDescription = allowed.Length == 0
                ? "(no specialist agents are configured for this session)"
                : string.Join(", ", allowed);

            _description =
                "Delegate a single, well-scoped task to one specialist agent. The specialist " +
                "runs in its own Copilot session with full shell/file/git tools and will " +
                "actually execute the work (do NOT narrate the work yourself - call this tool). " +
                "This call BLOCKS until the specialist finishes its task and returns the " +
                "specialist's final summary as a string. Always send exactly two arguments: " +
                "agent_id and task. Do not send name/prompt/description/agent_type/mode.";

            var schemaJson = BuildSchemaJson(allowed);
            _schema = System.Text.Json.JsonDocument.Parse(schemaJson).RootElement.Clone();
        }

        public override string Name => "delegate_to_agent";
        public override string Description => _description;
        public override System.Text.Json.JsonElement JsonSchema => _schema;

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var keysSummary = arguments == null
                ? "(null)"
                : string.Join(",", arguments.Keys);
            _logger.LogInformation(
                "delegate_to_agent handler entered: parent={ParentSessionId} argKeys=[{Keys}]",
                _parentSessionId, keysSummary);

            try
            {
                await _owner._eventDispatcher.DispatchProgressAsync(
                    _parentSessionId,
                    "delegate_to_agent reached the backend tool handler",
                    "delegation-dispatch",
                    isActive: true,
                    step: "delegate-callback-received",
                    toolName: Name).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to dispatch delegate_to_agent handler progress for {ParentSessionId}", _parentSessionId);
            }

            string? agentRaw = TryGetStringArg(arguments, "agent_id", "agentId", "agent", "subagent_type", "agent_type");
            string? taskRaw = TryGetStringArg(arguments, "task", "prompt", "description", "instruction", "instructions");

            if (string.IsNullOrWhiteSpace(agentRaw))
            {
                _logger.LogWarning(
                    "delegate_to_agent missing agent_id (received keys: [{Keys}])", keysSummary);
                return $"Error: agent_id is required. Allowed agents: {_allowedDescription}.";
            }
            if (string.IsNullOrWhiteSpace(taskRaw))
            {
                _logger.LogWarning(
                    "delegate_to_agent missing task (received keys: [{Keys}])", keysSummary);
                return "Error: task is required (pass the task description as 'task').";
            }

            var agentId = agentRaw.Trim();
            if (!_allowed.Contains(agentId, StringComparer.OrdinalIgnoreCase))
            {
                return $"Error: agent '{agentId}' is not configured for this session. Allowed agents: {_allowedDescription}.";
            }

            try
            {
                _logger.LogInformation(
                    "delegate_to_agent invoked: parent={ParentSessionId} agent={AgentId} taskLen={TaskLen}",
                    _parentSessionId, agentId, taskRaw.Length);
                var result = await _owner.DelegateAsync(_parentSessionId, agentId, taskRaw, _parentModel, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "delegate_to_agent returning: parent={ParentSessionId} agent={AgentId} resultLen={ResultLen}",
                    _parentSessionId, agentId, result?.Length ?? 0);
                return (object?)result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "delegate_to_agent cancelled: parent={ParentSessionId} agent={AgentId}",
                    _parentSessionId, agentId);
                return $"Delegation to '{agentId}' was aborted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delegation to {AgentId} for parent {ParentSessionId} failed", agentId, _parentSessionId);
                return $"Error: delegation to '{agentId}' failed: {ex.Message}";
            }
        }

        private static string BuildSchemaJson(string[] allowed)
        {
            // IMPORTANT: keep this schema deliberately permissive. The Copilot CLI
            // validates the LLM's tool-call payload against this schema BEFORE it
            // dispatches `tool.call` back to the .NET host. In local testing, strict
            // schemas can produce a `tool.execution_start` event without ever invoking
            // `InvokeCoreAsync`, which leaves the parent turn waiting forever. This
            // schema accepts any object; .NET validates required values and allowed
            // agents after dispatch.
            var allowedDesc = allowed.Length == 0
                ? string.Empty
                : " Must be one of: " + string.Join(", ", allowed) + ".";

            var agentDescription = "The id of the specialist agent to delegate to." + allowedDesc;
            var taskDescription = "The complete, self-contained task description for the specialist. Include any context the specialist needs because each delegation is independent.";

            var json = new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "object",
                ["properties"] = new System.Text.Json.Nodes.JsonObject
                {
                    ["agent_id"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = agentDescription,
                        ["type"] = "string"
                    },
                    ["task"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = taskDescription,
                        ["type"] = "string"
                    },
                    ["agentId"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = agentDescription,
                        ["type"] = "string"
                    },
                    ["agent"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = agentDescription,
                        ["type"] = "string"
                    },
                    ["subagent_type"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = agentDescription,
                        ["type"] = "string"
                    },
                    ["prompt"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = taskDescription,
                        ["type"] = "string"
                    },
                    ["description"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["description"] = taskDescription,
                        ["type"] = "string"
                    }
                }
            };
            return json.ToJsonString();
        }

        private static string? TryGetStringArg(AIFunctionArguments? args, params string[] keys)
        {
            if (args == null) return null;
            foreach (var key in keys)
            {
                if (!args.TryGetValue(key, out var raw) || raw == null) continue;
                switch (raw)
                {
                    case string s:
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                        break;
                    case System.Text.Json.JsonElement je:
                        if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var v = je.GetString();
                            if (!string.IsNullOrWhiteSpace(v)) return v;
                        }
                        else if (je.ValueKind != System.Text.Json.JsonValueKind.Null && je.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                        {
                            var v = je.ToString();
                            if (!string.IsNullOrWhiteSpace(v)) return v;
                        }
                        break;
                    default:
                        var fallback = raw.ToString();
                        if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
                        break;
                }
            }
            return null;
        }
    }

    public async Task PrepareChildrenAsync(
        string parentSessionId,
        IReadOnlyList<string> availableAgentIds,
        string parentModel,
        CancellationToken cancellationToken = default)
    {
        var agentIds = availableAgentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Where(id => !string.Equals(id, "orchestrator", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (agentIds.Length == 0)
        {
            return;
        }

        foreach (var agentId in agentIds)
        {
            await EnsureChildPreparedAsync(parentSessionId, agentId, parentModel, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> DelegateAsync(
        string parentSessionId,
        string agentId,
        string task,
        string parentModel,
        CancellationToken cancellationToken)
    {
        var executionId = Guid.NewGuid().ToString("N");
        if (!TryGetPreparedChild(parentSessionId, agentId, out var child))
        {
            try
            {
                await _eventDispatcher.DispatchProgressAsync(
                    parentSessionId,
                    $"Unable to delegate to {agentId}: specialist session was not prepared before the parent turn started.",
                    "agent-error",
                    isActive: false,
                    agentId: agentId,
                    executionId: executionId,
                    step: "missing-child-session").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to dispatch missing child progress for {AgentId}", agentId);
            }

            return $"Error: specialist agent '{agentId}' was not prepared. Start a new team session or retry after the backend prepares specialist sessions.";
        }

        return await RunDelegationAsync(parentSessionId, agentId, task, child, executionId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a delegated task on the prepared child session and waits for it to finish.
    /// The orchestrator's <c>delegate_to_agent</c> tool blocks here, which is why the
    /// frontend's "Running…" badge accurately reflects child progress and why the
    /// orchestrator naturally pauses until the specialist returns.
    /// </summary>
    private async Task<string> RunDelegationAsync(
        string parentSessionId,
        string agentId,
        string task,
        ChildSession child,
        string executionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting delegation for {AgentId}", agentId);
        // Serialize delegations against the same prepared child so two concurrent
        // delegate_to_agent calls don't interleave on a single Copilot session.
        await _eventDispatcher.DispatchProgressAsync(
            parentSessionId,
            $"Delegation queued for {agentId}; waiting for specialist availability",
            "delegation-queued",
            agentId: agentId,
            executionId: executionId,
            step: "waiting-for-specialist-lock").ConfigureAwait(false);

        await child.ExecutionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Acquired execution lock for {AgentId}", agentId);
        child.ProgressContext.Begin(executionId);
        _logger.LogDebug("Began progress context for {AgentId}", agentId);

        try
        {
            _logger.LogDebug("Starting progress context for {AgentId}: {TaskSummary}", agentId, SummarizeTask(task));
            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                $"Starting delegated task for {agentId}: {SummarizeTask(task)}",
                "agent",
                agentId: agentId,
                executionId: executionId,
                step: "delegation-start").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to dispatch delegation start for {AgentId}", agentId);
        }

        // Set up a one-shot completion source that fires when the child's turn ends or it
        // becomes idle, so we can return the final answer to the orchestrator.
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var assistantText = new StringBuilder();
        var toolCount = 0;
        var errorText = (string?)null;
        var timedOut = false;

        // Tracks what the child agent is currently doing so the parent UI can surface
        // intermediate status. Updated from the SDK event thread; read by the heartbeat
        // task. Reference assignment to a string field is atomic in .NET.
        string lastActivity = "starting delegated task";
        var startedAt = DateTime.UtcNow;

        SessionEventHandler completionHandler = (SessionEvent evt) =>
        {
            try
            {
                _logger.LogDebug(
                    "Delegation {ExecutionId} child {ChildId} ({AgentId}) saw event {EventType}",
                    executionId, child.Session.SessionId, agentId, evt.Type);

                switch (evt)
                {
                    case AssistantMessageEvent m:
                        if (!string.IsNullOrEmpty(m.Data.Content))
                        {
                            if (assistantText.Length > 0) assistantText.AppendLine();
                            assistantText.Append(m.Data.Content);
                        }
                        lastActivity = "wrote a response";
                        _logger.LogInformation(
                            "Delegation {ExecutionId} {AgentId}: assistant message received ({Length} chars, total {Total})",
                            executionId, agentId, m.Data.Content?.Length ?? 0, assistantText.Length);
                        break;
                    case AssistantMessageDeltaEvent:
                        lastActivity = "streaming response";
                        break;
                    case ToolExecutionStartEvent ts:
                        var summary = SessionEventDispatcher.ExtractToolActionSummary(ts.Data.Arguments);
                        lastActivity = string.IsNullOrWhiteSpace(summary)
                            ? $"running tool '{ts.Data.ToolName}'"
                            : $"running tool '{ts.Data.ToolName}': {summary}";
                        _logger.LogInformation(
                            "Delegation {ExecutionId} {AgentId}: tool start '{ToolName}' (callId={CallId})",
                            executionId, agentId, ts.Data.ToolName, ts.Data.ToolCallId);
                        break;
                    case ToolExecutionCompleteEvent t:
                        Interlocked.Increment(ref toolCount);
                        if (t.Data.Error != null)
                        {
                            errorText ??= t.Data.Error.Message;
                            lastActivity = $"tool failed: {t.Data.Error.Message}";
                            _logger.LogWarning(
                                "Delegation {ExecutionId} {AgentId}: tool (callId={CallId}) failed: {Error}",
                                executionId, agentId, t.Data.ToolCallId, t.Data.Error.Message);
                        }
                        else
                        {
                            lastActivity = "finished a tool call";
                            _logger.LogInformation(
                                "Delegation {ExecutionId} {AgentId}: tool (callId={CallId}) completed (toolCount={ToolCount})",
                                executionId, agentId, t.Data.ToolCallId, toolCount);
                        }
                        break;
                    case AssistantTurnEndEvent:
                        _logger.LogInformation(
                            "Delegation {ExecutionId} {AgentId}: AssistantTurnEnd received -> resolving completion",
                            executionId, agentId);
                        completion.TrySetResult(assistantText.ToString());
                        break;
                    case SessionIdleEvent:
                        _logger.LogInformation(
                            "Delegation {ExecutionId} {AgentId}: SessionIdle received -> resolving completion",
                            executionId, agentId);
                        completion.TrySetResult(assistantText.ToString());
                        break;
                    case SessionErrorEvent err:
                        errorText ??= err.Data.Message;
                        _logger.LogWarning(
                            "Delegation {ExecutionId} {AgentId}: SessionError '{Message}' -> resolving completion",
                            executionId, agentId, err.Data.Message);
                        completion.TrySetResult(assistantText.ToString());
                        break;
                    case AbortEvent:
                        _logger.LogWarning(
                            "Delegation {ExecutionId} {AgentId}: AbortEvent received -> resolving completion",
                            executionId, agentId);
                        completion.TrySetResult(assistantText.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Delegation {ExecutionId} {AgentId}: completion handler threw on event {EventType}",
                    executionId, agentId, evt.Type);
                completion.TrySetException(ex);
            }
        };

        IDisposable? completionSubscription = null;
        using var heartbeatCts = new CancellationTokenSource();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DelegationTimeout);
        Task? heartbeatTask = null;

        try
        {
            completionSubscription = child.Session.On(completionHandler);
            _logger.LogInformation(
                "Delegation {ExecutionId} -> {AgentId} on child session {ChildId}: completion handler subscribed, task length {TaskLen} chars",
                executionId, agentId, child.Session.SessionId, task?.Length ?? 0);

            // Background heartbeat: emits a progress event every few seconds so the user
            // sees the delegation is alive and what the agent is doing, even when the
            // child session goes silent (e.g. waiting on a long shell tool).
            heartbeatTask = Task.Run(async () =>
            {
                try
                {
                    while (!heartbeatCts.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), heartbeatCts.Token).ConfigureAwait(false);
                        if (heartbeatCts.IsCancellationRequested) break;
                        var elapsed = (int)(DateTime.UtcNow - startedAt).TotalSeconds;
                        var snapshot = lastActivity;
                        _logger.LogInformation(
                            "Delegation heartbeat {ExecutionId} {AgentId} elapsed={Elapsed}s activity='{Activity}' tools={Tools}",
                            executionId, agentId, elapsed, snapshot, toolCount);
                        try
                        {
                            await _eventDispatcher.DispatchProgressAsync(
                                parentSessionId,
                                $"{agentId} working ({elapsed}s): {snapshot}",
                                "delegating",
                                isActive: true,
                                agentId: agentId,
                                executionId: executionId,
                                step: snapshot).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Heartbeat dispatch failed for delegation to {AgentId}", agentId);
                        }
                    }
                }
                catch (OperationCanceledException) { /* expected on cancellation */ }
            }, heartbeatCts.Token);

            // Send the task to the child. We pass CancellationToken.None to SendAsync so a
            // parent cancellation doesn't tear down the RPC mid-write; we instead rely on
            // the timeoutCts wired into the await on completion.Task below.
            _logger.LogInformation("Delegating {ExecutionId} to {AgentId} on session {ChildId}",
                executionId, agentId, child.Session.SessionId);
            var sendStart = DateTime.UtcNow;
            await child.Session.SendAsync(new MessageOptions
            {
                Prompt = task,
                Mode = "enqueue"
            }, CancellationToken.None).ConfigureAwait(false);
            _logger.LogInformation(
                "Delegation {ExecutionId} {AgentId}: SendAsync returned in {Elapsed}ms, awaiting completion (timeout={TimeoutMin}min)",
                executionId, agentId, (int)(DateTime.UtcNow - sendStart).TotalMilliseconds, DelegationTimeout.TotalMinutes);

            // Await child completion with a hard timeout so the orchestrator's tool can
            // never block forever on a deadlocked specialist.
            using (timeoutCts.Token.Register(() =>
            {
                timedOut = true;
                _logger.LogWarning(
                    "Delegation {ExecutionId} {AgentId}: timeout fired after {TimeoutMin}min (parentCancelled={ParentCancelled})",
                    executionId, agentId, DelegationTimeout.TotalMinutes, cancellationToken.IsCancellationRequested);
                completion.TrySetCanceled(timeoutCts.Token);
            }))
            {
                try
                {
                    await completion.Task.ConfigureAwait(false);
                    _logger.LogInformation(
                        "Delegation {ExecutionId} {AgentId}: completion.Task resolved (assistantChars={Chars}, tools={Tools})",
                        executionId, agentId, assistantText.Length, toolCount);
                }
                catch (OperationCanceledException)
                {
                    // Surfaces below via the timedOut / cancellation path.
                    _logger.LogWarning(
                        "Delegation {ExecutionId} {AgentId}: completion.Task cancelled (timedOut={TimedOut}, parentCancelled={ParentCancelled})",
                        executionId, agentId, timedOut, cancellationToken.IsCancellationRequested);
                }
            }

            // Best-effort: stop the child if we gave up waiting so it doesn't keep
            // running detached and emitting events for a tool the parent already
            // considers complete.
            if (timedOut || cancellationToken.IsCancellationRequested)
            {
                try { await child.Session.AbortAsync().ConfigureAwait(false); }
                catch (Exception ex) { _logger.LogDebug(ex, "Abort after timeout failed for {AgentId}", agentId); }
            }

            var finalText = assistantText.ToString();
            var resultBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(finalText))
            {
                resultBuilder.Append(finalText);
            }
            else if (timedOut)
            {
                resultBuilder.Append($"(agent '{agentId}' did not return a response within {DelegationTimeout.TotalMinutes:0} minutes)");
            }
            else if (cancellationToken.IsCancellationRequested)
            {
                resultBuilder.Append($"(delegation to '{agentId}' was cancelled by the parent)");
            }
            else
            {
                resultBuilder.Append($"(agent '{agentId}' produced no assistant message)");
            }

            resultBuilder.AppendLine();
            resultBuilder.AppendLine();
            resultBuilder.Append($"[delegation summary] agent={agentId} tools_executed={toolCount}");
            if (timedOut)
            {
                resultBuilder.AppendLine();
                resultBuilder.Append($"[delegation summary] timed_out_after={DelegationTimeout.TotalMinutes:0}min");
            }
            if (!string.IsNullOrEmpty(errorText))
            {
                resultBuilder.AppendLine();
                resultBuilder.Append($"[delegation summary] last_error={errorText}");
            }

            // Fire a handoff progress event so the activity log shows control returning.
            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                timedOut
                    ? $"Delegation to {agentId} timed out after {DelegationTimeout.TotalMinutes:0} minutes"
                    : $"Handing off to orchestrator from {agentId}",
                "handoff",
                isActive: false,
                agentId: agentId,
                executionId: executionId,
                step: timedOut ? "delegation-timeout" : "handoff").ConfigureAwait(false);

            _logger.LogInformation("Delegation {ExecutionId} to {AgentId} completed (timedOut={TimedOut}, tools={ToolCount})",
                executionId, agentId, timedOut, toolCount);

            return resultBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delegation {ExecutionId} to {AgentId} failed", executionId, agentId);
            try
            {
                await _eventDispatcher.DispatchProgressAsync(
                    parentSessionId,
                    $"{agentId} delegation failed: {ex.Message}",
                    "agent-error",
                    isActive: false,
                    agentId: agentId,
                    executionId: executionId,
                    step: "delegation-failed").ConfigureAwait(false);
            }
            catch (Exception dispatchEx)
            {
                _logger.LogDebug(dispatchEx, "Failed to dispatch delegation failure for {AgentId}", agentId);
            }

            return $"Error: delegation to '{agentId}' failed: {ex.Message}";
        }
        finally
        {
            // Stop the heartbeat first so we don't emit progress after handoff/abort.
            try { heartbeatCts.Cancel(); } catch { /* ignore */ }
            if (heartbeatTask != null)
            {
                try { await heartbeatTask.ConfigureAwait(false); }
                catch { /* heartbeat exits via OperationCanceledException */ }
            }
            completionSubscription?.Dispose();
            child.ProgressContext.Clear(executionId);
            child.ExecutionLock.Release();
        }
    }

    private bool TryGetPreparedChild(string parentSessionId, string agentId, out ChildSession child)
    {
        child = null!;
        return _childrenByParent.TryGetValue(parentSessionId, out var children)
            && children.TryGetValue(agentId, out child!);
    }

    private async Task<ChildSession> EnsureChildPreparedAsync(
        string parentSessionId,
        string agentId,
        string parentModel,
        CancellationToken cancellationToken)
    {
        var children = _childrenByParent.GetOrAdd(parentSessionId, _ => new ConcurrentDictionary<string, ChildSession>(StringComparer.OrdinalIgnoreCase));
        if (children.TryGetValue(agentId, out var existing))
        {
            return existing;
        }

        var lockKey = $"{parentSessionId}::{agentId}";
        var prepareLock = _prepareLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await prepareLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (children.TryGetValue(agentId, out existing))
            {
                return existing;
            }

            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                $"Preparing specialist session for {agentId}",
                "agent-prepare",
                isActive: true,
                agentId: agentId,
                step: "prepare-child-session").ConfigureAwait(false);

            using var prepareCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            prepareCts.CancelAfter(ChildSessionPrepareTimeout);

            var child = await CreateChildAsync(parentSessionId, agentId, parentModel, prepareCts.Token).ConfigureAwait(false);
            children[agentId] = child;

            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                $"Specialist session ready for {agentId}",
                "agent-ready",
                isActive: false,
                agentId: agentId,
                step: "child-session-ready").ConfigureAwait(false);

            return child;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                $"Timed out preparing specialist session for {agentId} after {ChildSessionPrepareTimeout.TotalSeconds:0} seconds.",
                "agent-error",
                isActive: false,
                agentId: agentId,
                step: "prepare-child-session").ConfigureAwait(false);
            throw new TimeoutException($"Timed out preparing specialist session for '{agentId}'.", ex);
        }
        catch (Exception ex)
        {
            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                $"Failed to prepare specialist session for {agentId}: {ex.Message}",
                "agent-error",
                isActive: false,
                agentId: agentId,
                step: "prepare-child-session").ConfigureAwait(false);
            throw;
        }
        finally
        {
            prepareLock.Release();
        }
    }

    private async Task<ChildSession> CreateChildAsync(
        string parentSessionId,
        string agentId,
        string parentModel,
        CancellationToken cancellationToken)
    {
        // Load the agent's prompt
        var detail = await _agentTeamService.GetAgentDetailAsync(agentId, cancellationToken);
        if (detail == null)
        {
            throw new InvalidOperationException($"Agent '{agentId}' definition not found.");
        }

        // Sanitize child id so it satisfies the session id regex used by the API.
        var sanitizedAgent = SanitizeForSessionId(agentId);
        var sanitizedParent = SanitizeForSessionId(parentSessionId);
        var childIdSeed = $"{sanitizedParent}__{sanitizedAgent}__{Guid.NewGuid():N}";
        var childId = childIdSeed.Substring(0, Math.Min(120, childIdSeed.Length));

        var systemMessageContent = BuildChildSystemMessage(agentId, detail.PromptContent);
        var progressContext = new DelegationProgressContext();

        var config = new DomainSessionConfig
        {
            SessionId = childId,
            Model = parentModel,
            Streaming = true,
            // No AvailableTools/ExcludedTools: leave the full Copilot CLI tool set enabled
            // so the agent can actually run shell commands, edit files, run git, etc.
            SystemMessage = new DomainSystemMessageConfig
            {
                Mode = "append",
                Content = systemMessageContent
            },
            OnPermissionRequest = async (request, invocation) =>
            {
                await _eventDispatcher.DispatchProgressAsync(
                    parentSessionId,
                    $"{agentId} requested {request.Kind} permission",
                    "permission-request",
                    isActive: true,
                    agentId: agentId,
                    executionId: progressContext.ExecutionId,
                    step: "permission-request").ConfigureAwait(false);

                return await _permissionPolicyService.HandlePermissionRequestAsync(request, invocation).ConfigureAwait(false);
            }
        };

        _logger.LogInformation("Spawning child session {ChildId} for parent {ParentId} agent {AgentId} (model={Model})",
            childId, parentSessionId, agentId, parentModel);

        var spawnStart = DateTime.UtcNow;
        var session = await _clientManager.CreateSessionAsync(
            config,
            BuildChildTaskTools(parentSessionId, agentId, progressContext),
            cancellationToken);
        _logger.LogInformation(
            "Spawned child session {ChildId} for {AgentId} in {Elapsed}ms",
            childId, agentId, (int)(DateTime.UtcNow - spawnStart).TotalMilliseconds);

        // Wire up event relay: every child event is projected onto the parent's SignalR
        // group, annotated with the agent id, so the user sees a unified activity stream.
        var relayHandler = _eventDispatcher.CreateRelayHandler(parentSessionId, agentId);
        var relaySubscription = session.On(relayHandler);

        return new ChildSession(session, relaySubscription, agentId, progressContext);
    }

    private ICollection<AIFunction> BuildChildTaskTools(string parentSessionId, string agentId, DelegationProgressContext progressContext)
    {
        [Description(
            "Report progress for the current delegated task. Call before and after every " +
            "observable step such as creating a folder, initializing git, scaffolding, " +
            "installing dependencies, running a build, committing, or pushing.")]
        async Task<string> ReportTaskProgress(
            [Description("Short user-visible step name, e.g. 'Create project folder' or 'Initialize git repository'.")]
            string step,
            [Description("Step status. Use one of: planned, started, completed, failed, blocked, skipped.")]
            string status,
            [Description("Concise details or command/result. Use an empty string when there are no details.")]
            string details,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(step))
            {
                return "Error: step is required.";
            }

            var normalizedStatus = NormalizeStepStatus(status);
            var phase = normalizedStatus switch
            {
                "completed" or "skipped" => "task-step-complete",
                "failed" or "blocked" => "task-step-error",
                "planned" => "task-step-planned",
                _ => "task-step"
            };
            var isActive = normalizedStatus is "planned" or "started" or "in_progress";
            var message = BuildStepProgressMessage(agentId, step, normalizedStatus, details);

            await _eventDispatcher.DispatchProgressAsync(
                parentSessionId,
                message,
                phase,
                isActive,
                agentId,
                progressContext.ExecutionId,
                step).ConfigureAwait(false);

            return $"Progress reported: {normalizedStatus} - {step}";
        }

        return new[] { AIFunctionFactory.Create(ReportTaskProgress, "report_task_progress") };
    }

    private static string BuildChildSystemMessage(string agentId, string promptContent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Role: {agentId} (specialist agent)");
        sb.AppendLine();
        sb.AppendLine("You are a specialist agent invoked by an orchestrator. Your role description follows.");
        sb.AppendLine();
        sb.AppendLine("## Critical execution rules");
        sb.AppendLine("- You have access to real shell, file, and git tools. **Use them** to do the work.");
        sb.AppendLine("- Do NOT narrate, simulate, or pretend to run commands. Always emit a real tool call.");
        sb.AppendLine("- Do NOT delegate to other agents. The orchestrator handles all routing.");
        sb.AppendLine("- You have a `report_task_progress(step, status, details)` tool. Use it before and after each observable task step.");
        sb.AppendLine("- Do not combine unrelated execution steps into one shell command. Folder creation, git initialization, scaffolding, dependency install, build/test, commit, and push are separate reportable steps.");
        sb.AppendLine("- Complete the task you were given, then return a concise summary of what you did and any files/paths produced.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(promptContent.TrimEnd());
        return sb.ToString();
    }

    private static string NormalizeStepStatus(string status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        return normalized switch
        {
            "complete" or "done" or "success" or "succeeded" => "completed",
            "fail" or "error" => "failed",
            "start" or "running" or "inprogress" or "in_progress" => "started",
            "blocked" => "blocked",
            "skipped" or "skip" => "skipped",
            "planned" or "queued" => "planned",
            _ => "started"
        };
    }

    private static string BuildStepProgressMessage(string agentId, string step, string status, string? details)
    {
        var message = $"{agentId} {status}: {step.Trim()}";
        if (!string.IsNullOrWhiteSpace(details))
        {
            message += $" - {Truncate(details, 220)}";
        }

        return message;
    }

    private static string SummarizeTask(string task)
    {
        var line = task
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .FirstOrDefault(value => value.Length > 0) ?? "delegated task";

        return Truncate(line, 180);
    }

    private static string Truncate(string value, int maxLength)
    {
        var normalized = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maxLength ? normalized : normalized[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static string SanitizeForSessionId(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('-');
            }
        }
        return sb.ToString();
    }

    public async Task AbortChildrenAsync(string parentSessionId, CancellationToken cancellationToken = default)
    {
        if (!_childrenByParent.TryGetValue(parentSessionId, out var children) || children.IsEmpty)
        {
            return;
        }

        foreach (var kvp in children)
        {
            await SafeAbortAsync(kvp.Value.Session);
        }
    }

    public async Task DisposeChildrenAsync(string parentSessionId, CancellationToken cancellationToken = default)
    {
        if (!_childrenByParent.TryRemove(parentSessionId, out var children))
        {
            return;
        }

        foreach (var kvp in children)
        {
            try
            {
                kvp.Value.Subscription.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing relay subscription for child {ChildId}", kvp.Value.Session.SessionId);
            }

            await SafeAbortAsync(kvp.Value.Session);

            try
            {
                await _clientManager.DeleteSessionAsync(kvp.Value.Session.SessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting child session {ChildId}", kvp.Value.Session.SessionId);
            }
        }
    }

    private async Task SafeAbortAsync(CopilotSession session)
    {
        try
        {
            await session.AbortAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ignoring abort error for child session {ChildId}", session.SessionId);
        }
    }

    private sealed class DelegationProgressContext
    {
        private readonly object _lock = new();
        private string? _executionId;

        public string? ExecutionId
        {
            get
            {
                lock (_lock)
                {
                    return _executionId;
                }
            }
        }

        public void Begin(string executionId)
        {
            lock (_lock)
            {
                _executionId = executionId;
            }
        }

        public void Clear(string executionId)
        {
            lock (_lock)
            {
                if (string.Equals(_executionId, executionId, StringComparison.Ordinal))
                {
                    _executionId = null;
                }
            }
        }
    }

    private sealed record ChildSession(
        CopilotSession Session,
        IDisposable Subscription,
        string AgentId,
        DelegationProgressContext ProgressContext)
    {
        public SemaphoreSlim ExecutionLock { get; } = new(1, 1);
    }
}
