namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response model containing session information.
/// </summary>
public class SessionInfoResponse
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Model being used for this session.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Whether streaming is enabled for this session.
    /// </summary>
    public bool Streaming { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session was last active.
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Current status of the session.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Number of messages in the session.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Summary of the session conversation.
    /// </summary>
    public string? Summary { get; set; }
}
