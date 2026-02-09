namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request for an admin to update any user's account.
/// </summary>
public class AdminUpdateUserRequest
{
    /// <summary>New display name (optional).</summary>
    public string? DisplayName { get; set; }

    /// <summary>New email (optional, must be unique).</summary>
    public string? Email { get; set; }

    /// <summary>New role (optional): Admin, Creator, Player.</summary>
    public string? Role { get; set; }

    /// <summary>New active status (optional).</summary>
    public bool? IsActive { get; set; }

    /// <summary>New avatar type (optional).</summary>
    public string? AvatarType { get; set; }

    /// <summary>New avatar data (optional).</summary>
    public string? AvatarData { get; set; }
}
