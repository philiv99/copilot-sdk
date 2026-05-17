using CopilotSdk.Api.Models.Domain;
using SdkModelInfo = GitHub.Copilot.SDK.ModelInfo;

namespace CopilotSdk.Api.Managers;

/// <summary>
/// Interface for managing the CopilotClient lifecycle.
/// Provides the minimal interface needed for hosted service startup/shutdown.
/// </summary>
public interface ICopilotClientManager
{
    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    CopilotClientConfig Config { get; }

    /// <summary>
    /// Gets the current client status.
    /// </summary>
    ClientStatus Status { get; }

    /// <summary>
    /// Starts the Copilot client with the current configuration.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Copilot client gracefully.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Lists models available to the connected Copilot client.
    /// </summary>
    Task<List<SdkModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);
}
