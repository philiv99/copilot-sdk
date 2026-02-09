namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Public user information returned by the API.
/// </summary>
public class UserResponse
{
    /// <summary>User ID.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>User role.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Avatar type.</summary>
    public string AvatarType { get; set; } = string.Empty;

    /// <summary>Avatar data (preset name or base64).</summary>
    public string? AvatarData { get; set; }

    /// <summary>Whether the account is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Account creation date.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update date.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Last login date.</summary>
    public DateTime? LastLoginAt { get; set; }
}
