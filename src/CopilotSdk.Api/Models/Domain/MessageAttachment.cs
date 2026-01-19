namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Represents an attachment to a message.
/// </summary>
public class MessageAttachmentDto
{
    /// <summary>
    /// Type of attachment (e.g., "file", "uri").
    /// </summary>
    public string Type { get; set; } = "file";

    /// <summary>
    /// Path to the file (for file attachments).
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// URI reference (for URI attachments).
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Display name for the attachment.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Start line for file excerpts.
    /// </summary>
    public int? StartLine { get; set; }

    /// <summary>
    /// End line for file excerpts.
    /// </summary>
    public int? EndLine { get; set; }

    /// <summary>
    /// MIME type of the attachment.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Language identifier for code files.
    /// </summary>
    public string? Language { get; set; }
}
