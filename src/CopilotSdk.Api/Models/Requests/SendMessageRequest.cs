using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to send a message to a session.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// The message prompt to send.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Message mode: "enqueue" (default) or "immediate".
    /// "enqueue" adds to queue; "immediate" interrupts current processing.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Optional attachments to include with the message.
    /// </summary>
    public List<MessageAttachmentDto>? Attachments { get; set; }
}
