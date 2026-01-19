namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response from sending a message to a session.
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// Unique identifier for the sent message.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the message was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the send failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The session ID the message was sent to.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}
