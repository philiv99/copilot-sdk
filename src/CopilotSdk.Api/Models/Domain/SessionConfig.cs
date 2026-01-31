namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Configuration for creating a new session.
/// </summary>
public class SessionConfig
{
    /// <summary>
    /// Optional custom session ID. If not provided, one will be generated.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Model to use for the session (e.g., "gpt-4", "gpt-3.5-turbo").
    /// </summary>
    public string Model { get; set; } = "claude-opus-4.5";

    /// <summary>
    /// Whether to enable streaming responses.
    /// </summary>
    public bool Streaming { get; set; } = false;

    /// <summary>
    /// System message configuration.
    /// </summary>
    public SystemMessageConfig? SystemMessage { get; set; }

    /// <summary>
    /// List of tool names that are available for this session.
    /// If null, all tools are available.
    /// </summary>
    public List<string>? AvailableTools { get; set; }

    /// <summary>
    /// List of tool names to exclude from this session.
    /// </summary>
    public List<string>? ExcludedTools { get; set; }

    /// <summary>
    /// Custom provider configuration for BYOK scenarios.
    /// </summary>
    public ProviderConfig? Provider { get; set; }

    /// <summary>
    /// Custom tool definitions for this session.
    /// </summary>
    public List<ToolDefinition>? Tools { get; set; }
}
