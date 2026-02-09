using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request model for creating a new session.
/// </summary>
public class CreateSessionRequest : IValidatableObject
{
    /// <summary>
    /// Optional custom session ID. If not provided, one will be generated.
    /// Must contain only alphanumeric characters, hyphens, and underscores.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Validates the request model.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(SessionId))
        {
            // Session IDs must be alphanumeric with optional hyphens and underscores
            if (!Regex.IsMatch(SessionId, @"^[a-zA-Z0-9_-]+$"))
            {
                yield return new ValidationResult(
                    "Session ID can only contain letters, numbers, hyphens (-), and underscores (_). No spaces allowed.",
                    new[] { nameof(SessionId) });
            }
        }
    }

    /// <summary>
    /// Model to use for the session (e.g., "gpt-4", "gpt-3.5-turbo").
    /// </summary>
    public string Model { get; set; } = "claude-opus-4.5";

    /// <summary>
    /// Whether to enable streaming responses.
    /// </summary>
    public bool Streaming { get; set; } = false;

    /// <summary>
    /// System message configuration.
    /// </summary>
    public SystemMessageConfig? SystemMessage { get; set; }

    /// <summary>
    /// List of tool names that are available for this session.
    /// </summary>
    public List<string>? AvailableTools { get; set; }

    /// <summary>
    /// List of tool names to exclude from this session.
    /// </summary>
    public List<string>? ExcludedTools { get; set; }

    /// <summary>
    /// Custom provider configuration for BYOK scenarios.
    /// </summary>
    public ProviderConfig? Provider { get; set; }

    /// <summary>
    /// Custom tool definitions for this session.
    /// </summary>
    public List<ToolDefinition>? Tools { get; set; }

    /// <summary>
    /// Local path to the app's git repository / project directory.
    /// Used by the dev server to locate and serve the app.
    /// </summary>
    public string? AppPath { get; set; }
}
