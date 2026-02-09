using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

public class UserServiceTests
{
    private readonly Mock<IPersistenceService> _mockPersistence;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockPersistence = new Mock<IPersistenceService>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _service = new UserService(_mockPersistence.Object, _mockLogger.Object);
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUser()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mockPersistence.Setup(p => p.GetUserByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("Player", result.Role);
        Assert.True(result.IsActive);

        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u =>
            u.Username == "testuser" &&
            u.Email == "test@example.com" &&
            u.Role == UserRole.Player
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_Throws()
    {
        var request = new RegisterRequest
        {
            Username = "existing",
            Email = "new@example.com",
            DisplayName = "New User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "existing" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegisterAsync(request));
        Assert.Contains("already taken", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "exists@example.com",
            DisplayName = "New User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mockPersistence.Setup(p => p.GetUserByEmailAsync("exists@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "exists@example.com" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegisterAsync(request));
        Assert.Contains("already registered", ex.Message);
    }

    [Theory]
    [InlineData("", "test@example.com", "Test", "pass123", "pass123", "Username is required")]
    [InlineData("ab", "test@example.com", "Test", "pass123", "pass123", "3-50 characters")]
    [InlineData("test user", "test@example.com", "Test", "pass123", "pass123", "alphanumeric")]
    [InlineData("testuser", "", "Test", "pass123", "pass123", "Email is required")]
    [InlineData("testuser", "invalid", "Test", "pass123", "pass123", "Invalid email")]
    [InlineData("testuser", "test@example.com", "", "pass123", "pass123", "Display name is required")]
    [InlineData("testuser", "test@example.com", "Test", "", "", "Password is required")]
    [InlineData("testuser", "test@example.com", "Test", "short", "short", "at least 6")]
    [InlineData("testuser", "test@example.com", "Test", "pass123", "different", "do not match")]
    public async Task RegisterAsync_InvalidInput_Throws(string username, string email, string displayName, string password, string confirm, string expectedError)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            Password = password,
            ConfirmPassword = confirm
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mockPersistence.Setup(p => p.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterAsync(request));
        Assert.Contains(expectedError, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterAsync_WithPresetAvatar_SetsAvatarCorrectly()
    {
        var request = new RegisterRequest
        {
            Username = "avataruser",
            Email = "avatar@example.com",
            DisplayName = "Avatar User",
            Password = "password123",
            ConfirmPassword = "password123",
            AvatarType = "Preset",
            AvatarData = "robot"
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mockPersistence.Setup(p => p.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.RegisterAsync(request);

        Assert.Equal("Preset", result.AvatarType);
        Assert.Equal("robot", result.AvatarData);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var salt = UserService.GenerateSalt();
        var user = new User
        {
            Id = "user-1",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = UserService.HashPassword("password123", salt),
            PasswordSalt = salt,
            Role = UserRole.Player,
            IsActive = true
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest { Username = "testuser", Password = "password123" });

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("testuser", result.User.Username);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFail()
    {
        var salt = UserService.GenerateSalt();
        var user = new User
        {
            Id = "user-1",
            Username = "testuser",
            PasswordHash = UserService.HashPassword("correctpass", salt),
            PasswordSalt = salt,
            IsActive = true
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest { Username = "testuser", Password = "wrongpass" });

        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Contains("Invalid", result.Message);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedUser_ReturnsFail()
    {
        var salt = UserService.GenerateSalt();
        var user = new User
        {
            Id = "user-1",
            Username = "testuser",
            PasswordHash = UserService.HashPassword("password123", salt),
            PasswordSalt = salt,
            IsActive = false
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest { Username = "testuser", Password = "password123" });

        Assert.False(result.Success);
        Assert.Contains("deactivated", result.Message);
    }

    [Fact]
    public async Task LoginAsync_NonexistentUser_ReturnsFail()
    {
        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("nobody", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(new LoginRequest { Username = "nobody", Password = "password123" });

        Assert.False(result.Success);
        Assert.Contains("Invalid", result.Message);
    }

    [Fact]
    public async Task LoginAsync_EmptyCredentials_ReturnsFail()
    {
        var result = await _service.LoginAsync(new LoginRequest { Username = "", Password = "" });

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
    }

    [Fact]
    public async Task LoginAsync_UpdatesLastLoginAt()
    {
        var salt = UserService.GenerateSalt();
        var user = new User
        {
            Id = "user-1",
            Username = "testuser",
            PasswordHash = UserService.HashPassword("password123", salt),
            PasswordSalt = salt,
            IsActive = true,
            LastLoginAt = null
        };

        _mockPersistence.Setup(p => p.GetUserByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.LoginAsync(new LoginRequest { Username = "testuser", Password = "password123" });

        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u =>
            u.LastLoginAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Profile Update Tests

    [Fact]
    public async Task UpdateProfileAsync_UpdatesDisplayName()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.UpdateProfileAsync("user-1", new UpdateProfileRequest { DisplayName = "New Name" });

        Assert.Equal("New Name", result.DisplayName);
        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u => u.DisplayName == "New Name"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesEmail_ChecksUniqueness()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPersistence.Setup(p => p.GetUserByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.UpdateProfileAsync("user-1", new UpdateProfileRequest { Email = "new@example.com" });

        Assert.Equal("new@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateProfileAsync_DuplicateEmail_Throws()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPersistence.Setup(p => p.GetUserByEmailAsync("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = "user-2", Email = "taken@example.com" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateProfileAsync("user-1", new UpdateProfileRequest { Email = "taken@example.com" }));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_Throws()
    {
        _mockPersistence.Setup(p => p.GetUserByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateProfileAsync("missing", new UpdateProfileRequest { DisplayName = "X" }));
    }

    #endregion

    #region Password Change Tests

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ChangesPassword()
    {
        var salt = UserService.GenerateSalt();
        var user = new User
        {
            Id = "user-1",
            Username = "testuser",
            PasswordHash = UserService.HashPassword("oldpass", salt),
            PasswordSalt = salt
        };
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.ChangePasswordAsync("user-1", new ChangePasswordRequest
        {
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        });

        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u =>
            u.PasswordHash != UserService.HashPassword("oldpass", salt)
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_Throws()
    {
        var salt = UserService.GenerateSalt();
        var user = new User
        {
            Id = "user-1",
            PasswordHash = UserService.HashPassword("correctpass", salt),
            PasswordSalt = salt
        };
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangePasswordAsync("user-1", new ChangePasswordRequest
            {
                CurrentPassword = "wrongpass",
                NewPassword = "newpass123",
                ConfirmNewPassword = "newpass123"
            }));
        Assert.Contains("incorrect", ex.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_MismatchConfirmation_Throws()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangePasswordAsync("user-1", new ChangePasswordRequest
            {
                CurrentPassword = "old",
                NewPassword = "newpass123",
                ConfirmNewPassword = "different"
            }));
        Assert.Contains("do not match", ex.Message);
    }

    #endregion

    #region Admin Operations Tests

    [Fact]
    public async Task AdminUpdateUserAsync_UpdatesRole()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.AdminUpdateUserAsync("user-1", new AdminUpdateUserRequest { Role = "Creator" });

        Assert.Equal("Creator", result.Role);
    }

    [Fact]
    public async Task AdminUpdateUserAsync_InvalidRole_Throws()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AdminUpdateUserAsync("user-1", new AdminUpdateUserRequest { Role = "SuperAdmin" }));
        Assert.Contains("Invalid role", ex.Message);
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsIsActiveFalse()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.DeactivateUserAsync("user-1");

        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u => !u.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateUserAsync_SetsIsActiveTrue()
    {
        var user = CreateTestUser("user-1", "testuser");
        user.IsActive = false;
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.ActivateUserAsync("user-1");

        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u => u.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsTemporaryPassword()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var tempPassword = await _service.ResetPasswordAsync("user-1");

        Assert.NotNull(tempPassword);
        Assert.True(tempPassword.Length >= 8);
        _mockPersistence.Verify(p => p.SaveUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            CreateTestUser("1", "user1"),
            CreateTestUser("2", "user2"),
            CreateTestUser("3", "user3")
        };
        _mockPersistence.Setup(p => p.GetAllUsersAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _service.GetAllUsersAsync();

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Users.Count);
    }

    #endregion

    #region Forgot Username/Password Tests

    [Fact]
    public async Task ForgotUsernameAsync_ReturnsGenericMessage()
    {
        var message = await _service.ForgotUsernameAsync(new ForgotUsernameRequest { Email = "test@example.com" });

        Assert.Contains("username has been sent", message);
    }

    [Fact]
    public async Task ForgotUsernameAsync_EmptyEmail_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ForgotUsernameAsync(new ForgotUsernameRequest { Email = "" }));
    }

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsGenericMessage()
    {
        var message = await _service.ForgotPasswordAsync(new ForgotPasswordRequest { UsernameOrEmail = "testuser" });

        Assert.Contains("password reset link", message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_EmptyInput_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ForgotPasswordAsync(new ForgotPasswordRequest { UsernameOrEmail = "" }));
    }

    #endregion

    #region EnsureDefaultAdmin Tests

    [Fact]
    public async Task EnsureDefaultAdminAsync_NoUsers_CreatesAdmin()
    {
        _mockPersistence.Setup(p => p.GetUserCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _service.EnsureDefaultAdminAsync();

        _mockPersistence.Verify(p => p.SaveUserAsync(It.Is<User>(u =>
            u.Username == "admin" &&
            u.Role == UserRole.Admin
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureDefaultAdminAsync_UsersExist_SkipsCreation()
    {
        _mockPersistence.Setup(p => p.GetUserCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        await _service.EnsureDefaultAdminAsync();

        _mockPersistence.Verify(p => p.SaveUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Avatar Tests

    [Fact]
    public void GetAvatarPresets_Returns12Presets()
    {
        var presets = _service.GetAvatarPresets();

        Assert.Equal(12, presets.Presets.Count);
        Assert.Contains(presets.Presets, p => p.Name == "default");
        Assert.Contains(presets.Presets, p => p.Name == "robot");
    }

    [Fact]
    public async Task UpdateAvatarAsync_SetsPresetAvatar()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.UpdateAvatarAsync("user-1", "Preset", "cat");

        Assert.Equal("Preset", result.AvatarType);
        Assert.Equal("cat", result.AvatarData);
    }

    [Fact]
    public async Task UpdateAvatarAsync_InvalidPreset_Throws()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateAvatarAsync("user-1", "Preset", "nonexistent"));
    }

    #endregion

    #region ValidateUser Tests

    [Fact]
    public async Task ValidateUserAsync_ActiveUser_ReturnsUser()
    {
        var user = CreateTestUser("user-1", "testuser");
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.ValidateUserAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal("user-1", result.Id);
    }

    [Fact]
    public async Task ValidateUserAsync_InactiveUser_ReturnsNull()
    {
        var user = CreateTestUser("user-1", "testuser");
        user.IsActive = false;
        _mockPersistence.Setup(p => p.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.ValidateUserAsync("user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUserAsync_EmptyId_ReturnsNull()
    {
        var result = await _service.ValidateUserAsync("");

        Assert.Null(result);
    }

    #endregion

    #region Password Hashing Tests

    [Fact]
    public void HashPassword_SameSalt_ProducesSameHash()
    {
        var salt = UserService.GenerateSalt();
        var hash1 = UserService.HashPassword("password", salt);
        var hash2 = UserService.HashPassword("password", salt);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_DifferentSalt_ProducesDifferentHash()
    {
        var salt1 = UserService.GenerateSalt();
        var salt2 = UserService.GenerateSalt();
        var hash1 = UserService.HashPassword("password", salt1);
        var hash2 = UserService.HashPassword("password", salt2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateSalt_ProducesUniqueSalts()
    {
        var salt1 = UserService.GenerateSalt();
        var salt2 = UserService.GenerateSalt();

        Assert.NotEqual(salt1, salt2);
    }

    #endregion

    #region Helpers

    private static User CreateTestUser(string id, string username)
    {
        var salt = UserService.GenerateSalt();
        return new User
        {
            Id = id,
            Username = username,
            Email = $"{username}@example.com",
            DisplayName = $"Test {username}",
            PasswordHash = UserService.HashPassword("password123", salt),
            PasswordSalt = salt,
            Role = UserRole.Player,
            AvatarType = AvatarType.Default,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
