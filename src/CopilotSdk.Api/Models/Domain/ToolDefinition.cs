namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Represents a custom tool definition for use in sessions.
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// Unique name of the tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of parameters the tool accepts.
    /// </summary>
    public List<ToolParameter>? Parameters { get; set; }
}
