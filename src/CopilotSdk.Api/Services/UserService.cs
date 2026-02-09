using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for user management operations including registration, authentication, and profile management.
/// </summary>
public class UserService : IUserService
{
    private readonly IPersistenceService _persistence;
    private readonly ILogger<UserService> _logger;

    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private const int MinPasswordLength = 6;
    private const int MaxAvatarDataLength = 350_000; // ~256KB base64

    /// <summary>
    /// Preset avatars available for selection.
    /// </summary>
    public static readonly List<AvatarPresetItem> PresetAvatars = new()
    {
        new() { Name = "default", Label = "Default", Emoji = "👤" },
        new() { Name = "astronaut", Label = "Astronaut", Emoji = "🧑‍🚀" },
        new() { Name = "robot", Label = "Robot", Emoji = "🤖" },
        new() { Name = "ninja", Label = "Ninja", Emoji = "🥷" },
        new() { Name = "wizard", Label = "Wizard", Emoji = "🧙" },
        new() { Name = "pirate", Label = "Pirate", Emoji = "🏴‍☠️" },
        new() { Name = "alien", Label = "Alien", Emoji = "👽" },
        new() { Name = "cat", Label = "Cat", Emoji = "🐱" },
        new() { Name = "dog", Label = "Dog", Emoji = "🐶" },
        new() { Name = "dragon", Label = "Dragon", Emoji = "🐉" },
        new() { Name = "unicorn", Label = "Unicorn", Emoji = "🦄" },
        new() { Name = "phoenix", Label = "Phoenix", Emoji = "🔥" },
    };

    public UserService(IPersistenceService persistence, ILogger<UserService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ArgumentException("Username is required.");
        if (!UsernameRegex.IsMatch(request.Username))
            throw new ArgumentException("Username must be 3-50 characters, alphanumeric and underscores only.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");
        if (!EmailRegex.IsMatch(request.Email))
            throw new ArgumentException("Invalid email format.");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new ArgumentException("Display name is required.");
        if (request.DisplayName.Length > 100)
            throw new ArgumentException("Display name must be 100 characters or fewer.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");
        if (request.Password.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters.");
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException("Passwords do not match.");

        // Check uniqueness
        var existingByUsername = await _persistence.GetUserByUsernameAsync(request.Username, cancellationToken);
        if (existingByUsername != null)
            throw new InvalidOperationException("Username is already taken.");

        var existingByEmail = await _persistence.GetUserByEmailAsync(request.Email, cancellationToken);
        if (existingByEmail != null)
            throw new InvalidOperationException("Email is already registered.");

        // Parse avatar
        var avatarType = ParseAvatarType(request.AvatarType);
        ValidateAvatarData(avatarType, request.AvatarData);

        // Create user
        var salt = GenerateSalt();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = HashPassword(request.Password, salt),
            PasswordSalt = salt,
            Role = UserRole.Player, // Default role for new registrations
            AvatarType = avatarType,
            AvatarData = request.AvatarData,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Registered new user: {Username} ({UserId})", user.Username, user.Id);

        return MapToResponse(user);
    }

    /// <inheritdoc/>
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponse { Success = false, Message = "Username and password are required." };
        }

        var user = await _persistence.GetUserByUsernameAsync(request.Username, cancellationToken);
        if (user == null)
        {
            _logger.LogDebug("Login failed: user {Username} not found", request.Username);
            return new LoginResponse { Success = false, Message = "Invalid username or password." };
        }

        if (!user.IsActive)
        {
            _logger.LogDebug("Login failed: user {Username} is deactivated", request.Username);
            return new LoginResponse { Success = false, Message = "Account is deactivated. Contact an administrator." };
        }

