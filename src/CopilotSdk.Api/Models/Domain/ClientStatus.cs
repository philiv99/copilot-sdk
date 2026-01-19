namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Represents the current status of the Copilot client.
/// </summary>
public class ClientStatus
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
