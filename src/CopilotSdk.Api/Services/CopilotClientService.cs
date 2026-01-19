using System.Diagnostics;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service implementation for Copilot client operations.
/// </summary>
public class CopilotClientService : ICopilotClientService
{
    private readonly CopilotClientManager _clientManager;
    private readonly ILogger<CopilotClientService> _logger;

    public CopilotClientService(CopilotClientManager clientManager, ILogger<CopilotClientService> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ClientStatusResponse GetStatus()
    {
        var status = _clientManager.Status;
        return new ClientStatusResponse
        {
            ConnectionState = status.ConnectionState,
            IsConnected = status.IsConnected,
            ConnectedAt = status.ConnectedAt,
            Error = status.Error
        };
    }

    /// <inheritdoc/>
    public ClientConfigResponse GetConfig()
    {
        var config = _clientManager.Config;
        return new ClientConfigResponse
        {
            CliPath = config.CliPath,
            CliArgs = config.CliArgs,
            CliUrl = config.CliUrl,
            Port = config.Port,
            UseStdio = config.UseStdio,
            LogLevel = config.LogLevel,
            AutoStart = config.AutoStart,
            AutoRestart = config.AutoRestart,
            Cwd = config.Cwd,
            Environment = config.Environment
        };
    }

    /// <inheritdoc/>
    public void UpdateConfig(UpdateClientConfigRequest request)
    {
        var currentConfig = _clientManager.Config;

        var newConfig = new CopilotClientConfig
        {
            CliPath = request.CliPath ?? currentConfig.CliPath,
            CliArgs = request.CliArgs ?? currentConfig.CliArgs,
            CliUrl = request.CliUrl ?? currentConfig.CliUrl,
            Port = request.Port ?? currentConfig.Port,
            UseStdio = request.UseStdio ?? currentConfig.UseStdio,
            LogLevel = request.LogLevel ?? currentConfig.LogLevel,
            AutoStart = request.AutoStart ?? currentConfig.AutoStart,
            AutoRestart = request.AutoRestart ?? currentConfig.AutoRestart,
            Cwd = request.Cwd ?? currentConfig.Cwd,
            Environment = request.Environment ?? currentConfig.Environment
        };

        _clientManager.UpdateConfig(newConfig);
        _logger.LogInformation("Client configuration updated via service");
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Copilot client via service");
        await _clientManager.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping Copilot client via service");
        await _clientManager.StopAsync();
    }

    /// <inheritdoc/>
    public async Task ForceStopAsync()
    {
        _logger.LogInformation("Force stopping Copilot client via service");
        await _clientManager.ForceStopAsync();
    }

    /// <inheritdoc/>
    public async Task<PingResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sdkResponse = await _clientManager.PingAsync(request.Message, cancellationToken);
        stopwatch.Stop();

        return new PingResponse
        {
            Message = sdkResponse.Message,
            Timestamp = sdkResponse.Timestamp,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            ProtocolVersion = sdkResponse.ProtocolVersion
        };
    }
}
