namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Information about an active session.
/// </summary>
public class SessionInfo
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
    /// Current status of the session (e.g., "Active", "Idle", "Error").
    /// </summary>
    public string Status { get; set; } = "Active";
}
