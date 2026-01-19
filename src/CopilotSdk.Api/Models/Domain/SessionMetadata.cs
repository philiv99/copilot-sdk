namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Metadata about a session including activity tracking.
/// </summary>
public class SessionMetadata
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

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
    /// The configuration used to create this session.
    /// </summary>
    public SessionConfig? Config { get; set; }
}
