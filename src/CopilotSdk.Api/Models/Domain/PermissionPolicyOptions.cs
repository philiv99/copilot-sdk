namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Configures how SDK permission requests are handled for local app creation sessions.
/// </summary>
public class PermissionPolicyOptions
{
    /// <summary>
    /// Policy mode. "Deny" refuses permission requests; "AutoApproveLocalExecutor"
    /// approves local development operations within the configured root.
    /// </summary>
    public string Mode { get; set; } = "Deny";

    /// <summary>
    /// Absolute root path that shell/write/read requests may target.
    /// </summary>
    public string AllowedRoot { get; set; } = @"C:\development\repos";

    /// <summary>
    /// Permission request kinds that may be approved in local executor mode.
    /// </summary>
    public List<string> AllowedKinds { get; set; } = new() { "shell", "write", "read" };

    /// <summary>
    /// Regex patterns that force denial when found in the serialized permission request.
    /// </summary>
    public List<string> DeniedCommandPatterns { get; set; } = new();
}
