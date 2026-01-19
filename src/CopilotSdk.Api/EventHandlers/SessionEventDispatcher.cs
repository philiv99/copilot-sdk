using CopilotSdk.Api.Hubs;
using CopilotSdk.Api.Models.Domain;
using GitHub.Copilot.SDK;
using Microsoft.AspNetCore.SignalR;

namespace CopilotSdk.Api.EventHandlers;

/// <summary>
/// Dispatches SDK session events to SignalR clients.
/// Maps SDK event types to DTOs and sends them to the appropriate session groups.
/// </summary>
public class SessionEventDispatcher
{
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly ILogger<SessionEventDispatcher> _logger;

    public SessionEventDispatcher(
        IHubContext<SessionHub> hubContext,
        ILogger<SessionEventDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
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
            var eventDto = MapToDto(sessionEvent);
            if (eventDto == null)
            {
                _logger.LogDebug("Skipping unmapped event type: {EventType}", sessionEvent.Type);
                return;
            }

            // Use different methods for delta events vs regular events
            if (IsDeltaEvent(sessionEvent.Type))
            {
                await _hubContext.SendStreamingDeltaAsync(sessionId, eventDto);
                _logger.LogDebug("Sent streaming delta {EventType} to session {SessionId}", sessionEvent.Type, sessionId);
            }
            else
            {
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
