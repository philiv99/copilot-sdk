namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request to compose a team system message from agent and template selections.
/// </summary>
public class ComposeTeamMessageRequest
{
    /// <summary>
    /// Optional system prompt template name to use as the base.
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// List of agent IDs to include in the composition.
    /// </summary>
    public List<string> AgentIds { get; set; } = new();

    /// <summary>
    /// Workflow pattern: "sequential", "parallel", or "hub-spoke".
    /// </summary>
    public string WorkflowPattern { get; set; } = "sequential";

    /// <summary>
    /// Optional custom content to append at the end.
    /// </summary>
    public string? CustomContent { get; set; }
}
