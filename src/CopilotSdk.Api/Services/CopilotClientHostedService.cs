using CopilotSdk.Api.Managers;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Hosted service that manages automatic startup and shutdown of the Copilot client.
/// </summary>
public class CopilotClientHostedService : IHostedService
{
    private readonly ICopilotClientManager _clientManager;
    private readonly ILogger<CopilotClientHostedService> _logger;

    public CopilotClientHostedService(
        ICopilotClientManager clientManager,
        ILogger<CopilotClientHostedService> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    /// <summary>
    /// Called when the application starts. Starts the Copilot client if AutoStart is enabled.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = _clientManager.Config;

        if (!config.AutoStart)
        {
            _logger.LogInformation("Copilot client AutoStart is disabled. Client will not start automatically.");
            return;
        }

        _logger.LogInformation("Copilot client AutoStart is enabled. Starting client...");

        try
        {
            await _clientManager.StartAsync(cancellationToken);
            _logger.LogInformation("Copilot client started successfully via AutoStart");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-start Copilot client. You can start it manually via the API.");
            // Don't rethrow - allow the application to continue running even if client fails to start
        }
    }

    /// <summary>
    /// Called when the application is shutting down. Stops the Copilot client gracefully.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application shutting down. Stopping Copilot client...");

        try
        {
            await _clientManager.StopAsync();
            _logger.LogInformation("Copilot client stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Copilot client during shutdown");
        }
    }
}
