using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request model for resuming an existing session.
/// </summary>
public class ResumeSessionRequest
{
    /// <summary>
    /// Whether to enable streaming responses for the resumed session.
    /// </summary>
    public bool Streaming { get; set; } = false;

    /// <summary>
    /// Custom provider configuration for BYOK scenarios.
    /// </summary>
    public ProviderConfig? Provider { get; set; }

    /// <summary>
    /// Custom tool definitions for the resumed session.
    /// </summary>
    public List<ToolDefinition>? Tools { get; set; }
}
