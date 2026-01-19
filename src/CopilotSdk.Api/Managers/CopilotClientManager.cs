using System.Diagnostics;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using SdkConnectionState = GitHub.Copilot.SDK.ConnectionState;
using SdkSessionConfig = GitHub.Copilot.SDK.SessionConfig;
using SdkResumeSessionConfig = GitHub.Copilot.SDK.ResumeSessionConfig;
using SdkSystemMessageConfig = GitHub.Copilot.SDK.SystemMessageConfig;
using SdkSystemMessageMode = GitHub.Copilot.SDK.SystemMessageMode;
using SdkProviderConfig = GitHub.Copilot.SDK.ProviderConfig;
using SdkSessionMetadata = GitHub.Copilot.SDK.SessionMetadata;

namespace CopilotSdk.Api.Managers;

/// <summary>
/// Singleton manager for the CopilotClient lifecycle.
/// Manages a single CopilotClient instance and tracks its state.
/// </summary>
public class CopilotClientManager : ICopilotClientManager, IAsyncDisposable
{
    private readonly ILogger<CopilotClientManager> _logger;
    private readonly IPersistenceService? _persistenceService;
    private readonly object _lock = new();
    private CopilotClient? _client;
    private CopilotClientConfig _config = new();
    private DateTime? _connectedAt;
    private string? _lastError;

    public CopilotClientManager(ILogger<CopilotClientManager> logger, IPersistenceService? persistenceService = null)
    {
        _logger = logger;
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public CopilotClientConfig Config
    {
        get
        {
            lock (_lock)
            {
                return _config;
            }
        }
    }

    /// <summary>
    /// Gets the current client status.
    /// </summary>
    public ClientStatus Status
    {
        get
        {
            lock (_lock)
            {
                var state = _client?.State ?? SdkConnectionState.Disconnected;
                return new ClientStatus
                {
                    ConnectionState = state.ToString(),
                    IsConnected = state == SdkConnectionState.Connected,
                    ConnectedAt = _connectedAt,
                    Error = _lastError
                };
            }
        }
    }

    /// <summary>
    /// Gets the underlying CopilotClient instance, if available.
    /// </summary>
    public CopilotClient? Client
    {
        get
        {
            lock (_lock)
            {
                return _client;
            }
        }
    }

    /// <summary>
    /// Updates the client configuration. Client must be stopped to update config.
    /// </summary>
    public void UpdateConfig(CopilotClientConfig config)
    {
        lock (_lock)
        {
            if (_client != null && _client.State != SdkConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Cannot update configuration while client is running. Stop the client first.");
            }
            _config = config;
            _logger.LogInformation("Client configuration updated");
        }

        // Persist the configuration asynchronously
        _ = PersistConfigAsync();
    }

    /// <summary>
    /// Updates the client configuration and persists it. Client must be stopped to update config.
    /// </summary>
    public async Task UpdateConfigAsync(CopilotClientConfig config, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_client != null && _client.State != SdkConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Cannot update configuration while client is running. Stop the client first.");
            }
            _config = config;
            _logger.LogInformation("Client configuration updated");
        }

        await PersistConfigAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the persisted client configuration.
    /// </summary>
    public async Task LoadPersistedConfigAsync(CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            _logger.LogDebug("No persistence service available, skipping config load");
            return;
        }

        try
        {
            var persistedConfig = await _persistenceService.LoadClientConfigAsync(cancellationToken);
            if (persistedConfig != null)
            {
                lock (_lock)
                {
                    _config = persistedConfig;
                }
                _logger.LogInformation("Loaded persisted client configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted client configuration");
        }
    }

