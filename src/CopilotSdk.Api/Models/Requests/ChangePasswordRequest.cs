namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to change the current user's password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>Current password for verification.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>New password (minimum 6 characters).</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation of new password.</summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
