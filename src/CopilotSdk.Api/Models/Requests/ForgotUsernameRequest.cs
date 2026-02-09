namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to recover a forgotten username (stub — future email integration).
/// </summary>
public class ForgotUsernameRequest
{
    /// <summary>Email address associated with the account.</summary>
    public string Email { get; set; } = string.Empty;
}
