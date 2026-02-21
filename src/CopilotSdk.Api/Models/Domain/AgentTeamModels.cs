namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Represents an agent definition loaded from docs/agents/{id}/agent.json.
/// </summary>
public class AgentDefinition
{
    /// <summary>
    /// Unique identifier for the agent (folder name).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the agent.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role identifier (e.g., "orchestrator", "coder", "frontend").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the agent's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Emoji icon for display.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Tags for categorization/filtering.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Category: "traditional" or "specialist".
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Represents a team preset loaded from docs/teams/{id}.json.
/// </summary>
public class TeamDefinition
{
    /// <summary>
    /// Unique identifier for the team (derived from filename).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the team.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the team's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Emoji icon for display.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// List of agent IDs in this team.
    /// </summary>
    public List<string> Agents { get; set; } = new();

    /// <summary>
    /// Workflow pattern: "sequential", "parallel", or "hub-spoke".
    /// Informational — included in the composed system message.
    /// </summary>
    public string WorkflowPattern { get; set; } = "sequential";

    /// <summary>
    /// Human-readable description of the workflow.
    /// </summary>
    public string WorkflowDescription { get; set; } = string.Empty;
}
