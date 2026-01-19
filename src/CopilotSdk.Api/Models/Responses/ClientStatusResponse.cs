namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response model for client status queries.
/// </summary>
public class ClientStatusResponse
{
    /// <summary>
    /// Current connection state (Disconnected, Connecting, Connected, Error).
    /// </summary>
    public string ConnectionState { get; set; } = "Disconnected";

    /// <summary>
    /// Whether the client is currently connected.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Timestamp when the client connected, if connected.
    /// </summary>
    public DateTime? ConnectedAt { get; set; }

    /// <summary>
    /// Error message if the client is in an error state.
    /// </summary>
    public string? Error { get; set; }
}
