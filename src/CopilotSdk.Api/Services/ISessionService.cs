using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service interface for session management operations.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session with the specified configuration.
    /// </summary>
    /// <param name="request">The session creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the created session.</returns>
    Task<SessionInfoResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes an existing session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="request">Optional resume configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the resumed session.</returns>
    Task<SessionInfoResponse> ResumeSessionAsync(string sessionId, ResumeSessionRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session information.</returns>
    Task<SessionListResponse> ListSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session information, or null if not found.</returns>
    Task<SessionInfoResponse?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was deleted, false if it wasn't found.</returns>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to a session.
    /// </summary>
    /// <param name="sessionId">The session ID to send the message to.</param>
    /// <param name="request">The message request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing the message ID.</returns>
    Task<SendMessageResponse> SendMessageAsync(string sessionId, SendMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages/events from a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing the session's messages/events.</returns>
    Task<MessagesResponse> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts the currently processing message in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the abort was successful, false if the session wasn't found.</returns>
    Task<bool> AbortAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the persisted message history for a session.
    /// This returns messages saved to disk, which may include messages from previous runs.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing the session's persisted message history.</returns>
    Task<PersistedMessagesResponse> GetPersistedHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the development server for a session's app.
    /// </summary>
    Task<DevServerResponse> StartDevServerAsync(string sessionId, string? appPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the development server for a session.
    /// </summary>
    Task StopDevServerAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of the development server for a session.
    /// </summary>
    Task<DevServerStatusResponse> GetDevServerStatusAsync(string sessionId, CancellationToken cancellationToken = default);
}
