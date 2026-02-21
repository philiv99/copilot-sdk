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
    private readonly ConcurrentDictionary<string, (Process process, int pid, int port, string url)> _runningServers = new();
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

    public async Task<(bool success, int pid, int port, string url, string message)> StartDevServerAsync(
        string sessionId,
        string appPath,
        CancellationToken cancellationToken = default)
    {
        // If already running for this session, return existing info
        if (_runningServers.TryGetValue(sessionId, out var existing))
        {
            if (!existing.process.HasExited)
            {
                return (true, existing.pid, existing.port, existing.url, "Dev server already running");
            }
            // Process died — clean up and re-start
            _runningServers.TryRemove(sessionId, out _);
            existing.process.Dispose();
        }

        if (!Directory.Exists(appPath))
        {
            return (false, 0, 0, string.Empty, $"App path not found: {appPath}");
        }

        var packageJsonPath = Path.Combine(appPath, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            return (false, 0, 0, string.Empty, "No package.json found in app directory");
        }

        // Find an available port
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
                        return (false, 0, 0, string.Empty, $"npm install failed: {stderr}");
                    }
                    _logger.LogInformation("npm install completed for {Path}", appPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run npm install for {Path}", appPath);
                return (false, 0, 0, string.Empty, $"Failed to run npm install: {ex.Message}");
            }
        }

        // Start the Vite dev server as a background process.
        // --open tells Vite to auto-open the app in the default browser once ready.
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c npx vite --port {requestedPort} --host --open",
                WorkingDirectory = appPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            // TaskCompletionSource that resolves when Vite prints its localhost URL,
            // which proves the server is up and ready (Vite only prints this after binding).
            var urlTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            process.OutputDataReceived += (_, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                _logger.LogDebug("[{SessionId}] stdout: {Line}", sessionId, args.Data);
                var match = UrlRegex.Match(args.Data);
                if (match.Success)
                    urlTcs.TrySetResult(match.Value.TrimEnd('/'));
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                _logger.LogDebug("[{SessionId}] stderr: {Line}", sessionId, args.Data);
                // Vite sometimes prints the URL to stderr
                var match = UrlRegex.Match(args.Data);
                if (match.Success)
                    urlTcs.TrySetResult(match.Value.TrimEnd('/'));
            };

            // If the process exits before printing a URL, that's a failure
            process.Exited += (_, _) =>
            {
                urlTcs.TrySetException(new InvalidOperationException(
                    $"Dev server process exited with code {(process.HasExited ? process.ExitCode : -1)} before becoming ready"));
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var pid = process.Id;
            _logger.LogInformation("Launched dev server process PID {Pid} for session {SessionId}", pid, sessionId);

            // Wait up to 30 seconds for the URL to appear in stdout/stderr
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            timeoutCts.Token.Register(() => urlTcs.TrySetCanceled(), useSynchronizationContext: false);

            string actualUrl;
            int actualPort;
            try
            {
                actualUrl = await urlTcs.Task;
                var portMatch = Regex.Match(actualUrl, @":(\d+)");
                actualPort = portMatch.Success ? int.Parse(portMatch.Groups[1].Value) : requestedPort;
            }
            catch (OperationCanceledException)
            {
                // Timed out — process started but Vite hasn't printed its URL yet.
                // Use the requested port as fallback; the --open flag will still fire.
                actualPort = requestedPort;
                actualUrl = $"http://localhost:{requestedPort}";
                _logger.LogWarning(
                    "Timed out waiting for dev server URL for session {SessionId}; using fallback {Url}",
                    sessionId, actualUrl);
            }
            catch (InvalidOperationException ex)
            {
                // Process exited before becoming ready
                process.Dispose();
                return (false, 0, 0, string.Empty, ex.Message);
            }

            _runningServers[sessionId] = (process, pid, actualPort, actualUrl);
            _logger.LogInformation(
                "Dev server for session {SessionId} is running at {Url} (PID {Pid}, port {Port})",
                sessionId, actualUrl, pid, actualPort);

            return (true, pid, actualPort, actualUrl, $"Dev server started at {actualUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start dev server for session {SessionId}", sessionId);
            return (false, 0, 0, string.Empty, $"Failed to start dev server: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the dev server for a session by killing the process identified by the given PID.
    /// Falls back to the tracked process if the PID doesn't match or is 0.
    /// </summary>
    public async Task<(bool stopped, string message)> StopDevServerAsync(
        string sessionId,
        int pid,
        CancellationToken cancellationToken = default)
    {
        // Try to find the tracked entry for this session
        if (!_runningServers.TryRemove(sessionId, out var serverInfo))
        {
            // No tracked entry — try to kill by PID directly if one was provided
            if (pid > 0)
            {
                return await KillProcessByPidAsync(pid);
            }
            return (false, "No dev server is running for this session");
        }

        // If the caller supplied a specific PID, verify it matches what we tracked
        var targetProcess = serverInfo.process;
        if (pid > 0 && serverInfo.pid != pid)
        {
            _logger.LogWarning(
                "Caller requested stop of PID {RequestedPid} but tracked PID is {TrackedPid} for session {SessionId}. Killing both.",
                pid, serverInfo.pid, sessionId);
            // Kill the tracked process
            await KillProcessAsync(targetProcess, sessionId);
            // Also try the requested PID
            await KillProcessByPidAsync(pid);
            return (true, $"Killed tracked PID {serverInfo.pid} and requested PID {pid}");
        }

        return await KillProcessAsync(targetProcess, sessionId);
    }

    /// <summary>
    /// Legacy overload for backward compatibility (tests, etc.).
    /// </summary>
    public Task<(bool stopped, string message)> StopDevServerAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return StopDevServerAsync(sessionId, 0, cancellationToken);
    }

    public Task<(bool isRunning, int? pid, int? port)> GetDevServerStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (_runningServers.TryGetValue(sessionId, out var serverInfo))
        {
            if (!serverInfo.process.HasExited)
            {
                return Task.FromResult((true, (int?)serverInfo.pid, (int?)serverInfo.port));
            }

            // Process exited — clean up
            _runningServers.TryRemove(sessionId, out _);
            serverInfo.process.Dispose();
        }

        return Task.FromResult((false, (int?)null, (int?)null));
    }

    public string GetDevServerUrl(int port)
    {
        return $"http://localhost:{port}";
    }

    // ────────────────────── private helpers ──────────────────────

    private async Task<(bool stopped, string message)> KillProcessAsync(Process process, string sessionId)
    {
        try
        {
            if (!process.HasExited)
            {
                _logger.LogInformation("Killing dev server process tree (PID {Pid}) for session {SessionId}", process.Id, sessionId);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            var exited = process.HasExited;
            process.Dispose();
            _logger.LogInformation("Dev server for session {SessionId} stopped (exited={Exited})", sessionId, exited);
            return (exited, exited ? "Process killed successfully" : "Process may still be running");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing dev server process for session {SessionId}", sessionId);
            try { process.Dispose(); } catch { /* best effort */ }
            return (false, $"Error killing process: {ex.Message}");
        }
    }

    private async Task<(bool stopped, string message)> KillProcessByPidAsync(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            _logger.LogInformation("Killing process by PID {Pid}", pid);
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            var exited = process.HasExited;
            process.Dispose();
            return (exited, exited ? $"PID {pid} killed successfully" : $"PID {pid} may still be running");
        }
        catch (ArgumentException)
        {
            // Process with this PID does not exist (already dead)
            return (true, $"PID {pid} is not running (already exited)");
        }
        catch (Exception ex)
        {
            return (false, $"Error killing PID {pid}: {ex.Message}");
        }
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
                await StopDevServerAsync(kvp.Key, kvp.Value.pid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing dev server for session {SessionId}", kvp.Key);
            }
        }

        _portLock.Dispose();
    }
}
