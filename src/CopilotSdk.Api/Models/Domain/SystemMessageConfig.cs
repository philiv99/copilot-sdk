namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Configuration for the system message in a session.
/// </summary>
public class SystemMessageConfig
{
    /// <summary>
    /// Mode for applying the system message: "Append" or "Replace".
    /// Append adds to the default system message, Replace overrides it entirely.
    /// </summary>
    public string Mode { get; set; } = "Append";

    /// <summary>
    /// The content of the system message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
