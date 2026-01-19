using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response containing session messages/events.
/// </summary>
public class MessagesResponse
{
    /// <summary>
    /// The session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// List of events/messages in the session.
    /// </summary>
    public List<SessionEventDto> Events { get; set; } = new();

    /// <summary>
    /// Total count of events.
    /// </summary>
    public int TotalCount { get; set; }
}