    private async Task PersistConfigAsync(CancellationToken cancellationToken = default)
    {
        if (_persistenceService == null)
        {
            return;
        }

        try
        {
            CopilotClientConfig configCopy;
            lock (_lock)
            {
                // Create a copy to avoid locking during async I/O
                configCopy = new CopilotClientConfig
                {
                    CliPath = _config.CliPath,
                    CliArgs = _config.CliArgs,
                    CliUrl = _config.CliUrl,
                    Port = _config.Port,
                    UseStdio = _config.UseStdio,
                    LogLevel = _config.LogLevel,
                    AutoStart = _config.AutoStart,
                    AutoRestart = _config.AutoRestart,
                    Cwd = _config.Cwd,
                    Environment = _config.Environment?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
            }

            await _persistenceService.SaveClientConfigAsync(configCopy, cancellationToken);
            _logger.LogDebug("Client configuration persisted");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist client configuration");
        }
    }

    /// <summary>
    /// Starts the Copilot client with the current configuration.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_client != null && _client.State == SdkConnectionState.Connected)
            {
                _logger.LogWarning("Client is already connected");
                return;
            }

            // Create new client with current config
            var options = BuildClientOptions();
            _client = new CopilotClient(options);
            _lastError = null;
        }

        try
        {
            _logger.LogInformation("Starting Copilot client...");
            await _client!.StartAsync(cancellationToken);

            lock (_lock)
            {
                _connectedAt = DateTime.UtcNow;
            }

            _logger.LogInformation("Copilot client started successfully");
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _lastError = ex.Message;
                _connectedAt = null;
            }
            _logger.LogError(ex, "Failed to start Copilot client");
            throw;
        }
    }

    /// <summary>
    /// Stops the Copilot client gracefully.
    /// </summary>
    public async Task StopAsync()
    {
        CopilotClient? clientToStop;

        lock (_lock)
        {
            clientToStop = _client;
            if (clientToStop == null)
            {
                _logger.LogWarning("Client is not running");
                return;
            }
        }

        try
        {
            _logger.LogInformation("Stopping Copilot client...");
            await clientToStop.StopAsync();
            _logger.LogInformation("Copilot client stopped successfully");
        }
        finally
        {
            lock (_lock)
            {
                _connectedAt = null;
                _client = null;
            }
        }
    }

    /// <summary>
    /// Forces an immediate stop of the client without graceful cleanup.
    /// </summary>
    public async Task ForceStopAsync()
    {
        CopilotClient? clientToStop;

        lock (_lock)
        {
            clientToStop = _client;
            if (clientToStop == null)
            {
                _logger.LogWarning("Client is not running");
                return;
            }
        }

        try
        {
            _logger.LogInformation("Force stopping Copilot client...");
            await clientToStop.ForceStopAsync();
            _logger.LogInformation("Copilot client force stopped");
        }
        finally
        {
            lock (_lock)
            {
                _connectedAt = null;
                _client = null;
            }
        }
    }

    /// <summary>
    /// Pings the Copilot server to check connectivity.
    /// </summary>
    public async Task<GitHub.Copilot.SDK.PingResponse> PingAsync(string? message = null, CancellationToken cancellationToken = default)
    {
        CopilotClient? client;

        lock (_lock)
        {
            client = _client;
        }

        if (client == null || client.State != SdkConnectionState.Connected)
        {
            throw new InvalidOperationException("Client is not connected. Start the client first.");
        }

        return await client.PingAsync(message, cancellationToken);
    }

    private CopilotClientOptions BuildClientOptions()
    {
        return new CopilotClientOptions
        {
            CliPath = _config.CliPath,
            CliArgs = _config.CliArgs,
            CliUrl = _config.CliUrl,
            Port = _config.Port,
            UseStdio = _config.UseStdio,
            LogLevel = _config.LogLevel,
            AutoStart = _config.AutoStart,
            AutoRestart = _config.AutoRestart,
            Cwd = _config.Cwd,
            Environment = _config.Environment
        };
    }

    #region Session Management

    /// <summary>
    /// Creates a new Copilot session with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration for the session.</param>
    /// <param name="tools">Optional AI functions for custom tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session.</returns>
    public async Task<CopilotSession> CreateSessionAsync(
        Models.Domain.SessionConfig config,
        ICollection<AIFunction>? tools = null,
        CancellationToken cancellationToken = default)
    {
        CopilotClient? client;

        lock (_lock)
        {
            client = _client;
        }

        if (client == null || client.State != SdkConnectionState.Connected)
        {
            throw new InvalidOperationException("Client is not connected. Start the client first.");
        }

        var sdkConfig = BuildSdkSessionConfig(config, tools);

        _logger.LogInformation("Creating session with model {Model}", config.Model);
        var session = await client.CreateSessionAsync(sdkConfig, cancellationToken);
        _logger.LogInformation("Created session {SessionId}", session.SessionId);

        return session;
    }

    /// <summary>
    /// Resumes an existing session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="streaming">Whether to enable streaming.</param>
    /// <param name="provider">Optional provider configuration.</param>
    /// <param name="tools">Optional AI functions for custom tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resumed session.</returns>
    public async Task<CopilotSession> ResumeSessionAsync(
        string sessionId,
        bool streaming = false,
        Models.Domain.ProviderConfig? provider = null,
        ICollection<AIFunction>? tools = null,
        CancellationToken cancellationToken = default)
    {
        CopilotClient? client;

        lock (_lock)
        {
            client = _client;
        }

        if (client == null || client.State != SdkConnectionState.Connected)
        {
            throw new InvalidOperationException("Client is not connected. Start the client first.");
        }

        var sdkConfig = new SdkResumeSessionConfig
        {
            Streaming = streaming,
            Tools = tools,
            Provider = provider != null ? BuildSdkProviderConfig(provider) : null
        };

        _logger.LogInformation("Resuming session {SessionId}", sessionId);
        var session = await client.ResumeSessionAsync(sessionId, sdkConfig, cancellationToken);
        _logger.LogInformation("Resumed session {SessionId}", session.SessionId);

        return session;
    }

    /// <summary>
    /// Lists all sessions known to the Copilot server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session metadata.</returns>
    public async Task<List<SdkSessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        CopilotClient? client;

        lock (_lock)
        {
            client = _client;
        }

        // Return empty list if client is not connected (graceful handling for frontend)
        if (client == null || client.State != SdkConnectionState.Connected)
        {
            return new List<SdkSessionMetadata>();
        }

        return await client.ListSessionsAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        CopilotClient? client;

        lock (_lock)
        {
            client = _client;
        }

        if (client == null || client.State != SdkConnectionState.Connected)
        {
            throw new InvalidOperationException("Client is not connected. Start the client first.");
        }

        _logger.LogInformation("Deleting session {SessionId}", sessionId);
        await client.DeleteSessionAsync(sessionId, cancellationToken);
        _logger.LogInformation("Deleted session {SessionId}", sessionId);
    }

    private SdkSessionConfig BuildSdkSessionConfig(Models.Domain.SessionConfig config, ICollection<AIFunction>? tools)
    {
        return new SdkSessionConfig
        {
            SessionId = config.SessionId,
            Model = config.Model,
            Streaming = config.Streaming,
            Tools = tools,
            SystemMessage = config.SystemMessage != null ? BuildSdkSystemMessageConfig(config.SystemMessage) : null,
            AvailableTools = config.AvailableTools,
            ExcludedTools = config.ExcludedTools,
            Provider = config.Provider != null ? BuildSdkProviderConfig(config.Provider) : null
        };
    }

    private SdkSystemMessageConfig BuildSdkSystemMessageConfig(Models.Domain.SystemMessageConfig config)
    {
        return new SdkSystemMessageConfig
        {
            Mode = config.Mode?.ToLower() == "replace" ? SdkSystemMessageMode.Replace : SdkSystemMessageMode.Append,
            Content = config.Content
        };
    }

    private SdkProviderConfig BuildSdkProviderConfig(Models.Domain.ProviderConfig config)
    {
        return new SdkProviderConfig
        {
            Type = config.Type,
            BaseUrl = config.BaseUrl ?? string.Empty,
            ApiKey = config.ApiKey,
            BearerToken = config.BearerToken,
            WireApi = config.WireApi
        };
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        CopilotClient? clientToDispose;

        lock (_lock)
        {
            clientToDispose = _client;
            _client = null;
        }

        if (clientToDispose != null)
        {
            await clientToDispose.DisposeAsync();
        }
    }
}
