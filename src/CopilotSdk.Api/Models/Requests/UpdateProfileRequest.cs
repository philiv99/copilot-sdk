namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to update the current user's profile.
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>New display name (optional, null = no change).</summary>
    public string? DisplayName { get; set; }

    /// <summary>New email (optional, null = no change, must be unique).</summary>
    public string? Email { get; set; }

    /// <summary>New avatar type (optional).</summary>
    public string? AvatarType { get; set; }

    /// <summary>New avatar data (optional).</summary>
    public string? AvatarData { get; set; }
}
