namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to authenticate a user.
/// </summary>
public class LoginRequest
{
    /// <summary>Username to authenticate.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password to verify.</summary>
    public string Password { get; set; } = string.Empty;
}
