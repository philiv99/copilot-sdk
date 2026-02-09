namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response returned after successful login.
/// </summary>
public class LoginResponse
{
    /// <summary>Whether login was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Message (e.g., error description on failure).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Authenticated user info (null on failure).</summary>
    public UserResponse? User { get; set; }
}
