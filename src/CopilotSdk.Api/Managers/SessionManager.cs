using System.Collections.Concurrent;
using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using SdkSessionMetadata = GitHub.Copilot.SDK.SessionMetadata;

namespace CopilotSdk.Api.Managers;

/// <summary>
/// Manages active sessions and their metadata.
/// Session metadata is persisted to disk only - no in-memory caching.
/// Active SDK session objects are tracked in memory as they are runtime objects.
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly IPersistenceService? _persistenceService;
    // Only track active SDK session objects (runtime) - not metadata
    private readonly ConcurrentDictionary<string, CopilotSession> _activeSessions = new();
    private readonly ConcurrentDictionary<string, IDisposable> _eventSubscriptions = new();
    private SessionEventDispatcher? _eventDispatcher;

    public SessionManager(ILogger<SessionManager> logger, IPersistenceService? persistenceService = null)
    {
        _logger = logger;
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Sets the event dispatcher for real-time event streaming.
    /// </summary>
    /// <param name="dispatcher">The event dispatcher to use.</param>
    public void SetEventDispatcher(SessionEventDispatcher dispatcher)
    {
        _eventDispatcher = dispatcher;
        _logger.LogInformation("Event dispatcher set for session manager");
    }

    /// <summary>
    /// Registers a newly created session with its metadata.
    /// The metadata is persisted to disk immediately - no in-memory caching.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The SDK session instance.</param>
    /// <param name="config">The configuration used to create the session.</param>
    public async Task RegisterSessionAsync(string sessionId, CopilotSession session, Models.Domain.SessionConfig config, CancellationToken cancellationToken = default)
    {
        var metadata = new Models.Domain.SessionMetadata
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            MessageCount = 0,
            Config = config
        };

        // Track the active SDK session object
        _activeSessions[sessionId] = session;

        // Set up event handler if dispatcher is available
        SetupEventHandler(sessionId, session);

        // Persist the session to disk (this is the only storage)
        await PersistSessionAsync(sessionId, metadata, cancellationToken);

        _logger.LogInformation("Registered session {SessionId}", sessionId);
    }

    /// <summary>
    /// Sets up the event handler for a session to dispatch events to SignalR.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The SDK session instance.</param>
    private void SetupEventHandler(string sessionId, CopilotSession session)
    {
        if (_eventDispatcher == null)
        {
            _logger.LogDebug("No event dispatcher configured, skipping event handler setup for session {SessionId}", sessionId);
            return;
        }

        // Remove any existing subscription
        if (_eventSubscriptions.TryRemove(sessionId, out var existingSubscription))
        {
            existingSubscription.Dispose();
        }

        // Create and register new event handler
        var handler = _eventDispatcher.CreateHandler(sessionId);
        var subscription = session.On(handler);
        _eventSubscriptions[sessionId] = subscription;

        _logger.LogDebug("Set up event handler for session {SessionId}", sessionId);
    }

    /// <summary>
    /// Updates session metadata when a session is resumed.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The SDK session instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateResumedSessionAsync(string sessionId, CopilotSession session, CancellationToken cancellationToken = default)
    {
        _activeSessions[sessionId] = session;

        // Update last activity in persisted data
        await UpdateLastActivityAsync(sessionId, cancellationToken);

        // Set up event handler if dispatcher is available
        SetupEventHandler(sessionId, session);

        _logger.LogInformation("Updated resumed session {SessionId}", sessionId);
    }

    /// <summary>
    /// Gets an active session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to find.</param>
    /// <returns>The session if found, otherwise null.</returns>
    public CopilotSession? GetSession(string sessionId)
    {
        _activeSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Gets session metadata by ID from persistence.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The metadata if found, otherwise null.</returns>
    public async Task<Models.Domain.SessionMetadata?> GetMetadataAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            _logger.LogWarning("No persistence service available, cannot get metadata for session {SessionId}", sessionId);
            return null;
        }

        var sessionData = await _persistenceService.LoadSessionAsync(sessionId, cancellationToken);
        if (sessionData == null)
        {
            return null;
        }

        return ConvertToMetadata(sessionData);
    }

    /// <summary>
    /// Gets all session metadata from persistence.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all session metadata.</returns>
    public async Task<List<Models.Domain.SessionMetadata>> GetAllMetadataAsync(CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            _logger.LogWarning("No persistence service available, cannot get all metadata");
            return new List<Models.Domain.SessionMetadata>();
        }

        var persistedSessions = await _persistenceService.LoadAllSessionsAsync(cancellationToken);
        return persistedSessions.Select(ConvertToMetadata).ToList();
    }

    /// <summary>
    /// Removes a session from tracking and persistence.
    /// </summary>
    /// <param name="sessionId">The session ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was removed, false if it wasn't found.</returns>
    public async Task<bool> RemoveSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // Clean up event subscription
        if (_eventSubscriptions.TryRemove(sessionId, out var subscription))
        {
            subscription.Dispose();
            _logger.LogDebug("Disposed event subscription for session {SessionId}", sessionId);
        }

        // Remove from active sessions
        _activeSessions.TryRemove(sessionId, out _);

        // Delete the persisted session file (this is the source of truth)
        var deleted = await DeletePersistedSessionAsync(sessionId, cancellationToken);
        
        if (deleted)
        {
            _logger.LogInformation("Removed session {SessionId}", sessionId);
        }

        return deleted;
    }

    /// <summary>
    /// Updates the last activity time for a session in persistence.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateLastActivityAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null) return;

        try
        {
            var sessionData = await _persistenceService.LoadSessionAsync(sessionId, cancellationToken);
            if (sessionData != null)
            {
                sessionData.LastActivityAt = DateTime.UtcNow;
                await _persistenceService.SaveSessionAsync(sessionData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update last activity for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Increments the message count for a session in persistence.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task IncrementMessageCountAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null) return;

        try
        {
            var sessionData = await _persistenceService.LoadSessionAsync(sessionId, cancellationToken);
            if (sessionData != null)
            {
                sessionData.MessageCount++;
                sessionData.LastActivityAt = DateTime.UtcNow;
                await _persistenceService.SaveSessionAsync(sessionData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment message count for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Updates the summary for a session in persistence.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="summary">The new summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateSummaryAsync(string sessionId, string summary, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null) return;

        try
        {
            var sessionData = await _persistenceService.LoadSessionAsync(sessionId, cancellationToken);
            if (sessionData != null)
            {
                sessionData.Summary = summary;
                await _persistenceService.SaveSessionAsync(sessionData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update summary for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Checks if a session is currently active (has a running SDK session object).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if the session has an active SDK session, false otherwise.</returns>
    public bool IsSessionActive(string sessionId)
    {
        return _activeSessions.ContainsKey(sessionId);
    }

    /// <summary>
    /// Checks if a session exists in persistence.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if the session exists in persistence, false otherwise.</returns>
    public bool SessionExistsInPersistence(string sessionId)
    {
        return _persistenceService?.SessionExists(sessionId) ?? false;
    }

    /// <summary>
    /// Gets the count of active session objects.
    /// </summary>
    public int ActiveSessionCount => _activeSessions.Count;

    /// <summary>
    /// Clears all active sessions (runtime objects only).
    /// Does not clear persisted session data.
    /// </summary>
    public void ClearActiveSessions()
    {
        // Dispose all event subscriptions
        foreach (var subscription in _eventSubscriptions.Values)
        {
            subscription.Dispose();
        }
        _eventSubscriptions.Clear();

        _activeSessions.Clear();
        _logger.LogInformation("Cleared all active session objects");
    }

    /// <summary>
    /// Syncs SDK session list with persisted data.
    /// Persists sessions from SDK that don't exist in persistence.
    /// </summary>
    /// <param name="sdkSessions">List of sessions from the SDK.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SyncFromSdkSessionListAsync(List<SdkSessionMetadata> sdkSessions, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null) return;

        foreach (var sdkSession in sdkSessions)
        {
            if (!_persistenceService.SessionExists(sdkSession.SessionId))
            {
                var sessionData = new PersistedSessionData
                {
                    SessionId = sdkSession.SessionId,
                    CreatedAt = sdkSession.StartTime,
                    LastActivityAt = sdkSession.ModifiedTime,
                    Summary = sdkSession.Summary,
                    IsRemote = sdkSession.IsRemote,
                    MessageCount = 0,
                    Messages = new List<PersistedMessage>()
                };
                await _persistenceService.SaveSessionAsync(sessionData, cancellationToken);
                _logger.LogDebug("Persisted session {SessionId} from SDK sync", sdkSession.SessionId);
            }
        }
    }

    #region Persistence Methods

    /// <summary>
    /// Persists a session to disk.
    /// </summary>
    public async Task PersistSessionAsync(string sessionId, Models.Domain.SessionMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            _logger.LogWarning("No persistence service available, cannot persist session {SessionId}", sessionId);
            return;
        }

        try
        {
            var sessionData = ConvertToPersistedData(metadata);
            await _persistenceService.SaveSessionAsync(sessionData, cancellationToken);
            _logger.LogDebug("Persisted session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Appends messages to a session's persisted data.
    /// </summary>
    public async Task AppendMessagesAsync(string sessionId, IEnumerable<PersistedMessage> messages, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            return;
        }

        try
        {
            await _persistenceService.AppendMessagesAsync(sessionId, messages, cancellationToken);
            _logger.LogDebug("Appended messages to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append messages to session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Gets all persisted messages for a session.
    /// </summary>
    public async Task<List<PersistedMessage>> GetPersistedMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            return new List<PersistedMessage>();
        }

        return await _persistenceService.GetMessagesAsync(sessionId, cancellationToken);
    }

    private async Task<bool> DeletePersistedSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            return false;
        }

        try
        {
            var deleted = await _persistenceService.DeleteSessionAsync(sessionId, cancellationToken);
            if (deleted)
            {
                _logger.LogDebug("Deleted persisted session {SessionId}", sessionId);
            }
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete persisted session {SessionId}", sessionId);
            return false;
        }
    }

    private static Models.Domain.SessionMetadata ConvertToMetadata(PersistedSessionData sessionData)
    {
        return new Models.Domain.SessionMetadata
        {
            SessionId = sessionData.SessionId,
            CreatedAt = sessionData.CreatedAt,
            LastActivityAt = sessionData.LastActivityAt,
            MessageCount = sessionData.MessageCount,
            Summary = sessionData.Summary,
            IsRemote = sessionData.IsRemote,
            Config = sessionData.Config != null ? ConvertToSessionConfig(sessionData.Config) : null
        };
    }

    private static Models.Domain.SessionConfig ConvertToSessionConfig(PersistedSessionConfig config)
    {
        return new Models.Domain.SessionConfig
        {
            Model = config.Model,
            Streaming = config.Streaming,
            SystemMessage = config.SystemMessage != null ? new Models.Domain.SystemMessageConfig
            {
                Mode = config.SystemMessage.Mode,
                Content = config.SystemMessage.Content
            } : null,
            AvailableTools = config.AvailableTools,
            ExcludedTools = config.ExcludedTools,
            Provider = config.Provider != null ? new Models.Domain.ProviderConfig
            {
                Type = config.Provider.Type,
                BaseUrl = config.Provider.BaseUrl,
                ApiKey = config.Provider.ApiKey,
                BearerToken = config.Provider.BearerToken,
                WireApi = config.Provider.WireApi
            } : null,
            Tools = config.Tools?.Select(t => new Models.Domain.ToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = t.Parameters?.Select(p => new Models.Domain.ToolParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Required = p.Required,
                    DefaultValue = p.DefaultValue,
                    AllowedValues = p.AllowedValues
                }).ToList()
            }).ToList()
        };
    }

    private static PersistedSessionData ConvertToPersistedData(Models.Domain.SessionMetadata metadata)
    {
        return new PersistedSessionData
        {
            SessionId = metadata.SessionId,
            CreatedAt = metadata.CreatedAt ?? DateTime.UtcNow,
            LastActivityAt = metadata.LastActivityAt,
            MessageCount = metadata.MessageCount,
            Summary = metadata.Summary,
            IsRemote = metadata.IsRemote,
            Config = metadata.Config != null ? ConvertToPersistedConfig(metadata.Config) : null,
            Messages = new List<PersistedMessage>()
        };
    }

    private static PersistedSessionConfig ConvertToPersistedConfig(Models.Domain.SessionConfig config)
    {
        return new PersistedSessionConfig
        {
            Model = config.Model,
            Streaming = config.Streaming,
            SystemMessage = config.SystemMessage != null ? new PersistedSystemMessageConfig
            {
                Mode = config.SystemMessage.Mode,
                Content = config.SystemMessage.Content
            } : null,
            AvailableTools = config.AvailableTools,
            ExcludedTools = config.ExcludedTools,
            Provider = config.Provider != null ? new PersistedProviderConfig
            {
                Type = config.Provider.Type,
                BaseUrl = config.Provider.BaseUrl,
                ApiKey = config.Provider.ApiKey,
                BearerToken = config.Provider.BearerToken,
                WireApi = config.Provider.WireApi
            } : null,
            Tools = config.Tools?.Select(t => new PersistedToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = t.Parameters?.Select(p => new PersistedToolParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Required = p.Required,
                    DefaultValue = p.DefaultValue,
                    AllowedValues = p.AllowedValues
                }).ToList()
            }).ToList()
        };
    }

    #endregion
}
