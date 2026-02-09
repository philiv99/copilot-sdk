using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for session management operations.
/// </summary>
[ApiController]
[Route("api/copilot/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IUserService _userService;
    private readonly ILogger<SessionsController> _logger;

    private const string UserIdHeader = "X-User-Id";

    public SessionsController(ISessionService sessionService, IUserService userService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Lists sessions visible to the authenticated user based on their role.
    /// Admins see all sessions. Creators see only their own. Players see playable games.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SessionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionListResponse>> ListSessions(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing sessions");
        var user = await GetCurrentUserAsync(cancellationToken);
        var response = await _sessionService.ListSessionsAsync(user, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new session with the specified configuration.
    /// Requires Creator or Admin role.
    /// </summary>
    /// <param name="request">The session creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the created session.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SessionInfoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionInfoResponse>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating session with model {Model}", request.Model);

        // Require Creator or Admin role
        var authResult = await RequireRole(UserRole.Creator, cancellationToken);
        if (authResult != null) return authResult;

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Model is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var userId = GetCurrentUserId();
        var response = await _sessionService.CreateSessionAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId = response.SessionId }, response);
    }

    /// <summary>
    /// Gets information about a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session information.</returns>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(SessionInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionInfoResponse>> GetSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting session {SessionId}", sessionId);

        var response = await _sessionService.GetSessionAsync(sessionId, cancellationToken);

        if (response == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session Not Found",
                Detail = $"Session with ID '{sessionId}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes a session by ID.
    /// Requires Creator or Admin role. Creators can only delete their own sessions.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting session {SessionId}", sessionId);

        // Require Creator or Admin role
        var authResult = await RequireRole(UserRole.Creator, cancellationToken);
        if (authResult != null) return authResult;

        // Check ownership for non-admins
        var ownershipResult = await RequireSessionOwnership(sessionId, cancellationToken);
        if (ownershipResult != null) return ownershipResult;

        var deleted = await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session Not Found",
                Detail = $"Session with ID '{sessionId}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Resumes an existing session by ID.
    /// Requires Creator or Admin role. Creators can only resume their own sessions.
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="request">Optional resume configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the resumed session.</returns>
    [HttpPost("{sessionId}/resume")]
    [ProducesResponseType(typeof(SessionInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionInfoResponse>> ResumeSession(
        string sessionId,
        [FromBody] ResumeSessionRequest? request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Resuming session {SessionId}", sessionId);

        // Require Creator or Admin role
        var authResult = await RequireRole(UserRole.Creator, cancellationToken);
        if (authResult != null) return authResult;

        // Check ownership for non-admins
        var ownershipResult = await RequireSessionOwnership(sessionId, cancellationToken);
        if (ownershipResult != null) return ownershipResult;

        try
        {
            var response = await _sessionService.ResumeSessionAsync(sessionId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("does not exist"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session Not Found",
                Detail = $"Session with ID '{sessionId}' was not found or cannot be resumed",
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Sends a message to a session.
    /// Requires Creator or Admin role. Creators can only send to their own sessions.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="request">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing the message ID.</returns>
    [HttpPost("{sessionId}/messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(
        string sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending message to session {SessionId}", sessionId);

        // Require Creator or Admin role
        var authResult = await RequireRole(UserRole.Creator, cancellationToken);
        if (authResult != null) return authResult;

        // Check ownership for non-admins
        var ownershipResult = await RequireSessionOwnership(sessionId, cancellationToken);
        if (ownershipResult != null) return ownershipResult;

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Prompt is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await _sessionService.SendMessageAsync(sessionId, request, cancellationToken);

        if (!response.Success)
        {
            if (response.Error?.Contains("not found") == true || response.Error?.Contains("not active") == true)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Session Not Found",
                    Detail = response.Error,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Send Message Failed",
                Detail = response.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets all messages/events from a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session events.</returns>
    [HttpGet("{sessionId}/messages")]
    [ProducesResponseType(typeof(MessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MessagesResponse>> GetMessages(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting messages for session {SessionId}", sessionId);

        // First check if session exists
        var sessionInfo = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        if (sessionInfo == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session Not Found",
                Detail = $"Session with ID '{sessionId}' was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        var response = await _sessionService.GetMessagesAsync(sessionId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Gets the persisted message history for a session.
    /// Returns messages saved to disk from current and previous runs.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted message history.</returns>
    [HttpGet("{sessionId}/history")]
    [ProducesResponseType(typeof(PersistedMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersistedMessagesResponse>> GetSessionHistory(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting persisted history for session {SessionId}", sessionId);

        var response = await _sessionService.GetPersistedHistoryAsync(sessionId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Aborts the currently processing message in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{sessionId}/abort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> AbortSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Aborting session {SessionId}", sessionId);

        var aborted = await _sessionService.AbortAsync(sessionId, cancellationToken);

        if (!aborted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session Not Found",
                Detail = $"Session with ID '{sessionId}' was not found or is not active",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Starts the development server for a session's app.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="appPath">Optional override for the app path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dev server information.</returns>
    [HttpPost("{sessionId}/dev-server/start")]
    [ProducesResponseType(typeof(DevServerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DevServerResponse>> StartDevServer(
        string sessionId,
        [FromQuery] string? appPath,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting dev server for session {SessionId}", sessionId);
        var response = await _sessionService.StartDevServerAsync(sessionId, appPath, cancellationToken);
        
        if (!response.Success)
        {
            return Problem(response.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(response);
    }

    /// <summary>
    /// Stops the development server for a session's app.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{sessionId}/dev-server/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StopDevServer(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping dev server for session {SessionId}", sessionId);
        await _sessionService.StopDevServerAsync(sessionId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sets or updates the app path for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="appPath">The absolute path to the app directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{sessionId}/app-path")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SetAppPath(
        string sessionId,
        [FromBody] SetAppPathRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.AppPath))
            return BadRequest(new ProblemDetails { Title = "Invalid Request", Detail = "appPath is required", Status = StatusCodes.Status400BadRequest });

        _logger.LogDebug("Setting app path for session {SessionId} to {AppPath}", sessionId, request.AppPath);
        await _sessionService.SetAppPathAsync(sessionId, request.AppPath, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the status of the development server for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{sessionId}/dev-server/status")]
    [ProducesResponseType(typeof(DevServerStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DevServerStatusResponse>> GetDevServerStatus(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var status = await _sessionService.GetDevServerStatusAsync(sessionId, cancellationToken);
        return Ok(status);
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

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;
        return await _userService.ValidateUserAsync(userId, cancellationToken);
    }

    private async Task<ActionResult?> RequireRole(UserRole requiredRole, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "Authentication required." });

        var user = await _userService.ValidateUserAsync(userId, cancellationToken);
        if (user == null)
            return Unauthorized(new { error = "User not found or inactive." });

        if (user.Role < requiredRole)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions. Creator or Admin role required." });

        return null;
    }

    /// <summary>
    /// Checks that the current user owns the session (or is an admin).
    /// </summary>
    private async Task<ActionResult?> RequireSessionOwnership(string sessionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Authentication required." });

        var user = await _userService.ValidateUserAsync(userId, cancellationToken);
        if (user == null) return Unauthorized(new { error = "User not found or inactive." });

        // Admins can access any session
        if (user.Role == UserRole.Admin) return null;

        // Check if this user owns the session
        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
            return NotFound(new ProblemDetails { Title = "Session Not Found", Detail = $"Session with ID '{sessionId}' was not found", Status = StatusCodes.Status404NotFound });

        if (session.CreatorUserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "You do not have access to this session." });

        return null;
    }

    #endregion
}
