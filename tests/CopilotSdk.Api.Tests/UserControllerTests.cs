using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

public class UserControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        _controller = new UserController(_mockUserService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void SetUserIdHeader(string userId)
    {
        _controller.HttpContext.Request.Headers["X-User-Id"] = userId;
    }

    private void SetAdminUser()
    {
        SetUserIdHeader("admin-id");
        _mockUserService.Setup(s => s.ValidateUserAsync("admin-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = "admin-id", Role = UserRole.Admin, IsActive = true });
    }

    private void SetPlayerUser()
    {
        SetUserIdHeader("player-id");
        _mockUserService.Setup(s => s.ValidateUserAsync("player-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = "player-id", Role = UserRole.Player, IsActive = true });
    }

    #region Register Tests

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            DisplayName = "New User",
            Password = "password123",
            ConfirmPassword = "password123"
        };
        _mockUserService.Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse { Id = "1", Username = "newuser" });

        var result = await _controller.Register(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidInput_Returns400()
    {
        var request = new RegisterRequest { Username = "" };
        _mockUserService.Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Username is required."));

        var result = await _controller.Register(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409()
    {
        var request = new RegisterRequest { Username = "existing" };
        _mockUserService.Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Username is already taken."));

        var result = await _controller.Register(request, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var request = new LoginRequest { Username = "test", Password = "pass" };
        _mockUserService.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { Success = true, User = new UserResponse { Username = "test" } });

        var result = await _controller.Login(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var request = new LoginRequest { Username = "test", Password = "wrong" };
        _mockUserService.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { Success = false, Message = "Invalid credentials." });

        var result = await _controller.Login(request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_Authenticated_Returns200()
    {
        SetUserIdHeader("user-1");
        _mockUserService.Setup(s => s.GetCurrentUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse { Id = "user-1", Username = "test" });

        var result = await _controller.GetCurrentUser(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentUser_NoHeader_Returns401()
    {
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentUser_InvalidUser_Returns401()
    {
        SetUserIdHeader("invalid-id");
        _mockUserService.Setup(s => s.GetCurrentUserAsync("invalid-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _controller.GetCurrentUser(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_Authenticated_Returns200()
    {
        SetUserIdHeader("user-1");
        var request = new UpdateProfileRequest { DisplayName = "New Name" };
        _mockUserService.Setup(s => s.UpdateProfileAsync("user-1", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse { DisplayName = "New Name" });

        var result = await _controller.UpdateProfile(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_NoAuth_Returns401()
    {
        var result = await _controller.UpdateProfile(new UpdateProfileRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_Valid_Returns200()
    {
        SetUserIdHeader("user-1");
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newpass",
            ConfirmNewPassword = "newpass"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync("user-1", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.ChangePassword(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns400()
    {
        SetUserIdHeader("user-1");
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "wrong",
            NewPassword = "newpass",
            ConfirmNewPassword = "newpass"
        };
        _mockUserService.Setup(s => s.ChangePasswordAsync("user-1", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Current password is incorrect."));

        var result = await _controller.ChangePassword(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Admin Endpoints Tests

    [Fact]
    public async Task GetAllUsers_Admin_Returns200()
    {
        SetAdminUser();
        _mockUserService.Setup(s => s.GetAllUsersAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Users = new List<UserResponse>(), TotalCount = 0 });

        var result = await _controller.GetAllUsers(null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAllUsers_Player_Returns403()
    {
        SetPlayerUser();

        var result = await _controller.GetAllUsers(null, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_NoAuth_Returns401()
    {
        var result = await _controller.GetAllUsers(null, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetUserById_Admin_Returns200()
    {
        SetAdminUser();
        _mockUserService.Setup(s => s.GetUserByIdAsync("target-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse { Id = "target-id" });

        var result = await _controller.GetUserById("target-id", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUserById_NotFound_Returns404()
    {
        SetAdminUser();
        _mockUserService.Setup(s => s.GetUserByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _controller.GetUserById("missing", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AdminUpdateUser_Admin_Returns200()
    {
        SetAdminUser();
        var request = new AdminUpdateUserRequest { Role = "Creator" };
        _mockUserService.Setup(s => s.AdminUpdateUserAsync("user-1", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse { Role = "Creator" });

        var result = await _controller.AdminUpdateUser("user-1", request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeactivateUser_Admin_Returns200()
    {
        SetAdminUser();
        _mockUserService.Setup(s => s.DeactivateUserAsync("user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeactivateUser("user-1", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ActivateUser_Admin_Returns200()
    {
        SetAdminUser();
        _mockUserService.Setup(s => s.ActivateUserAsync("user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.ActivateUser("user-1", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_Admin_Returns200WithTempPassword()
    {
        SetAdminUser();
        _mockUserService.Setup(s => s.ResetPasswordAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TempPass123!");

        var result = await _controller.ResetPassword("user-1", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Forgot Username/Password Tests

    [Fact]
    public async Task ForgotUsername_ReturnsMessage()
    {
        _mockUserService.Setup(s => s.ForgotUsernameAsync(It.IsAny<ForgotUsernameRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("If an account exists...");

        var result = await _controller.ForgotUsername(new ForgotUsernameRequest { Email = "test@example.com" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsMessage()
    {
        _mockUserService.Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("If an account exists...");

        var result = await _controller.ForgotPassword(new ForgotPasswordRequest { UsernameOrEmail = "test" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Avatar Presets Tests

    [Fact]
    public void GetAvatarPresets_Returns200()
    {
        _mockUserService.Setup(s => s.GetAvatarPresets())
            .Returns(new AvatarPresetsResponse { Presets = new List<AvatarPresetItem> { new() { Name = "default" } } });

        var result = _controller.GetAvatarPresets();

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public void Logout_Returns200()
    {
        var result = _controller.Logout();

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion
}
