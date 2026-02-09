using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for managing Vite dev server processes.
/// </summary>
public class DevServerService : IDevServerService, IAsyncDisposable
{
    private readonly ILogger<DevServerService> _logger;
    private readonly ConcurrentDictionary<string, (Process process, int port)> _runningServers = new();
    private readonly SemaphoreSlim _portLock = new(1, 1);
    private int _nextPort = 5173; // Vite's default port

    public DevServerService(ILogger<DevServerService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool success, int port, string message)> StartDevServerAsync(
        string sessionId,
        string appPath,
        CancellationToken cancellationToken = default)
    {
        if (_runningServers.ContainsKey(sessionId))
        {
            var existing = _runningServers[sessionId];
            return (true, existing.port, "Dev server already running");
        }

        if (!Directory.Exists(appPath))
        {
            return (false, 0, $"App path not found: {appPath}");
        }

        // Check if package.json exists
        var packageJsonPath = Path.Combine(appPath, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            return (false, 0, "No package.json found in app directory");
        }

        // Find available port
        await _portLock.WaitAsync(cancellationToken);
        int port;
        try
        {
            port = await FindAvailablePortAsync(_nextPort, cancellationToken);
            _nextPort = port + 1;
        }
        finally
        {
            _portLock.Release();
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c npm run dev -- --port {port} --host",
                WorkingDirectory = appPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    _logger.LogDebug("[{SessionId}] Dev server output: {Output}", sessionId, args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    _logger.LogWarning("[{SessionId}] Dev server error: {Error}", sessionId, args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _runningServers[sessionId] = (process, port);
            _logger.LogInformation("Started dev server for session {SessionId} on port {Port}", sessionId, port);

            // Wait a bit for server to start
            await Task.Delay(3000, cancellationToken);

            return (true, port, $"Dev server started on port {port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start dev server for session {SessionId}", sessionId);
            return (false, 0, $"Failed to start dev server: {ex.Message}");
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
