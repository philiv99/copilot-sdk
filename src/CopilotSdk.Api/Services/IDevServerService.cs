namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for managing development server processes for session apps.
/// </summary>
public interface IDevServerService
{
    /// <summary>
    /// Starts the Vite dev server for a session's app.
    /// Waits until the server prints its localhost URL (proving it is ready),
    /// then returns success + PID + port + URL.
    /// The --open flag causes Vite to auto-open the browser.
    /// </summary>
    Task<(bool success, int pid, int port, string url, string message)> StartDevServerAsync(string sessionId, string appPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the dev server for a session, killing the process identified by PID.
    /// </summary>
    Task<(bool stopped, string message)> StopDevServerAsync(string sessionId, int pid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the dev server for a session (without explicit PID).
    /// </summary>
    Task<(bool stopped, string message)> StopDevServerAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if dev server is running for a session.
    /// </summary>
    Task<(bool isRunning, int? pid, int? port)> GetDevServerStatusAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the URL for accessing the dev server.
    /// </summary>
    string GetDevServerUrl(int port);
}
