namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Configuration for the Copilot client connection.
/// </summary>
public class CopilotClientConfig
{
    /// <summary>
    /// Path to the Copilot CLI executable.
    /// </summary>
    public string? CliPath { get; set; }

    /// <summary>
    /// Additional arguments to pass to the CLI.
    /// </summary>
    public string[]? CliArgs { get; set; }

    /// <summary>
    /// URL of an existing Copilot CLI server to connect to.
    /// Mutually exclusive with CliPath and UseStdio.
    /// </summary>
    public string? CliUrl { get; set; }

    /// <summary>
    /// Port number for TCP connection.
    /// </summary>
    public int Port { get; set; } = 0;

    /// <summary>
    /// Whether to use stdio for communication (default: true).
    /// </summary>
    public bool UseStdio { get; set; } = true;

    /// <summary>
    /// Log level for the CLI server.
    /// </summary>
    public string LogLevel { get; set; } = "info";

    /// <summary>
    /// Whether to automatically start the client on first operation (default: true).
    /// </summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Whether to automatically restart the client on connection failure (default: true).
    /// </summary>
    public bool AutoRestart { get; set; } = true;

    /// <summary>
    /// Working directory for the CLI process.
    /// </summary>
    public string? Cwd { get; set; }

    /// <summary>
    /// Environment variables to pass to the CLI process.
    /// </summary>
    public Dictionary<string, string>? Environment { get; set; }
}
