using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>Register a new user account.</summary>
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Authenticate a user and return their info.</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Get the current user's profile.</summary>
    Task<UserResponse?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Update the current user's profile.</summary>
    Task<UserResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Change the current user's password.</summary>
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update the current user's avatar.</summary>
    Task<UserResponse> UpdateAvatarAsync(string userId, string avatarType, string? avatarData, CancellationToken cancellationToken = default);

    /// <summary>Get all users (admin only).</summary>
    Task<UserListResponse> GetAllUsersAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>Get a user by ID (admin only).</summary>
    Task<UserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Admin update any user.</summary>
    Task<UserResponse> AdminUpdateUserAsync(string userId, AdminUpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Admin deactivate (soft-delete) a user.</summary>
    Task DeactivateUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Admin reactivate a user.</summary>
    Task ActivateUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Admin reset a user's password.</summary>
    Task<string> ResetPasswordAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Get available preset avatars.</summary>
    AvatarPresetsResponse GetAvatarPresets();

    /// <summary>Forgot username stub — returns a generic message.</summary>
    Task<string> ForgotUsernameAsync(ForgotUsernameRequest request, CancellationToken cancellationToken = default);

    /// <summary>Forgot password stub — returns a generic message.</summary>
    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Validate that a user exists and is active. Returns the user or null.</summary>
    Task<User?> ValidateUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Ensure the default admin account exists (seed data).</summary>
    Task EnsureDefaultAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>Ensure the default creator account "Fred" exists and assign orphaned sessions to Fred.</summary>
    Task EnsureDefaultCreatorAsync(CancellationToken cancellationToken = default);
}
