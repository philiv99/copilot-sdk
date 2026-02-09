using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for user management endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    private const string UserIdHeader = "X-User-Id";

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetCurrentUser), null, user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticate a user.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { error = result.Message });
        }
        return Ok(result);
    }

    /// <summary>
    /// Logout the current user (client-side action, server just acknowledges).
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Get the current user's profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Authentication required." });

        var user = await _userService.GetCurrentUserAsync(userId, cancellationToken);
        if (user == null)
            return Unauthorized(new { error = "User not found or inactive." });

        return Ok(user);
    }

    /// <summary>
    /// Update the current user's profile.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Authentication required." });

        try
        {
            var user = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Change the current user's password.
    /// </summary>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Authentication required." });

        try
        {
            await _userService.ChangePasswordAsync(userId, request, cancellationToken);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update the current user's avatar.
    /// </summary>
    [HttpPut("me/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Authentication required." });

        try
        {
            var user = await _userService.UpdateAvatarAsync(userId, request.AvatarType ?? "Default", request.AvatarData, cancellationToken);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all users (admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var authResult = await RequireRole(UserRole.Admin, cancellationToken);
        if (authResult != null)
            return authResult;

        var users = await _userService.GetAllUsersAsync(activeOnly, cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Get a specific user by ID (admin only).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var authResult = await RequireRole(UserRole.Admin, cancellationToken);
        if (authResult != null)
            return authResult;

        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new { error = $"User {id} not found." });

        return Ok(user);
    }

    /// <summary>
    /// Update a user (admin only).
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminUpdateUser(string id, [FromBody] AdminUpdateUserRequest request, CancellationToken cancellationToken)
    {
        var authResult = await RequireRole(UserRole.Admin, cancellationToken);
        if (authResult != null)
            return authResult;

        try
        {
            var user = await _userService.AdminUpdateUserAsync(id, request, cancellationToken);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate a user (admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateUser(string id, CancellationToken cancellationToken)
    {
        var authResult = await RequireRole(UserRole.Admin, cancellationToken);
        if (authResult != null)
            return authResult;

        try
        {
            await _userService.DeactivateUserAsync(id, cancellationToken);
            return Ok(new { message = "User deactivated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reactivate a user (admin only).
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateUser(string id, CancellationToken cancellationToken)
    {
        var authResult = await RequireRole(UserRole.Admin, cancellationToken);
        if (authResult != null)
            return authResult;

        try
        {
            await _userService.ActivateUserAsync(id, cancellationToken);
            return Ok(new { message = "User activated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reset a user's password (admin only). Returns the temporary password.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword(string id, CancellationToken cancellationToken)
    {
        var authResult = await RequireRole(UserRole.Admin, cancellationToken);
        if (authResult != null)
            return authResult;

        try
        {
            var tempPassword = await _userService.ResetPasswordAsync(id, cancellationToken);
            return Ok(new { message = "Password reset successfully.", temporaryPassword = tempPassword });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Forgot username (stub for future email integration).
    /// </summary>
    [HttpPost("forgot-username")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotUsername([FromBody] ForgotUsernameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _userService.ForgotUsernameAsync(request, cancellationToken);
            return Ok(new { message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Forgot password (stub for future email integration).
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _userService.ForgotPasswordAsync(request, cancellationToken);
            return Ok(new { message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available preset avatars.
    /// </summary>
    [HttpGet("avatars/presets")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAvatarPresets()
    {
        return Ok(_userService.GetAvatarPresets());
    }

    #region Private Helpers

    private string? GetCurrentUserId()
    {
        if (Request.Headers.TryGetValue(UserIdHeader, out var values))
        {
            var userId = values.FirstOrDefault();
            return string.IsNullOrWhiteSpace(userId) ? null : userId;
        }
        return null;
    }

    private async Task<IActionResult?> RequireRole(UserRole requiredRole, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Authentication required." });

        var user = await _userService.ValidateUserAsync(userId, cancellationToken);
        if (user == null)
            return Unauthorized(new { error = "User not found or inactive." });

        if (user.Role < requiredRole)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions." });

        return null;
    }

    #endregion
}
