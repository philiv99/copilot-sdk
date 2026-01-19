namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Represents a parameter definition for a custom tool.
/// </summary>
public class ToolParameter
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the parameter (e.g., "string", "number", "boolean", "object", "array").
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Description of what the parameter is used for.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter is required.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Default value for the parameter.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Allowed values (for enum-like parameters).
    /// </summary>
    public List<string>? AllowedValues { get; set; }
}
