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
    /// The user ID of the creator who owns this session.
    /// </summary>
    public string? CreatorUserId { get; set; }

    /// <summary>
    /// The configuration used to create this session.
    /// </summary>
    public SessionConfig? Config { get; set; }

    /// <summary>
    /// Local repository path for the app being built.
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// The repository/project folder name (e.g. "my-app"). Derived from AppPath if not set.
    /// </summary>
    public string? RepoName { get; set; }

    /// <summary>
    /// Port where the dev server is running (if started).
    /// </summary>
    public int? DevServerPort { get; set; }

    /// <summary>
    /// Whether the dev server is currently running.
    /// </summary>
    public bool IsDevServerRunning { get; set; }

    /// <summary>
    /// List of selected agent IDs for the session's team configuration.
    /// </summary>
    public List<string>? SelectedAgents { get; set; }

    /// <summary>
    /// The team preset ID used for the session (if any).
    /// </summary>
    public string? SelectedTeam { get; set; }

    /// <summary>
    /// The workflow pattern used for the team: "sequential", "parallel", or "hub-spoke".
    /// </summary>
    public string? WorkflowPattern { get; set; }
}
