namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response from starting a development server.
/// </summary>
public class DevServerResponse
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Port number where the server is running.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// URL to access the dev server.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Status or error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for dev server status check.
/// </summary>
public class DevServerStatusResponse
{
    /// <summary>
    /// Whether the dev server is currently running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Port number if running.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// URL to access the dev server if running.
    /// </summary>
    public string? Url { get; set; }
}
