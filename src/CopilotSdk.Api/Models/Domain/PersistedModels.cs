using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Represents a persisted session including all configuration, metadata, and messages.
/// </summary>
public class PersistedSessionData
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session was last active.
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Number of messages in the session.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Summary or title of the session conversation.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Whether this is a remote session.
    /// </summary>
    public bool IsRemote { get; set; }

    /// <summary>
    /// The user ID of the creator who owns this session.
    /// </summary>
    public string? CreatorUserId { get; set; }

    /// <summary>
    /// The full session configuration.
    /// </summary>
    public PersistedSessionConfig? Config { get; set; }

    /// <summary>
    /// All messages in the session conversation.
    /// </summary>
    public List<PersistedMessage> Messages { get; set; } = new();
}

/// <summary>
/// Persisted session configuration with all settings.
/// </summary>
public class PersistedSessionConfig
{
    /// <summary>
    /// Model being used for this session.
    /// </summary>
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Whether streaming is enabled for this session.
    /// </summary>
    public bool Streaming { get; set; }

    /// <summary>
    /// System message configuration.
    /// </summary>
    public PersistedSystemMessageConfig? SystemMessage { get; set; }

    /// <summary>
    /// List of tool names that are available for this session.
    /// </summary>
    public List<string>? AvailableTools { get; set; }

    /// <summary>
    /// List of tool names to exclude from this session.
    /// </summary>
    public List<string>? ExcludedTools { get; set; }

    /// <summary>
    /// Custom provider configuration for BYOK scenarios.
    /// </summary>
    public PersistedProviderConfig? Provider { get; set; }

    /// <summary>
    /// Custom tool definitions for this session.
    /// </summary>
    public List<PersistedToolDefinition>? Tools { get; set; }
}

/// <summary>
/// Persisted system message configuration.
/// </summary>
public class PersistedSystemMessageConfig
{
    /// <summary>
    /// Mode for system message handling ("append" or "replace").
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// The system message content.
    /// </summary>
    public string? Content { get; set; }
}

/// <summary>
/// Persisted provider configuration (BYOK).
/// </summary>
public class PersistedProviderConfig
{
    /// <summary>
    /// Provider type identifier.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Base URL for the provider.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// API key for authentication (stored securely).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Bearer token for authentication.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Wire API format.
    /// </summary>
    public string? WireApi { get; set; }
}

/// <summary>
/// Persisted tool definition.
/// </summary>
public class PersistedToolDefinition
{
    /// <summary>
    /// Unique name of the tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of parameters the tool accepts.
    /// </summary>
    public List<PersistedToolParameter>? Parameters { get; set; }
}

/// <summary>
/// Persisted tool parameter.
/// </summary>
public class PersistedToolParameter
{
    /// <summary>
    /// Parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parameter type (e.g., "string", "number", "boolean").
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Whether this parameter is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Default value for the parameter.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Allowed values (for enum-like parameters).
    /// </summary>
    public List<string>? AllowedValues { get; set; }
}

/// <summary>
/// Represents a persisted message in a session conversation.
/// </summary>
public class PersistedMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When this message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Role of the message sender ("user", "assistant", "system", "tool").
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// The content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Transformed content (for user messages after processing).
    /// </summary>
    public string? TransformedContent { get; set; }

    /// <summary>
    /// Message ID from the assistant (for assistant messages).
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Attachments included with the message.
    /// </summary>
    public List<PersistedAttachment>? Attachments { get; set; }

    /// <summary>
    /// Tool requests made by the assistant.
    /// </summary>
    public List<PersistedToolRequest>? ToolRequests { get; set; }

    /// <summary>
    /// Tool call ID if this message is a tool response.
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Tool name if this message is a tool response.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Tool execution result if this message is a tool response.
    /// </summary>
    public string? ToolResult { get; set; }

    /// <summary>
    /// Error message if tool execution failed.
    /// </summary>
    public string? ToolError { get; set; }

    /// <summary>
    /// Reasoning content (for reasoning events).
    /// </summary>
    public string? ReasoningContent { get; set; }

    /// <summary>
    /// Parent tool call ID if this message is part of a tool execution.
    /// </summary>
    public string? ParentToolCallId { get; set; }

    /// <summary>
    /// Source of the message (e.g., "direct", "copilot").
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Represents a persisted file attachment.
/// </summary>
public class PersistedAttachment
{
    /// <summary>
    /// Type of attachment ("file" or "directory").
    /// </summary>
    public string Type { get; set; } = "file";

    /// <summary>
    /// Path to the attachment.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the attachment.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Represents a persisted tool request from the assistant.
/// </summary>
public class PersistedToolRequest
{
    /// <summary>
    /// Tool call identifier.
    /// </summary>
    public string ToolCallId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the tool being called.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Arguments passed to the tool as JSON.
    /// </summary>
    public string? Arguments { get; set; }
}
