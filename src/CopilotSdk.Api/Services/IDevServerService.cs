namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for managing development server processes for session apps.
/// </summary>
public interface IDevServerService
{
    /// <summary>
    /// Starts the Vite dev server for a session's app.
    /// </summary>
    Task<(bool success, int port, string message)> StartDevServerAsync(string sessionId, string appPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the dev server for a session.
    /// </summary>
    Task<bool> StopDevServerAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if dev server is running for a session.
    /// </summary>
    Task<(bool isRunning, int? port)> GetDevServerStatusAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the URL for accessing the dev server.
    /// </summary>
    string GetDevServerUrl(int port);
}
