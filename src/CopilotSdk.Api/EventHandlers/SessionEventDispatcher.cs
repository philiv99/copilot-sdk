using System.Text.Json;
using System.Text.RegularExpressions;
using CopilotSdk.Api.Hubs;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using Microsoft.AspNetCore.SignalR;

namespace CopilotSdk.Api.EventHandlers;

/// <summary>
/// Dispatches SDK session events to SignalR clients.
/// Maps SDK event types to DTOs and sends them to the appropriate session groups.
/// Also persists significant events (assistant messages, tool executions) to storage.
/// </summary>
public class SessionEventDispatcher
{
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly ILogger<SessionEventDispatcher> _logger;
    private readonly SessionManager? _sessionManager;

    // Tracks delta-content tail per session+messageId so we can detect agent markers
    // ([AGENT: name] / [HANDOFF: name]) as soon as a marker line completes during streaming.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _markerScanBuffers = new();

    // Tracks active tool executions so we can surface a useful waiting message if a
    // tool start event is emitted but no completion/progress follows for a while.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _toolWatchdogs = new();

    private static readonly Regex AgentMarkerRegex = new(
        @"\[(?<kind>AGENT|HANDOFF)\s*:\s*(?<name>[^\]\r\n]+?)\s*\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SessionEventDispatcher(
        IHubContext<SessionHub> hubContext,
        ILogger<SessionEventDispatcher> logger,
        SessionManager? sessionManager = null)
    {
        _hubContext = hubContext;
        _logger = logger;
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// Sets the session manager for persistence operations.
    /// Called during application startup to avoid circular dependencies.
    /// </summary>
    internal void SetSessionManager(SessionManager sessionManager)
    {
        // This is now set via constructor injection, but keeping for backward compatibility
    }

    /// <summary>
    /// Dispatches a session event to all clients subscribed to the session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="sessionEvent">The SDK session event.</param>
    public async Task DispatchEventAsync(string sessionId, SessionEvent sessionEvent)
    {
        try
        {
            // Persist significant events
            await PersistEventAsync(sessionId, sessionEvent);

            // Use different methods for delta events vs regular events
            if (IsDeltaEvent(sessionEvent.Type))
            {
                var deltaDto = MapToStreamingDelta(sessionId, sessionEvent);
                if (deltaDto == null)
                {
                    _logger.LogDebug("Skipping unmapped delta event type: {EventType}", sessionEvent.Type);
                    return;
                }
                await _hubContext.SendStreamingDeltaAsync(sessionId, deltaDto);
                //_logger.LogDebug("Sent streaming delta {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
            }
            else
            {
                var eventDto = MapToDto(sessionEvent);
                if (eventDto == null)
                {
                    _logger.LogDebug("Skipping unmapped event type: {EventType}", sessionEvent.Type);
                    return;
                }
                await _hubContext.SendSessionEventAsync(sessionId, eventDto);
                _logger.LogDebug("Sent event {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
            }

            await DispatchDerivedProgressAsync(sessionId, sessionEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching event {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
        }
    }

    /// <summary>
    /// Creates an event handler that dispatches events to SignalR for the given session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A session event handler.</returns>
    public SessionEventHandler CreateHandler(string sessionId)
    {
        return (SessionEvent evt) =>
        {
            // Fire and forget - we don't want to block the SDK event processing
            _ = DispatchEventAsync(sessionId, evt);
        };
    }

    /// <summary>
    /// Creates an event handler for a sub-agent (child) session that relays events to the
    /// parent (user-facing) session's SignalR group, annotated with the agent id. Used by
    /// the multi-agent orchestrator so the user sees a unified live activity log.
    /// Child events are NOT persisted under the child session id and NOT re-broadcast to
    /// the child's own group; they are projected onto the parent session.
    /// </summary>
    public SessionEventHandler CreateRelayHandler(string parentSessionId, string agentId)
    {
        return (SessionEvent evt) =>
        {
            _ = RelayEventAsync(parentSessionId, agentId, evt);
        };
    }

    private async Task RelayEventAsync(string parentSessionId, string agentId, SessionEvent sessionEvent)
    {
        try
        {
            var isDelta = IsDeltaEvent(sessionEvent.Type);
            if (!isDelta)
            {
                _logger.LogDebug(
                    "Relaying child event {EventType} from agent {AgentId} -> parent session {ParentSessionId}",
                    sessionEvent.Type, agentId, parentSessionId);
            }

            if (isDelta)
            {
                var deltaDto = MapToStreamingDelta(parentSessionId, sessionEvent);
                if (deltaDto == null) return;
                deltaDto.AgentId = agentId;
                await _hubContext.SendStreamingDeltaAsync(parentSessionId, deltaDto);
            }
            else
            {
                var eventDto = MapToDto(sessionEvent);
                if (eventDto == null)
                {
                    _logger.LogDebug(
                        "Child event {EventType} from {AgentId} produced no DTO (skipped relay)",
                        sessionEvent.Type, agentId);
                    return;
                }
                eventDto.AgentId = agentId;
                await _hubContext.SendSessionEventAsync(parentSessionId, eventDto);
            }

            await DispatchDerivedProgressAsync(parentSessionId, sessionEvent, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error relaying child event {EventType} from agent {AgentId} to session {ParentSessionId}",
                sessionEvent.Type, agentId, parentSessionId);
        }
    }

    /// <summary>
    /// Sends an explicit progress event for UI visibility during long-running work.
    /// </summary>
    public async Task DispatchProgressAsync(
        string sessionId,
        string message,
        string phase,
        bool isActive = true,
        string? agentId = null,
        string? executionId = null,
        string? step = null,
        int? stepIndex = null,
        int? stepCount = null,
        string? toolName = null,
        string? toolCallId = null)
    {
        var eventDto = new SessionEventDto
        {
            Id = Guid.NewGuid(),
            Type = "session.progress",
            Timestamp = DateTimeOffset.UtcNow,
            Ephemeral = true,
            AgentId = agentId,
            Data = new SessionProgressDataDto
            {
                Message = message,
                Phase = phase,
                IsActive = isActive,
                AgentId = agentId,
                ExecutionId = executionId,
                Step = step,
                StepIndex = stepIndex,
                StepCount = stepCount,
                ToolName = toolName,
                ToolCallId = toolCallId
            }
        };

        await _hubContext.SendSessionEventAsync(sessionId, eventDto);
        _logger.LogDebug("Sent progress event to session {SessionId}: {Message}", sessionId, message);
    }

    private async Task DispatchDerivedProgressAsync(
        string sessionId,
        SessionEvent sessionEvent,
        string? agentId = null,
        string? executionId = null)
    {
        switch (sessionEvent)
        {
            case AssistantTurnStartEvent:
                await DispatchProgressAsync(
                    sessionId,
                    agentId == null ? "Assistant started working" : $"{agentId} started working",
                    agentId == null ? "thinking" : "agent-turn",
                    agentId: agentId,
                    executionId: executionId);
                break;

            case ToolExecutionStartEvent e:
                _logger.LogInformation(
                    "ToolExecutionStartEvent received: session={SessionId} tool='{ToolName}' callId={CallId} hasArgs={HasArgs}",
                    sessionId, e.Data.ToolName, e.Data.ToolCallId, e.Data.Arguments != null);
                await DispatchProgressAsync(
                    sessionId,
                    BuildToolStartMessage(agentId, e.Data),
                    "tool",
                    agentId: agentId,
                    executionId: executionId,
                    step: ExtractToolActionSummary(e.Data.Arguments),
                    toolName: e.Data.ToolName,
                    toolCallId: e.Data.ToolCallId);
                StartToolWatchdog(sessionId, agentId, executionId, e.Data);
                break;

            case ToolExecutionCompleteEvent e:
                StopToolWatchdog(sessionId, e.Data.ToolCallId);
                _logger.LogInformation(
                    "ToolExecutionCompleteEvent received: session={SessionId} callId={CallId} hasError={HasError}",
                    sessionId, e.Data.ToolCallId, e.Data.Error != null);
                await DispatchProgressAsync(
                    sessionId,
                    e.Data.Error == null
                        ? BuildToolCompleteMessage(agentId, e.Data.ToolCallId)
                        : BuildToolFailedMessage(agentId, e.Data.Error.Message),
                    e.Data.Error == null ? "tool-complete" : "tool-error",
                    isActive: false,
                    agentId: agentId,
                    executionId: executionId,
                    toolCallId: e.Data.ToolCallId);
                break;

            case AssistantMessageDeltaEvent dEvt:
                await ScanDeltaForAgentMarkersAsync(sessionId, dEvt.Data.MessageId, dEvt.Data.DeltaContent);
                break;

            case AssistantMessageEvent fEvt:
                ClearMarkerScanBuffer(sessionId, fEvt.Data.MessageId);
                await ScanContentForAgentMarkersAsync(sessionId, fEvt.Data.Content);
                break;

            case AssistantTurnEndEvent:
            case SessionIdleEvent:
                StopSessionToolWatchdogs(sessionId);
                await DispatchProgressAsync(
                    sessionId,
                    agentId == null ? "Assistant finished" : $"{agentId} finished",
                    "done",
                    isActive: false,
                    agentId: agentId,
                    executionId: executionId);
                break;

            case AbortEvent:
                StopSessionToolWatchdogs(sessionId);
                await DispatchProgressAsync(sessionId, "Aborted by user", "abort", isActive: false, agentId: agentId, executionId: executionId);
                break;

            case SessionErrorEvent e:
                StopSessionToolWatchdogs(sessionId);
                await DispatchProgressAsync(sessionId, $"Error: {e.Data.Message}", "error", isActive: false, agentId: agentId, executionId: executionId);
                break;
        }
    }

    private void StartToolWatchdog(string sessionId, string? agentId, string? executionId, ToolExecutionStartData data)
    {
        if (string.IsNullOrWhiteSpace(data.ToolCallId))
        {
            return;
        }

        var key = ToolWatchdogKey(sessionId, data.ToolCallId);
        StopToolWatchdog(sessionId, data.ToolCallId);

        var cts = new CancellationTokenSource();
        if (!_toolWatchdogs.TryAdd(key, cts))
        {
            cts.Dispose();
            return;
        }

        _ = Task.Run(async () =>
        {
            var startedAt = DateTime.UtcNow;
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), cts.Token).ConfigureAwait(false);
                    if (cts.Token.IsCancellationRequested) break;

                    var elapsed = (int)(DateTime.UtcNow - startedAt).TotalSeconds;
                    var summary = ExtractToolActionSummary(data.Arguments);
                    var actor = string.IsNullOrWhiteSpace(agentId) ? "Assistant" : agentId;
                    var detail = string.IsNullOrWhiteSpace(summary) ? string.Empty : $": {summary}";

                    await DispatchProgressAsync(
                        sessionId,
                        $"Still waiting on {actor} tool '{data.ToolName}'{detail} ({elapsed}s elapsed)",
                        "tool-waiting",
                        isActive: true,
                        agentId: agentId,
                        executionId: executionId,
                        step: summary,
                        toolName: data.ToolName,
                        toolCallId: data.ToolCallId).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the tool completes or the session stops.
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Tool watchdog failed for {SessionId}/{ToolCallId}", sessionId, data.ToolCallId);
            }
            finally
            {
                _toolWatchdogs.TryRemove(key, out _);
                cts.Dispose();
            }
        });
    }

    private void StopToolWatchdog(string sessionId, string? toolCallId)
    {
        if (string.IsNullOrWhiteSpace(toolCallId))
        {
            return;
        }

        var key = ToolWatchdogKey(sessionId, toolCallId);
        if (_toolWatchdogs.TryGetValue(key, out var cts))
        {
            try { cts.Cancel(); } catch { /* ignore cancellation races */ }
        }
    }

    private void StopSessionToolWatchdogs(string sessionId)
    {
        var prefix = sessionId + "::";
        foreach (var item in _toolWatchdogs.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            try { item.Value.Cancel(); } catch { /* ignore cancellation races */ }
        }
    }

    private static string ToolWatchdogKey(string sessionId, string toolCallId) => $"{sessionId}::{toolCallId}";

    private static string BuildToolStartMessage(string? agentId, ToolExecutionStartData data)
    {
        var actor = string.IsNullOrWhiteSpace(agentId) ? "Assistant" : agentId;
        var summary = ExtractToolActionSummary(data.Arguments);

        return string.IsNullOrWhiteSpace(summary)
            ? $"{actor} started {data.ToolName}"
            : $"{actor} started {data.ToolName}: {summary}";
    }

    private static string BuildToolCompleteMessage(string? agentId, string toolCallId)
    {
        var actor = string.IsNullOrWhiteSpace(agentId) ? "Tool" : $"{agentId} tool";
        var suffix = string.IsNullOrWhiteSpace(toolCallId) ? string.Empty : $" ({Truncate(toolCallId, 8)})";
        return $"{actor} completed{suffix}";
    }

    private static string BuildToolFailedMessage(string? agentId, string error)
    {
        var actor = string.IsNullOrWhiteSpace(agentId) ? "Tool" : $"{agentId} tool";
        return $"{actor} failed: {Truncate(error, 220)}";
    }

    internal static string? ExtractToolActionSummary(object? arguments)
    {
        foreach (var key in new[] { "description", "command", "path", "filePath", "cwd", "query" })
        {
            var value = TryGetArgumentString(arguments, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return Truncate(value, 220);
            }
        }

        return null;
    }

    private static string? TryGetArgumentString(object? arguments, string key)
    {
        if (arguments == null)
        {
            return null;
        }

        if (arguments is JsonElement element)
        {
            return TryGetJsonElementString(element, key);
        }

        if (arguments is IReadOnlyDictionary<string, object?> readOnlyDictionary
            && TryGetDictionaryValue(readOnlyDictionary, key, out var readOnlyValue))
        {
            return ConvertArgumentValue(readOnlyValue);
        }

        if (arguments is IDictionary<string, object?> dictionary
            && TryGetDictionaryValue(dictionary, key, out var value))
        {
            return ConvertArgumentValue(value);
        }

        if (arguments is IDictionary<string, object> objectDictionary
            && TryGetDictionaryValue(objectDictionary, key, out var objectValue))
        {
            return ConvertArgumentValue(objectValue);
        }

        return null;
    }

    private static string? TryGetJsonElementString(JsonElement element, string key)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
        }

