namespace CopilotSdk.Api.Tools;

/// <summary>
/// Result from the echo tool.
/// </summary>
public record EchoToolResult(string Echoed, int OriginalLength);

/// <summary>
/// Result from the get_current_time tool.
/// </summary>
public record GetCurrentTimeResult(string CurrentTime, string Timezone, string UtcOffset);
