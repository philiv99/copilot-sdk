namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response model for ping operations.
/// </summary>
public class PingResponse
{
    /// <summary>
    /// The message echoed back from the server.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Server timestamp when the ping was received.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Round-trip latency in milliseconds.
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Protocol version of the server.
    /// </summary>
    public int? ProtocolVersion { get; set; }
}