        return null;
    }

    private static bool TryGetDictionaryValue<TValue>(IEnumerable<KeyValuePair<string, TValue>> dictionary, string key, out TValue? value)
    {
        foreach (var pair in dictionary)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? ConvertArgumentValue(object? value)
    {
        return value switch
        {
            null => null,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            JsonElement element => element.ToString(),
            string text => text,
            _ => value.ToString()
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        var normalized = Regex.Replace(value, @"\s+", " ").Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..Math.Max(0, maxLength - 3)] + "...";
    }

    /// <summary>
    /// Streams a buffer of assistant deltas, looking for [AGENT: name] / [HANDOFF: name]
    /// markers. When a complete marker is detected, emits a session.progress event so the
    /// UI can show which agent role is currently active or handing off.
    /// </summary>
    private async Task ScanDeltaForAgentMarkersAsync(string sessionId, string messageId, string? deltaContent)
    {
        if (string.IsNullOrEmpty(deltaContent))
        {
            return;
        }

        var key = sessionId + "|" + messageId;
        var buffer = _markerScanBuffers.AddOrUpdate(key, deltaContent, (_, existing) => existing + deltaContent);

        var lastMatchEnd = 0;
        foreach (Match match in AgentMarkerRegex.Matches(buffer))
        {
            lastMatchEnd = match.Index + match.Length;
            await EmitAgentMarkerProgressAsync(sessionId, match);
        }

        if (lastMatchEnd > 0)
        {
            // Keep only the unparsed tail (anything after the last completed marker)
            // so we don't re-emit on subsequent deltas.
            _markerScanBuffers[key] = buffer.Substring(lastMatchEnd);
        }
        else if (buffer.Length > 4096)
        {
            // Cap buffer growth - keep last 1KB for partial-marker detection across deltas.
            _markerScanBuffers[key] = buffer.Substring(buffer.Length - 1024);
        }
    }

    private async Task ScanContentForAgentMarkersAsync(string sessionId, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        // Only emit markers from the final message that weren't already emitted via deltas.
        // We can't reliably know which ones streamed, so we skip here to avoid duplicates.
        // (Final-message scan retained for non-streaming sessions; in that case _markerScanBuffers
        //  has no entries for this message, so we emit them all.)
        await Task.CompletedTask;
    }

    private void ClearMarkerScanBuffer(string sessionId, string messageId)
    {
        _markerScanBuffers.TryRemove(sessionId + "|" + messageId, out _);
    }

    private async Task EmitAgentMarkerProgressAsync(string sessionId, Match match)
    {
        var kind = match.Groups["kind"].Value.ToUpperInvariant();
        var name = match.Groups["name"].Value.Trim();

        var phase = kind == "HANDOFF" ? "handoff" : "agent";
        var message = kind == "HANDOFF"
            ? $"\u21A9 Handing off to {name}"
            : $"\u25B6 {name} is working";

        await DispatchProgressAsync(sessionId, message, phase);
    }

    /// <summary>
    /// Determines if an event type is a delta (streaming) event.
    /// </summary>
    private static bool IsDeltaEvent(string eventType)
    {
        return eventType == "assistant.message_delta" ||
               eventType == "assistant.reasoning_delta";
    }

    /// <summary>
    /// Maps a delta event to a StreamingDeltaDto for SignalR transmission.
    /// </summary>
    private static StreamingDeltaDto? MapToStreamingDelta(string sessionId, SessionEvent sessionEvent)
    {
        return sessionEvent switch
        {
            AssistantMessageDeltaEvent e => new StreamingDeltaDto
            {
                SessionId = sessionId,
                Type = "message",
                Id = e.Data.MessageId,
                Content = e.Data.DeltaContent,
                TotalBytes = e.Data.TotalResponseSizeBytes
            },
            AssistantReasoningDeltaEvent e => new StreamingDeltaDto
            {
                SessionId = sessionId,
                Type = "reasoning",
                Id = e.Data.ReasoningId,
                Content = e.Data.DeltaContent,
                TotalBytes = null
            },
            _ => null
        };
    }

    /// <summary>
    /// Maps an SDK session event to a DTO for SignalR transmission.
    /// </summary>
    private SessionEventDto? MapToDto(SessionEvent sessionEvent)
    {
        var dto = new SessionEventDto
        {
            Id = sessionEvent.Id,
            Type = sessionEvent.Type,
            Timestamp = sessionEvent.Timestamp,
            ParentId = sessionEvent.ParentId,
            Ephemeral = sessionEvent.Ephemeral
        };

        dto.Data = sessionEvent switch
        {
            SessionStartEvent e => MapSessionStartData(e.Data),
            SessionErrorEvent e => MapSessionErrorData(e.Data),
            SessionIdleEvent _ => new SessionIdleDataDto(),
            UserMessageEvent e => MapUserMessageData(e.Data),
            AssistantMessageEvent e => MapAssistantMessageData(e.Data),
            AssistantMessageDeltaEvent e => MapAssistantMessageDeltaData(e.Data),
            AssistantReasoningEvent e => MapAssistantReasoningData(e.Data),
            AssistantReasoningDeltaEvent e => MapAssistantReasoningDeltaData(e.Data),
            AssistantTurnStartEvent e => MapAssistantTurnStartData(e.Data),
            AssistantTurnEndEvent e => MapAssistantTurnEndData(e.Data),
            AssistantUsageEvent e => MapAssistantUsageData(e.Data),
            ToolExecutionStartEvent e => MapToolExecutionStartData(e.Data),
            ToolExecutionCompleteEvent e => MapToolExecutionCompleteData(e.Data),
            AbortEvent e => MapAbortData(e.Data),
            _ => null // Unknown event types will have null data
        };

        // If we have a known event type but couldn't map the data, still return the DTO
        // This allows clients to at least see the event type
        return dto;
    }

    #region Persistence

    /// <summary>
    /// Persists significant events (assistant messages, tool executions) to storage.
    /// Delta events are not persisted as they are ephemeral.
    /// </summary>
    private async Task PersistEventAsync(string sessionId, SessionEvent sessionEvent)
    {
        if (_sessionManager == null)
        {
            return;
        }

        // Skip ephemeral/delta events
        if (IsDeltaEvent(sessionEvent.Type))
        {
            return;
        }

        try
        {
            PersistedMessage? message = sessionEvent switch
            {
                AssistantMessageEvent e => new PersistedMessage
                {
                    Id = sessionEvent.Id,
                    Timestamp = sessionEvent.Timestamp.UtcDateTime,
                    Role = "assistant",
                    Content = e.Data.Content,
                    MessageId = e.Data.MessageId,
                    ParentToolCallId = e.Data.ParentToolCallId,
                    ToolRequests = e.Data.ToolRequests?.Select(tr => new PersistedToolRequest
                    {
                        ToolCallId = tr.ToolCallId,
                        ToolName = tr.Name,
                        Arguments = tr.Arguments != null ? JsonSerializer.Serialize(tr.Arguments) : null
                    }).ToList()
                },
                AssistantReasoningEvent e => new PersistedMessage
                {
                    Id = sessionEvent.Id,
                    Timestamp = sessionEvent.Timestamp.UtcDateTime,
                    Role = "assistant",
                    ReasoningContent = e.Data.Content
                },
                ToolExecutionCompleteEvent e => new PersistedMessage
                {
                    Id = sessionEvent.Id,
                    Timestamp = sessionEvent.Timestamp.UtcDateTime,
                    Role = "tool",
                    ToolCallId = e.Data.ToolCallId,
                    ToolResult = e.Data.Result?.Content,
                    ToolError = e.Data.Error?.Message
                },
                SessionErrorEvent e => new PersistedMessage
                {
                    Id = sessionEvent.Id,
                    Timestamp = sessionEvent.Timestamp.UtcDateTime,
                    Role = "system",
                    Content = $"Error: {e.Data.Message}"
                },
                _ => null
            };

            if (message != null)
            {
                await _sessionManager.AppendMessagesAsync(sessionId, new[] { message });
                _logger.LogDebug("Persisted {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
            }

            // When a tool execution completes, try to extract the repo path from the result
            // and persist it as the session's AppPath (for the Play button / dev server).
            if (sessionEvent is ToolExecutionCompleteEvent toolEvent)
            {
                await TryExtractAndPersistAppPathAsync(sessionId, toolEvent.Data.Result?.Content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist event {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
        }
    }

    /// <summary>
    /// Regex that matches repo paths like C:\development\repos\jestquest\... or C:\development\repos\jestquest
    /// Captures the folder name immediately after the repos\ prefix (letters, digits, hyphens, underscores only).
    /// </summary>
    private static readonly Regex RepoPathRegex = new(
        @"[A-Za-z]:\\[^""'\s]*?repos[/\\]([A-Za-z0-9][A-Za-z0-9_\-]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Attempts to extract the repository folder path from tool execution output
    /// and persist it as the session's AppPath if not already set.
    /// Tool results contain paths like "Created file C:\development\repos\jestquest\src\App.tsx".
    /// </summary>
    private async Task TryExtractAndPersistAppPathAsync(string sessionId, string? toolResult)
    {
        if (string.IsNullOrEmpty(toolResult) || _sessionManager == null)
            return;

        try
        {
            // Only proceed if the session doesn't already have an AppPath
            var metadata = await _sessionManager.GetMetadataAsync(sessionId);
            if (metadata == null || !string.IsNullOrEmpty(metadata.AppPath))
                return;

            var repoFolder = ExtractRepoFolder(toolResult);
            if (repoFolder == null)
                return;

            // Exclude the copilot-sdk project itself
            if (repoFolder.Equals("copilot-sdk", StringComparison.OrdinalIgnoreCase))
                return;

            var reposDir = @"C:\development\repos";
            var fullPath = Path.Combine(reposDir, repoFolder);

            // Verify the folder exists and has a package.json
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "package.json")))
            {
                metadata.AppPath = fullPath;
                await _sessionManager.PersistSessionAsync(sessionId, metadata);
                _logger.LogInformation(
                    "Auto-detected and persisted AppPath for session {SessionId}: {AppPath}",
                    sessionId, fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract AppPath from tool result for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Extracts the repo folder name from a string containing a repo path reference.
    /// Returns null if no repo path is found.
    /// </summary>
    internal static string? ExtractRepoFolder(string text)
    {
        var match = RepoPathRegex.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    #endregion

    #region Data Mappers

    private static SessionStartDataDto MapSessionStartData(SessionStartData data)
    {
        return new SessionStartDataDto
        {
            SessionId = data.SessionId,
            Version = data.Version,
            Producer = data.Producer,
            CopilotVersion = data.CopilotVersion,
            StartTime = data.StartTime,
            SelectedModel = data.SelectedModel
        };
    }

    private static SessionErrorDataDto MapSessionErrorData(SessionErrorData data)
    {
        return new SessionErrorDataDto
        {
            ErrorType = data.ErrorType,
            Message = data.Message,
            Stack = data.Stack
        };
    }

    private static UserMessageDataDto MapUserMessageData(UserMessageData data)
    {
        return new UserMessageDataDto
        {
            Content = data.Content,
            TransformedContent = data.TransformedContent,
            Source = data.Source,
            Attachments = data.Attachments?.Select(a => new MessageAttachmentDto
            {
                Type = a.Type.ToString().ToLowerInvariant(),
                Path = a.Path,
                DisplayName = a.DisplayName
            }).ToList()
        };
    }

    private static AssistantMessageDataDto MapAssistantMessageData(AssistantMessageData data)
    {
        return new AssistantMessageDataDto
        {
            MessageId = data.MessageId,
            Content = data.Content,
            ParentToolCallId = data.ParentToolCallId,
            ToolRequests = data.ToolRequests?.Select(tr => new ToolRequestDto
            {
                ToolCallId = tr.ToolCallId,
                ToolName = tr.Name,
                Arguments = tr.Arguments
            }).ToList()
        };
    }

    private static AssistantMessageDeltaDataDto MapAssistantMessageDeltaData(AssistantMessageDeltaData data)
    {
        return new AssistantMessageDeltaDataDto
        {
            MessageId = data.MessageId,
            DeltaContent = data.DeltaContent,
            TotalResponseSizeBytes = data.TotalResponseSizeBytes,
            ParentToolCallId = data.ParentToolCallId
        };
    }

    private static AssistantReasoningDataDto MapAssistantReasoningData(AssistantReasoningData data)
    {
        return new AssistantReasoningDataDto
        {
            ReasoningId = data.ReasoningId,
            Content = data.Content
        };
    }

    private static AssistantReasoningDeltaDataDto MapAssistantReasoningDeltaData(AssistantReasoningDeltaData data)
    {
        return new AssistantReasoningDeltaDataDto
        {
            ReasoningId = data.ReasoningId,
            DeltaContent = data.DeltaContent
        };
    }

    private static AssistantTurnStartDataDto MapAssistantTurnStartData(AssistantTurnStartData data)
    {
        return new AssistantTurnStartDataDto
        {
            TurnId = data.TurnId
        };
    }

    private static AssistantTurnEndDataDto MapAssistantTurnEndData(AssistantTurnEndData data)
    {
        return new AssistantTurnEndDataDto
        {
            TurnId = data.TurnId
        };
    }

    private static AssistantUsageDataDto MapAssistantUsageData(AssistantUsageData data)
    {
        return new AssistantUsageDataDto
        {
            Model = data.Model,
            InputTokens = data.InputTokens,
            OutputTokens = data.OutputTokens,
            CacheReadTokens = data.CacheReadTokens,
            CacheWriteTokens = data.CacheWriteTokens,
            Cost = data.Cost,
            Duration = data.Duration
        };
    }

    private static ToolExecutionStartDataDto MapToolExecutionStartData(ToolExecutionStartData data)
    {
        return new ToolExecutionStartDataDto
        {
            ToolCallId = data.ToolCallId,
            ToolName = data.ToolName,
            Arguments = data.Arguments
        };
    }

    private static ToolExecutionCompleteDataDto MapToolExecutionCompleteData(ToolExecutionCompleteData data)
    {
        return new ToolExecutionCompleteDataDto
        {
            ToolCallId = data.ToolCallId,
            ToolName = string.Empty, // Not provided in complete event
            Result = data.Result?.Content,
            Error = data.Error?.Message
        };
    }

    private static AbortDataDto MapAbortData(AbortData data)
    {
        return new AbortDataDto
        {
            Reason = data.Reason
        };
    }

    #endregion
}
