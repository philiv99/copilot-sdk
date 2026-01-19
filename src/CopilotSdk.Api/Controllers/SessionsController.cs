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
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all available sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SessionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionListResponse>> ListSessions(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing all sessions");
        var response = await _sessionService.ListSessionsAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new session with the specified configuration.
    /// </summary>
    /// <param name="request">The session creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the created session.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SessionInfoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionInfoResponse>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating session with model {Model}", request.Model);

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Model is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await _sessionService.CreateSessionAsync(request, cancellationToken);
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
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteSession(
        string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting session {SessionId}", sessionId);

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
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="request">Optional resume configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the resumed session.</returns>
    [HttpPost("{sessionId}/resume")]
    [ProducesResponseType(typeof(SessionInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionInfoResponse>> ResumeSession(
        string sessionId,
        [FromBody] ResumeSessionRequest? request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Resuming session {SessionId}", sessionId);

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
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="request">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing the message ID.</returns>
    [HttpPost("{sessionId}/messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(
        string sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending message to session {SessionId}", sessionId);

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
}
