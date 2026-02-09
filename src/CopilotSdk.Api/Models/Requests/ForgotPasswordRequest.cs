namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to reset a forgotten password (stub — future email integration).
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>Username or email associated with the account.</summary>
    public string UsernameOrEmail { get; set; } = string.Empty;
}
