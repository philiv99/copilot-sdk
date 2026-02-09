namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to set or update the app path for a session.
/// </summary>
public class SetAppPathRequest
{
    /// <summary>
    /// The absolute path to the app directory.
    /// </summary>
    public string AppPath { get; set; } = string.Empty;
}
