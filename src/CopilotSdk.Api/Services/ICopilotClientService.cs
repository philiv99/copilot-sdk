using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service interface for Copilot client operations.
/// </summary>
public interface ICopilotClientService
{
    /// <summary>
    /// Gets the current client status.
    /// </summary>
    ClientStatusResponse GetStatus();

    /// <summary>
    /// Gets the current client configuration.
    /// </summary>
    ClientConfigResponse GetConfig();

    /// <summary>
    /// Updates the client configuration.
    /// </summary>
    void UpdateConfig(UpdateClientConfigRequest request);

    /// <summary>
    /// Starts the Copilot client.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Copilot client gracefully.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Forces an immediate stop of the Copilot client.
    /// </summary>
    Task ForceStopAsync();

    /// <summary>
    /// Pings the Copilot server to check connectivity.
    /// </summary>
    Task<PingResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default);
}
