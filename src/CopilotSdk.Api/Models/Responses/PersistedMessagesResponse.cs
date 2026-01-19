using CopilotSdk.Api.Services;

namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response containing persisted message history for a session.
/// </summary>
public class PersistedMessagesResponse
{
    /// <summary>
    /// The session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The persisted messages for this session.
    /// </summary>
    public List<PersistedMessage> Messages { get; set; } = new();

    /// <summary>
    /// Total count of messages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// When the session was created (if known).
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the session was last active.
    /// </summary>
    public DateTime? LastActivityAt { get; set; }
}
