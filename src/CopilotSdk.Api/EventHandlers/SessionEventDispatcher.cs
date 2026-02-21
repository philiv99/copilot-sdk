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
                _logger.LogDebug("Sent streaming delta {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
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
