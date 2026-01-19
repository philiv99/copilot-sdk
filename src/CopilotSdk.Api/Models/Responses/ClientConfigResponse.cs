namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response model for client configuration queries.
/// </summary>
public class ClientConfigResponse
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
    /// </summary>
    public string? CliUrl { get; set; }

    /// <summary>
    /// Port number for TCP connection.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Whether to use stdio for communication.
    /// </summary>
    public bool UseStdio { get; set; }

    /// <summary>
    /// Log level for the CLI server.
    /// </summary>
    public string LogLevel { get; set; } = "info";

    /// <summary>
    /// Whether to automatically start the client on first operation.
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// Whether to automatically restart the client on connection failure.
    /// </summary>
    public bool AutoRestart { get; set; }

    /// <summary>
    /// Working directory for the CLI process.
    /// </summary>
    public string? Cwd { get; set; }

    /// <summary>
    /// Environment variables to pass to the CLI process.
    /// </summary>
    public Dictionary<string, string>? Environment { get; set; }
}
