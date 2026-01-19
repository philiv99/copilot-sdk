using System.Text.Json;
using System.Text.Json.Serialization;
using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service implementation for persisting session data and client configuration to JSON files.
/// </summary>
public class PersistenceService : IPersistenceService
{
    private readonly ILogger<PersistenceService> _logger;
    private readonly string _dataDirectory;
    private readonly string _sessionsDirectory;
    private readonly string _clientConfigPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PersistenceService(ILogger<PersistenceService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get data directory from configuration or use default
        var configuredPath = configuration.GetValue<string>("Persistence:DataDirectory");
        _dataDirectory = string.IsNullOrWhiteSpace(configuredPath) 
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : configuredPath;

        _sessionsDirectory = Path.Combine(_dataDirectory, "sessions");
        _clientConfigPath = Path.Combine(_dataDirectory, "client-config.json");

        // Ensure directories exist
        EnsureDirectoriesExist();
    }

    /// <inheritdoc/>
    public string DataDirectory => _dataDirectory;

    private void EnsureDirectoriesExist()
    {
        try
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
                _logger.LogInformation("Created data directory: {DataDirectory}", _dataDirectory);
            }

            if (!Directory.Exists(_sessionsDirectory))
            {
                Directory.CreateDirectory(_sessionsDirectory);
                _logger.LogInformation("Created sessions directory: {SessionsDirectory}", _sessionsDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data directories");
            throw;
        }
    }

    #region Client Configuration

    /// <inheritdoc/>
    public async Task SaveClientConfigAsync(CopilotClientConfig config, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(_clientConfigPath, json, cancellationToken);
            _logger.LogDebug("Saved client configuration to {Path}", _clientConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save client configuration");
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<CopilotClientConfig?> LoadClientConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_clientConfigPath))
            {
                _logger.LogDebug("No client configuration file found at {Path}", _clientConfigPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_clientConfigPath, cancellationToken);
            var config = JsonSerializer.Deserialize<CopilotClientConfig>(json, JsonOptions);
            _logger.LogDebug("Loaded client configuration from {Path}", _clientConfigPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load client configuration");
            return null;
        }
    }

    #endregion

    #region Session Data

    private string GetSessionFilePath(string sessionId)
    {
        // Sanitize session ID for use as filename
        var sanitizedId = SanitizeFileName(sessionId);
        return Path.Combine(_sessionsDirectory, $"{sanitizedId}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <inheritdoc/>
    public async Task SaveSessionAsync(PersistedSessionData sessionData, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetSessionFilePath(sessionData.SessionId);
            var json = JsonSerializer.Serialize(sessionData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger.LogDebug("Saved session {SessionId} to {Path}", sessionData.SessionId, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session {SessionId}", sessionData.SessionId);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PersistedSessionData?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetSessionFilePath(sessionId);
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No session file found for {SessionId}", sessionId);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var sessionData = JsonSerializer.Deserialize<PersistedSessionData>(json, JsonOptions);
            _logger.LogDebug("Loaded session {SessionId} from {Path}", sessionId, filePath);
            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session {SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PersistedSessionData>> LoadAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<PersistedSessionData>();

        try
        {
            if (!Directory.Exists(_sessionsDirectory))
            {
                return sessions;
            }

            var sessionFiles = Directory.GetFiles(_sessionsDirectory, "*.json");
            _logger.LogDebug("Found {Count} session files", sessionFiles.Length);

            foreach (var filePath in sessionFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var sessionData = JsonSerializer.Deserialize<PersistedSessionData>(json, JsonOptions);
                    if (sessionData != null)
                    {
                        sessions.Add(sessionData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load session from {Path}", filePath);
                }
            }

            _logger.LogInformation("Loaded {Count} sessions from disk", sessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions");
        }

        return sessions;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetSessionFilePath(sessionId);
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Session file not found for deletion: {SessionId}", sessionId);
                return false;
            }

            File.Delete(filePath);
            _logger.LogInformation("Deleted session file: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session {SessionId}", sessionId);
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public bool SessionExists(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        return File.Exists(filePath);
    }

    /// <inheritdoc/>
    public List<string> GetPersistedSessionIds()
    {
        var sessionIds = new List<string>();

        try
        {
            if (!Directory.Exists(_sessionsDirectory))
            {
                return sessionIds;
            }

            var sessionFiles = Directory.GetFiles(_sessionsDirectory, "*.json");
            foreach (var filePath in sessionFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                sessionIds.Add(fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get persisted session IDs");
        }

        return sessionIds;
    }

    #endregion

    #region Message Persistence

    /// <inheritdoc/>
    public async Task AppendMessagesAsync(string sessionId, IEnumerable<PersistedMessage> messages, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var sessionData = await LoadSessionInternalAsync(sessionId, cancellationToken);
            if (sessionData == null)
            {
                _logger.LogWarning("Cannot append messages to non-existent session {SessionId}", sessionId);
                return;
            }

            sessionData.Messages.AddRange(messages);
            sessionData.MessageCount = sessionData.Messages.Count;
            sessionData.LastActivityAt = DateTime.UtcNow;

            var filePath = GetSessionFilePath(sessionId);
            var json = JsonSerializer.Serialize(sessionData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogDebug("Appended {Count} messages to session {SessionId}", messages.Count(), sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append messages to session {SessionId}", sessionId);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<List<PersistedMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionData = await LoadSessionAsync(sessionId, cancellationToken);
            return sessionData?.Messages ?? new List<PersistedMessage>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for session {SessionId}", sessionId);
            return new List<PersistedMessage>();
        }
    }

    /// <summary>
    /// Internal load method that doesn't acquire the lock (for use within locked sections).
    /// </summary>
    private async Task<PersistedSessionData?> LoadSessionInternalAsync(string sessionId, CancellationToken cancellationToken)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<PersistedSessionData>(json, JsonOptions);
    }

    #endregion
}
