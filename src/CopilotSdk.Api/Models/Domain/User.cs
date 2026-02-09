namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Domain model representing a user account.
/// </summary>
public class User
{
    /// <summary>Unique identifier (GUID).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Unique username (3-50 chars, alphanumeric + underscore).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Unique email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>SHA-256 password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Random salt for password hashing.</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>User role (Admin, Creator, Player).</summary>
    public UserRole Role { get; set; } = UserRole.Player;

    /// <summary>Type of avatar (Default, Preset, Custom).</summary>
    public AvatarType AvatarType { get; set; } = AvatarType.Default;

    /// <summary>Avatar data — preset name or base64 image data.</summary>
    public string? AvatarData { get; set; }

    /// <summary>Whether the account is active (soft delete).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Account creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last login timestamp (UTC), null if never logged in.</summary>
    public DateTime? LastLoginAt { get; set; }
}
