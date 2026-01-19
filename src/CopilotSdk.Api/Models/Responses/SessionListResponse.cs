namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response model containing a list of sessions.
/// </summary>
public class SessionListResponse
{
    /// <summary>
    /// List of session information.
    /// </summary>
    public List<SessionInfoResponse> Sessions { get; set; } = new();

    /// <summary>
    /// Total number of sessions.
    /// </summary>
    public int TotalCount { get; set; }
}
