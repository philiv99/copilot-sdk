namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to register a new user account.
/// </summary>
public class RegisterRequest
{
    /// <summary>Desired username (3-50 chars, alphanumeric + underscore).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address (must be unique).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name for the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Password (minimum 6 characters).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Password confirmation (must match Password).</summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Optional avatar type selection.</summary>
    public string? AvatarType { get; set; }

    /// <summary>Optional avatar data (preset name or base64 image).</summary>
    public string? AvatarData { get; set; }
}