        var hash = HashPassword(request.Password, user.PasswordSalt);
        if (hash != user.PasswordHash)
        {
            _logger.LogDebug("Login failed: invalid password for {Username}", request.Username);
            return new LoginResponse { Success = false, Message = "Invalid username or password." };
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _persistence.SaveUserAsync(user, cancellationToken);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);
        return new LoginResponse
        {
            Success = true,
            Message = "Login successful.",
            User = MapToResponse(user)
        };
    }

    /// <inheritdoc/>
    public async Task<UserResponse?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        return user != null ? MapToResponse(user) : null;
    }

    /// <inheritdoc/>
    public async Task<UserResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        if (request.DisplayName != null)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
                throw new ArgumentException("Display name cannot be empty.");
            if (request.DisplayName.Length > 100)
                throw new ArgumentException("Display name must be 100 characters or fewer.");
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.Email != null)
        {
            if (!EmailRegex.IsMatch(request.Email))
                throw new ArgumentException("Invalid email format.");
            var existingByEmail = await _persistence.GetUserByEmailAsync(request.Email, cancellationToken);
            if (existingByEmail != null && existingByEmail.Id != userId)
                throw new InvalidOperationException("Email is already registered.");
            user.Email = request.Email.Trim().ToLowerInvariant();
        }

        if (request.AvatarType != null)
        {
            user.AvatarType = ParseAvatarType(request.AvatarType);
            user.AvatarData = request.AvatarData;
            ValidateAvatarData(user.AvatarType, user.AvatarData);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Updated profile for user {UserId}", userId);

        return MapToResponse(user);
    }

    /// <inheritdoc/>
    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new ArgumentException("Current password is required.");
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ArgumentException("New password is required.");
        if (request.NewPassword.Length < MinPasswordLength)
            throw new ArgumentException($"New password must be at least {MinPasswordLength} characters.");
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new ArgumentException("New passwords do not match.");

        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        var currentHash = HashPassword(request.CurrentPassword, user.PasswordSalt);
        if (currentHash != user.PasswordHash)
            throw new InvalidOperationException("Current password is incorrect.");

        var newSalt = GenerateSalt();
        user.PasswordHash = HashPassword(request.NewPassword, newSalt);
        user.PasswordSalt = newSalt;
        user.UpdatedAt = DateTime.UtcNow;

        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Password changed for user {UserId}", userId);
    }

    /// <inheritdoc/>
    public async Task<UserResponse> UpdateAvatarAsync(string userId, string avatarType, string? avatarData, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        user.AvatarType = ParseAvatarType(avatarType);
        user.AvatarData = avatarData;
        ValidateAvatarData(user.AvatarType, user.AvatarData);
        user.UpdatedAt = DateTime.UtcNow;

        await _persistence.SaveUserAsync(user, cancellationToken);
        return MapToResponse(user);
    }

    /// <inheritdoc/>
    public async Task<UserListResponse> GetAllUsersAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var users = await _persistence.GetAllUsersAsync(activeOnly, cancellationToken);
        return new UserListResponse
        {
            Users = users.Select(MapToResponse).ToList(),
            TotalCount = users.Count
        };
    }

    /// <inheritdoc/>
    public async Task<UserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        return user != null ? MapToResponse(user) : null;
    }

    /// <inheritdoc/>
    public async Task<UserResponse> AdminUpdateUserAsync(string userId, AdminUpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        if (request.DisplayName != null)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
                throw new ArgumentException("Display name cannot be empty.");
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.Email != null)
        {
            if (!EmailRegex.IsMatch(request.Email))
                throw new ArgumentException("Invalid email format.");
            var existingByEmail = await _persistence.GetUserByEmailAsync(request.Email, cancellationToken);
            if (existingByEmail != null && existingByEmail.Id != userId)
                throw new InvalidOperationException("Email is already registered.");
            user.Email = request.Email.Trim().ToLowerInvariant();
        }

        if (request.Role != null)
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
                throw new ArgumentException($"Invalid role: {request.Role}. Valid roles: Admin, Creator, Player.");
            user.Role = role;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (request.AvatarType != null)
        {
            user.AvatarType = ParseAvatarType(request.AvatarType);
            user.AvatarData = request.AvatarData;
            ValidateAvatarData(user.AvatarType, user.AvatarData);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Admin updated user {UserId}", userId);

        return MapToResponse(user);
    }

    /// <inheritdoc/>
    public async Task DeactivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Deactivated user {UserId}", userId);
    }

    /// <inheritdoc/>
    public async Task ActivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Activated user {UserId}", userId);
    }

    /// <inheritdoc/>
    public async Task<string> ResetPasswordAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found.");

        // Generate a temporary password
        var tempPassword = GenerateTemporaryPassword();
        var newSalt = GenerateSalt();
        user.PasswordHash = HashPassword(tempPassword, newSalt);
        user.PasswordSalt = newSalt;
        user.UpdatedAt = DateTime.UtcNow;

        await _persistence.SaveUserAsync(user, cancellationToken);
        _logger.LogInformation("Admin reset password for user {UserId}", userId);

        return tempPassword;
    }

    /// <inheritdoc/>
    public AvatarPresetsResponse GetAvatarPresets()
    {
        return new AvatarPresetsResponse { Presets = PresetAvatars };
    }

    /// <inheritdoc/>
    public async Task<string> ForgotUsernameAsync(ForgotUsernameRequest request, CancellationToken cancellationToken = default)
    {
        // Stub: In the future, this would send an email with the username.
        // For now, we log the request and return a generic message.
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");

        _logger.LogInformation("[STUB] Forgot username requested for email: {Email}", request.Email);

        // Future: await SendUsernameReminderEmailAsync(request.Email);
        await Task.CompletedTask;

        return "If an account exists with this email, the username has been sent to the registered email address.";
    }

    /// <inheritdoc/>
    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Stub: In the future, this would send a password reset email or SMS.
        // For now, we log the request and return a generic message.
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail))
            throw new ArgumentException("Username or email is required.");

        _logger.LogInformation("[STUB] Forgot password requested for: {UsernameOrEmail}", request.UsernameOrEmail);

        // Future: await SendPasswordResetEmailAsync(request.UsernameOrEmail);
        await Task.CompletedTask;

        return "If an account exists, a password reset link has been sent to the registered email address.";
    }

    /// <inheritdoc/>
    public async Task<User?> ValidateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await _persistence.GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
            return null;

        return user;
    }

    /// <inheritdoc/>
    public async Task EnsureDefaultAdminAsync(CancellationToken cancellationToken = default)
    {
        var userCount = await _persistence.GetUserCountAsync(cancellationToken);
        if (userCount > 0)
        {
            _logger.LogDebug("Users already exist, skipping default admin creation");
            return;
        }

        var salt = GenerateSalt();
        var admin = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "admin",
            Email = "admin@local",
            DisplayName = "Administrator",
            PasswordHash = HashPassword("admin123", salt),
            PasswordSalt = salt,
            Role = UserRole.Admin,
            AvatarType = AvatarType.Preset,
            AvatarData = "wizard",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _persistence.SaveUserAsync(admin, cancellationToken);
        _logger.LogInformation("Created default admin account (username: admin, password: admin123)");
    }

    /// <inheritdoc/>
    public async Task EnsureDefaultCreatorAsync(CancellationToken cancellationToken = default)
    {
        // Check if Fred already exists
        var existingFred = await _persistence.GetUserByUsernameAsync("fred", cancellationToken);
        if (existingFred != null)
        {
            // Fred exists — just assign any orphaned sessions to Fred
            await _persistence.AssignOrphanedSessionsAsync(existingFred.Id, cancellationToken);
            _logger.LogDebug("Default creator 'fred' already exists ({UserId}), assigned orphaned sessions", existingFred.Id);
            return;
        }

        var salt = GenerateSalt();
        var fred = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = "fred",
            Email = "fred@local",
            DisplayName = "Fred",
            PasswordHash = HashPassword("fred123", salt),
            PasswordSalt = salt,
            Role = UserRole.Creator,
            AvatarType = AvatarType.Preset,
            AvatarData = "dragon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _persistence.SaveUserAsync(fred, cancellationToken);
        _logger.LogInformation("Created default creator account (username: fred, password: fred123)");

        // Assign all orphaned sessions (no creator) to Fred
        await _persistence.AssignOrphanedSessionsAsync(fred.Id, cancellationToken);
    }

    #region Private Helpers

    internal static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    internal static string HashPassword(string password, string salt)
    {
        var combined = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = SHA256.HashData(combined);
        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateTemporaryPassword()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
        var passwordBytes = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(passwordBytes);

        var password = new char[12];
        for (int i = 0; i < 12; i++)
        {
            password[i] = chars[passwordBytes[i] % chars.Length];
        }
        return new string(password);
    }

    private static AvatarType ParseAvatarType(string? avatarType)
    {
        if (string.IsNullOrWhiteSpace(avatarType))
            return AvatarType.Default;

        if (Enum.TryParse<AvatarType>(avatarType, true, out var result))
            return result;

        throw new ArgumentException($"Invalid avatar type: {avatarType}. Valid types: Default, Preset, Custom.");
    }

    private static void ValidateAvatarData(AvatarType type, string? data)
    {
        switch (type)
        {
            case AvatarType.Preset:
                if (string.IsNullOrWhiteSpace(data))
                    throw new ArgumentException("Preset avatar name is required.");
                if (!PresetAvatars.Any(p => p.Name == data))
                    throw new ArgumentException($"Unknown preset avatar: {data}.");
                break;
            case AvatarType.Custom:
                if (string.IsNullOrWhiteSpace(data))
                    throw new ArgumentException("Custom avatar data is required.");
                if (data.Length > MaxAvatarDataLength)
                    throw new ArgumentException("Avatar image is too large. Maximum size is ~256KB.");
                break;
        }
    }

    internal static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            AvatarType = user.AvatarType.ToString(),
            AvatarData = user.AvatarData,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    /// <summary>
    /// Stub for future email integration — send username reminder.
    /// </summary>
    internal static Task SendUsernameReminderEmailAsync(string email)
    {
        // Future: integrate with email service (SendGrid, SMTP, etc.)
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stub for future email/SMS integration — send password reset link.
    /// </summary>
    internal static Task SendPasswordResetEmailAsync(string usernameOrEmail)
    {
        // Future: integrate with email/SMS service
        return Task.CompletedTask;
    }

    #endregion
}
