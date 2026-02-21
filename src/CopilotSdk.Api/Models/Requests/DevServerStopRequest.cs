namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to stop a dev server process by PID.
/// </summary>
public class DevServerStopRequest
{
    /// <summary>
    /// The process ID of the dev server to stop.
    /// </summary>
    public int Pid { get; set; }
}
