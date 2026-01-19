namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request model for pinging the Copilot server.
/// </summary>
public class PingRequest
{
    /// <summary>
    /// Optional message to include in the ping request.
    /// </summary>
    public string? Message { get; set; }
}
