namespace CopilotSdk.Api.Services;

/// <summary>
/// Interface for data persistence operations.
/// Handles saving and loading session data and client configuration to/from JSON files.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Gets the base directory for data storage.
    /// </summary>
    string DataDirectory { get; }

    #region Client Configuration

    /// <summary>
    /// Saves the client configuration to a JSON file.
    /// </summary>
    /// <param name="config">The client configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveClientConfigAsync(Models.Domain.CopilotClientConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the client configuration from a JSON file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration, or null if no config file exists.</returns>
    Task<Models.Domain.CopilotClientConfig?> LoadClientConfigAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Session Data

    /// <summary>
    /// Saves a session's data to a JSON file.
    /// </summary>
    /// <param name="sessionData">The session data to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSessionAsync(PersistedSessionData sessionData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a session's data from a JSON file.
    /// </summary>
    /// <param name="sessionId">The session ID to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded session data, or null if no session file exists.</returns>
    Task<PersistedSessionData?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all persisted sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all persisted session data.</returns>
    Task<List<PersistedSessionData>> LoadAllSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session's data file.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was deleted, false if it didn't exist.</returns>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session data file exists.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <returns>True if the session file exists.</returns>
    bool SessionExists(string sessionId);

    /// <summary>
    /// Gets all persisted session IDs.
    /// </summary>
    /// <returns>List of session IDs.</returns>
    List<string> GetPersistedSessionIds();

    /// <summary>
    /// Assigns a CreatorUserId to all sessions that have no creator assigned.
    /// </summary>
    /// <param name="creatorUserId">The user ID to assign as creator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sessions updated.</returns>
    Task<int> AssignOrphanedSessionsAsync(string creatorUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Message Persistence

    /// <summary>
    /// Appends messages to a session's persisted data.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="messages">The messages to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AppendMessagesAsync(string sessionId, IEnumerable<PersistedMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of messages for the session.</returns>
    Task<List<PersistedMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region User Persistence

    /// <summary>
    /// Saves a user to the database (insert or update).
    /// </summary>
    Task SaveUserAsync(Models.Domain.User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<Models.Domain.User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their username (case-insensitive).
    /// </summary>
    Task<Models.Domain.User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email (case-insensitive).
    /// </summary>
    Task<Models.Domain.User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users, optionally filtered by active status.
    /// </summary>
    Task<List<Models.Domain.User>> GetAllUsersAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user by their ID (hard delete).
    /// </summary>
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total user count.
    /// </summary>
    Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);

    #endregion
}
