using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for managing Vite dev server processes.
/// </summary>
public class DevServerService : IDevServerService, IAsyncDisposable
{
    private readonly ILogger<DevServerService> _logger;
    private readonly ConcurrentDictionary<string, (Process process, int port, string url)> _runningServers = new();
    private readonly SemaphoreSlim _portLock = new(1, 1);
    private int _nextPort = 5173; // Vite's default port

    /// <summary>
    /// Regex to extract the local URL from Vite/dev-server output.
    /// Matches patterns like:  ➜  Local:   http://localhost:5173/
    /// or plain:  http://localhost:5173
    /// </summary>
    private static readonly Regex UrlRegex = new(
        @"https?://localhost:\d+/?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DevServerService(ILogger<DevServerService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool success, int port, string url, string message)> StartDevServerAsync(
        string sessionId,
        string appPath,
        CancellationToken cancellationToken = default)
    {
        if (_runningServers.ContainsKey(sessionId))
        {
            var existing = _runningServers[sessionId];
            return (true, existing.port, existing.url, "Dev server already running");
        }

        if (!Directory.Exists(appPath))
        {
            return (false, 0, string.Empty, $"App path not found: {appPath}");
        }

        // Check if package.json exists
        var packageJsonPath = Path.Combine(appPath, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            return (false, 0, string.Empty, "No package.json found in app directory");
        }

        // Find available port
        await _portLock.WaitAsync(cancellationToken);
        int requestedPort;
        try
        {
            requestedPort = await FindAvailablePortAsync(_nextPort, cancellationToken);
            _nextPort = requestedPort + 1;
        }
        finally
        {
            _portLock.Release();
        }

        // Run npm install if node_modules is missing
        var nodeModulesPath = Path.Combine(appPath, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            _logger.LogInformation("node_modules not found at {Path}, running npm install...", appPath);
            try
            {
                var installInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm install",
                    WorkingDirectory = appPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var installProcess = Process.Start(installInfo);
                if (installProcess != null)
                {
                    await installProcess.WaitForExitAsync(cancellationToken);
                    if (installProcess.ExitCode != 0)
                    {
                        var stderr = await installProcess.StandardError.ReadToEndAsync(cancellationToken);
                        _logger.LogWarning("npm install exited with code {Code}: {Error}", installProcess.ExitCode, stderr);
                        return (false, 0, string.Empty, $"npm install failed: {stderr}");
                    }
                    _logger.LogInformation("npm install completed for {Path}", appPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run npm install for {Path}", appPath);
                return (false, 0, string.Empty, $"Failed to run npm install: {ex.Message}");
            }
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c npm run dev -- --port {requestedPort} --host",
                WorkingDirectory = appPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };
            
            // Use a TaskCompletionSource to wait for the actual URL from process output
            var urlTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var outputLines = new System.Collections.Generic.List<string>();

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    _logger.LogDebug("[{SessionId}] Dev server output: {Output}", sessionId, args.Data);
                    outputLines.Add(args.Data);

                    // Try to extract the localhost URL from the output
                    var match = UrlRegex.Match(args.Data);
                    if (match.Success)
                    {
                        urlTcs.TrySetResult(match.Value.TrimEnd('/'));
                    }
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    _logger.LogDebug("[{SessionId}] Dev server stderr: {Error}", sessionId, args.Data);
                    // Some tools (e.g. Vite) write the URL to stderr
                    var match = UrlRegex.Match(args.Data);
                    if (match.Success)
                    {
                        urlTcs.TrySetResult(match.Value.TrimEnd('/'));
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait up to 15 seconds for the URL to appear in output
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            string actualUrl;
            int actualPort;

            try
            {
                // Register cancellation to unblock the TCS
                timeoutCts.Token.Register(() => urlTcs.TrySetCanceled(), useSynchronizationContext: false);
                actualUrl = await urlTcs.Task;
                
                // Extract port from the actual URL
                var portMatch = Regex.Match(actualUrl, @":(\d+)");
                actualPort = portMatch.Success ? int.Parse(portMatch.Groups[1].Value) : requestedPort;

                _logger.LogInformation(
                    "Dev server for session {SessionId} reported URL: {Url} (port {Port})",
                    sessionId, actualUrl, actualPort);
            }
            catch (OperationCanceledException)
            {
                // Timed out waiting for URL — fall back to the requested port
                actualPort = requestedPort;
                actualUrl = $"http://localhost:{requestedPort}";
                _logger.LogWarning(
                    "Timed out waiting for dev server URL for session {SessionId}, using fallback {Url}",
                    sessionId, actualUrl);
            }

            _runningServers[sessionId] = (process, actualPort, actualUrl);
            _logger.LogInformation("Started dev server for session {SessionId} at {Url}", sessionId, actualUrl);

            return (true, actualPort, actualUrl, $"Dev server started at {actualUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start dev server for session {SessionId}", sessionId);
            return (false, 0, string.Empty, $"Failed to start dev server: {ex.Message}");
        }
    }

    public async Task<bool> StopDevServerAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!_runningServers.TryRemove(sessionId, out var serverInfo))
        {
            return false;
        }

        try
        {
            if (!serverInfo.process.HasExited)
            {
                serverInfo.process.Kill(true); // Kill process tree
                await serverInfo.process.WaitForExitAsync(cancellationToken);
            }

            serverInfo.process.Dispose();
            _logger.LogInformation("Stopped dev server for session {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping dev server for session {SessionId}", sessionId);
            return false;
        }
    }

    public Task<(bool isRunning, int? port)> GetDevServerStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_runningServers.TryGetValue(sessionId, out var serverInfo))
        {
            if (!serverInfo.process.HasExited)
            {
                return Task.FromResult((true, (int?)serverInfo.port));
            }

            // Process exited, clean up
            _runningServers.TryRemove(sessionId, out _);
            serverInfo.process.Dispose();
        }

        return Task.FromResult((false, (int?)null));
    }

    public string GetDevServerUrl(int port)
    {
        return $"http://localhost:{port}";
    }

    private static async Task<int> FindAvailablePortAsync(int startPort, CancellationToken cancellationToken)
    {
        for (int port = startPort; port < startPort + 100; port++)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port, cancellationToken);
                // Port is in use, continue
            }
            catch (SocketException)
            {
                // Port is available
                return port;
            }
        }

        throw new InvalidOperationException("No available ports found");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kvp in _runningServers)
        {
            try
            {
                await StopDevServerAsync(kvp.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing dev server for session {SessionId}", kvp.Key);
            }
        }

        _portLock.Dispose();
    }
}
