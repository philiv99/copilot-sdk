using System.Text.Json.Serialization;

namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Represents a session event that occurred during a conversation.
/// </summary>
public class SessionEventDto
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of the event (e.g., "user.message", "assistant.message", "session.idle").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// When this event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Parent event ID if this event is related to another event.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Whether this event is ephemeral (not persisted).
    /// </summary>
    public bool? Ephemeral { get; set; }

    /// <summary>
    /// Event-specific data payload.
    /// </summary>
    public object? Data { get; set; }
}

#region User Message Data

/// <summary>
/// Data for user.message events.
/// </summary>
public class UserMessageDataDto
{
    /// <summary>
    /// The content of the user's message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Transformed content after processing.
    /// </summary>
    public string? TransformedContent { get; set; }

    /// <summary>
    /// Attachments included with the message.
    /// </summary>
    public List<MessageAttachmentDto>? Attachments { get; set; }

    /// <summary>
    /// Source of the message.
    /// </summary>
    public string? Source { get; set; }
}

#endregion

#region Assistant Message Data

/// <summary>
/// Data for assistant.message events.
/// </summary>
public class AssistantMessageDataDto
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The content of the assistant's message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Tool requests made by the assistant.
    /// </summary>
    public List<ToolRequestDto>? ToolRequests { get; set; }

    /// <summary>
    /// Parent tool call ID if this message is part of a tool execution.
    /// </summary>
    public string? ParentToolCallId { get; set; }
}

/// <summary>
/// Data for assistant.message_delta events (streaming).
/// </summary>
public class AssistantMessageDeltaDataDto
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The delta content being streamed.
    /// </summary>
    public string DeltaContent { get; set; } = string.Empty;

    /// <summary>
    /// Total response size in bytes so far.
    /// </summary>
    public double? TotalResponseSizeBytes { get; set; }

    /// <summary>
    /// Parent tool call ID if this message is part of a tool execution.
    /// </summary>
    public string? ParentToolCallId { get; set; }
}

/// <summary>
/// Represents a tool request from the assistant.
/// </summary>
public class ToolRequestDto
{
    /// <summary>
    /// Unique ID for this tool call.
    /// </summary>
    public string ToolCallId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the tool being requested.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the tool.
    /// </summary>
    public object? Arguments { get; set; }
}

#endregion

#region Assistant Reasoning Data

/// <summary>
/// Data for assistant.reasoning events.
/// </summary>
public class AssistantReasoningDataDto
{
    /// <summary>
    /// Unique identifier for this reasoning block.
    /// </summary>
    public string ReasoningId { get; set; } = string.Empty;

    /// <summary>
    /// The reasoning content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Data for assistant.reasoning_delta events (streaming).
/// </summary>
public class AssistantReasoningDeltaDataDto
{
    /// <summary>
    /// Unique identifier for this reasoning block.
    /// </summary>
    public string ReasoningId { get; set; } = string.Empty;

    /// <summary>
    /// The delta content being streamed.
    /// </summary>
    public string DeltaContent { get; set; } = string.Empty;
}

#endregion

#region Tool Execution Data

/// <summary>
/// Data for tool.execution_start events.
/// </summary>
public class ToolExecutionStartDataDto
{
    /// <summary>
    /// Unique ID for this tool call.
    /// </summary>
    public string ToolCallId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the tool being executed.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Arguments passed to the tool.
    /// </summary>
    public object? Arguments { get; set; }

    /// <summary>
    /// Display name of the tool.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Data for tool.execution_complete events.
/// </summary>
public class ToolExecutionCompleteDataDto
{
    /// <summary>
    /// Unique ID for this tool call.
    /// </summary>
    public string ToolCallId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the tool that was executed.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// The result of the tool execution.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error message if the tool execution failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Duration of the tool execution in milliseconds.
    /// </summary>
    public double? Duration { get; set; }
}

#endregion

#region Session Events Data

/// <summary>
/// Data for session.start events.
/// </summary>
public class SessionStartDataDto
{
    /// <summary>
    /// The session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Session version.
    /// </summary>
    public double Version { get; set; }

    /// <summary>
    /// Producer of the session.
    /// </summary>
    public string Producer { get; set; } = string.Empty;

    /// <summary>
    /// Copilot version.
    /// </summary>
    public string CopilotVersion { get; set; } = string.Empty;

    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// The selected model for the session.
    /// </summary>
    public string? SelectedModel { get; set; }
}

/// <summary>
/// Data for session.error events.
/// </summary>
public class SessionErrorDataDto
{
    /// <summary>
    /// Type of error.
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Stack trace if available.
    /// </summary>
    public string? Stack { get; set; }
}

/// <summary>
/// Data for session.idle events.
/// </summary>
public class SessionIdleDataDto
{
    // Empty - session.idle events have no data payload
}

/// <summary>
/// Data for assistant.turn_start events.
/// </summary>
public class AssistantTurnStartDataDto
{
    /// <summary>
    /// Unique identifier for this turn.
    /// </summary>
    public string TurnId { get; set; } = string.Empty;
}

/// <summary>
/// Data for assistant.turn_end events.
/// </summary>
public class AssistantTurnEndDataDto
{
    /// <summary>
    /// Unique identifier for this turn.
    /// </summary>
    public string TurnId { get; set; } = string.Empty;
}

/// <summary>
/// Data for assistant.usage events.
/// </summary>
public class AssistantUsageDataDto
{
    /// <summary>
    /// Model used.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Number of input tokens.
    /// </summary>
    public double? InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens.
    /// </summary>
    public double? OutputTokens { get; set; }

    /// <summary>
    /// Number of cache read tokens.
    /// </summary>
    public double? CacheReadTokens { get; set; }

    /// <summary>
    /// Number of cache write tokens.
    /// </summary>
    public double? CacheWriteTokens { get; set; }

    /// <summary>
    /// Cost of the API call.
    /// </summary>
    public double? Cost { get; set; }

    /// <summary>
    /// Duration of the API call.
    /// </summary>
    public double? Duration { get; set; }
}

#endregion

/// <summary>
/// Data for abort events.
/// </summary>
public class AbortDataDto
{
    /// <summary>
    /// Reason for the abort.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Source of the abort request.
    /// </summary>
    public string? Source { get; set; }
}
