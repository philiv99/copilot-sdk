using System.Collections.Concurrent;
using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using SdkSessionMetadata = GitHub.Copilot.SDK.SessionMetadata;

namespace CopilotSdk.Api.Managers;

/// <summary>
/// Manages active sessions and their metadata.
/// Tracks session state independently of the SDK's session tracking.
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly IPersistenceService? _persistenceService;
    private readonly ConcurrentDictionary<string, Models.Domain.SessionMetadata> _sessionMetadata = new();
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
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The SDK session instance.</param>
    /// <param name="config">The configuration used to create the session.</param>
    public void RegisterSession(string sessionId, CopilotSession session, Models.Domain.SessionConfig config)
    {
        var metadata = new Models.Domain.SessionMetadata
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            MessageCount = 0,
            Config = config
        };

        if (!_sessionMetadata.TryAdd(sessionId, metadata))
        {
            _logger.LogWarning("Session {SessionId} metadata already exists, updating", sessionId);
            _sessionMetadata[sessionId] = metadata;
        }

        _activeSessions[sessionId] = session;

        // Set up event handler if dispatcher is available
        SetupEventHandler(sessionId, session);

        // Persist the session asynchronously
        _ = PersistSessionAsync(sessionId, metadata);

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
    public void UpdateResumedSession(string sessionId, CopilotSession session)
    {
        _activeSessions[sessionId] = session;

        if (_sessionMetadata.TryGetValue(sessionId, out var metadata))
        {
            metadata.LastActivityAt = DateTime.UtcNow;
        }

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
    /// Gets session metadata by ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The metadata if found, otherwise null.</returns>
    public Models.Domain.SessionMetadata? GetMetadata(string sessionId)
    {
        _sessionMetadata.TryGetValue(sessionId, out var metadata);
        return metadata;
    }

    /// <summary>
    /// Gets all tracked session metadata.
    /// </summary>
    /// <returns>List of all session metadata.</returns>
    public List<Models.Domain.SessionMetadata> GetAllMetadata()
    {
        return _sessionMetadata.Values.ToList();
    }

    /// <summary>
    /// Removes a session from tracking.
    /// </summary>
    /// <param name="sessionId">The session ID to remove.</param>
    /// <returns>True if the session was removed, false if it wasn't found.</returns>
    public bool RemoveSession(string sessionId)
    {
        // Clean up event subscription
        if (_eventSubscriptions.TryRemove(sessionId, out var subscription))
        {
            subscription.Dispose();
            _logger.LogDebug("Disposed event subscription for session {SessionId}", sessionId);
        }

        var removed = _sessionMetadata.TryRemove(sessionId, out _);
        _activeSessions.TryRemove(sessionId, out _);

        if (removed)
        {
            // Delete the persisted session file
            _ = DeletePersistedSessionAsync(sessionId);
            _logger.LogInformation("Removed session {SessionId}", sessionId);
        }

        return removed;
    }

    /// <summary>
    /// Updates the last activity time for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    public void UpdateLastActivity(string sessionId)
    {
        if (_sessionMetadata.TryGetValue(sessionId, out var metadata))
        {
            metadata.LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Increments the message count for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    public void IncrementMessageCount(string sessionId)
    {
        if (_sessionMetadata.TryGetValue(sessionId, out var metadata))
        {
            metadata.MessageCount++;
            metadata.LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Updates the summary for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="summary">The new summary.</param>
    public void UpdateSummary(string sessionId, string summary)
    {
        if (_sessionMetadata.TryGetValue(sessionId, out var metadata))
        {
            metadata.Summary = summary;
        }
    }

    /// <summary>
    /// Checks if a session exists in tracking.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if the session exists, false otherwise.</returns>
    public bool SessionExists(string sessionId)
    {
        return _activeSessions.ContainsKey(sessionId);
    }

    /// <summary>
    /// Gets the count of tracked sessions.
    /// </summary>
    public int SessionCount => _activeSessions.Count;

    /// <summary>
    /// Clears all tracked sessions.
    /// </summary>
    public void ClearAll()
    {
        // Dispose all event subscriptions
        foreach (var subscription in _eventSubscriptions.Values)
        {
            subscription.Dispose();
        }
        _eventSubscriptions.Clear();

        _sessionMetadata.Clear();
        _activeSessions.Clear();
        _logger.LogInformation("Cleared all session tracking data");
    }

    /// <summary>
    /// Syncs local tracking with SDK session list.
    /// Adds metadata for sessions that exist in SDK but not locally tracked.
    /// </summary>
    /// <param name="sdkSessions">List of sessions from the SDK.</param>
    public void SyncFromSdkSessionList(List<SdkSessionMetadata> sdkSessions)
    {
        foreach (var sdkSession in sdkSessions)
        {
            if (!_sessionMetadata.ContainsKey(sdkSession.SessionId))
            {
                var metadata = new Models.Domain.SessionMetadata
                {
                    SessionId = sdkSession.SessionId,
                    CreatedAt = sdkSession.StartTime,
                    LastActivityAt = sdkSession.ModifiedTime,
                    Summary = sdkSession.Summary,
                    IsRemote = sdkSession.IsRemote,
                    MessageCount = 0
                };
                _sessionMetadata.TryAdd(sdkSession.SessionId, metadata);
                _logger.LogDebug("Added session {SessionId} from SDK sync", sdkSession.SessionId);
            }
        }
    }

    #region Persistence Methods

    /// <summary>
    /// Loads all persisted sessions from disk into memory.
    /// </summary>
    public async Task LoadPersistedSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            _logger.LogDebug("No persistence service available, skipping session load");
            return;
        }

        try
        {
            var persistedSessions = await _persistenceService.LoadAllSessionsAsync(cancellationToken);
            foreach (var sessionData in persistedSessions)
            {
                var metadata = ConvertToMetadata(sessionData);
                _sessionMetadata.TryAdd(sessionData.SessionId, metadata);
            }
            _logger.LogInformation("Loaded {Count} persisted sessions", persistedSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted sessions");
        }
    }

    /// <summary>
    /// Persists a session to disk.
    /// </summary>
    public async Task PersistSessionAsync(string sessionId, Models.Domain.SessionMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            return;
        }

        try
        {
            metadata ??= GetMetadata(sessionId);
            if (metadata == null)
            {
                _logger.LogWarning("Cannot persist session {SessionId}: metadata not found", sessionId);
                return;
            }

            var sessionData = await ConvertToPersistedDataAsync(sessionId, metadata, cancellationToken);
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

    private async Task DeletePersistedSessionAsync(string sessionId)
    {
        if (_persistenceService == null)
        {
            return;
        }

        try
        {
            await _persistenceService.DeleteSessionAsync(sessionId);
            _logger.LogDebug("Deleted persisted session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete persisted session {SessionId}", sessionId);
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

    private async Task<PersistedSessionData> ConvertToPersistedDataAsync(string sessionId, Models.Domain.SessionMetadata metadata, CancellationToken cancellationToken)
    {
        // Load existing messages if any
        var existingMessages = _persistenceService != null 
            ? await _persistenceService.GetMessagesAsync(sessionId, cancellationToken) 
            : new List<PersistedMessage>();

        return new PersistedSessionData
        {
            SessionId = sessionId,
            CreatedAt = metadata.CreatedAt ?? DateTime.UtcNow,
            LastActivityAt = metadata.LastActivityAt,
            MessageCount = metadata.MessageCount,
            Summary = metadata.Summary,
            IsRemote = metadata.IsRemote,
            Config = metadata.Config != null ? ConvertToPersistedConfig(metadata.Config) : null,
            Messages = existingMessages
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
