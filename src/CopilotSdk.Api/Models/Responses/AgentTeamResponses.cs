using CopilotSdk.Api.Models.Domain;

namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response containing a list of available agents.
/// </summary>
public class AgentListResponse
{
    /// <summary>
    /// Available agent definitions.
    /// </summary>
    public List<AgentDefinition> Agents { get; set; } = new();
}

/// <summary>
/// Response containing agent details including prompt content.
/// </summary>
public class AgentDetailResponse
{
    /// <summary>
    /// Agent metadata.
    /// </summary>
    public AgentDefinition Agent { get; set; } = new();

    /// <summary>
    /// The agent's system prompt content (from prompt.md).
    /// </summary>
    public string PromptContent { get; set; } = string.Empty;
}

/// <summary>
/// Response containing a list of available team presets.
/// </summary>
public class TeamListResponse
{
    /// <summary>
    /// Available team preset definitions.
    /// </summary>
    public List<TeamDefinition> Teams { get; set; } = new();
}

/// <summary>
/// Response containing team details with resolved agent list.
/// </summary>
public class TeamDetailResponse
{
    /// <summary>
    /// Team definition.
    /// </summary>
    public TeamDefinition Team { get; set; } = new();

    /// <summary>
    /// Resolved agent definitions for agents in this team.
    /// </summary>
    public List<AgentDefinition> ResolvedAgents { get; set; } = new();
}

/// <summary>
/// Response from composing a team system message.
/// </summary>
public class ComposeTeamMessageResponse
{
    /// <summary>
    /// The fully composed system message content.
    /// </summary>
    public string ComposedContent { get; set; } = string.Empty;

    /// <summary>
    /// Number of agents included in the composition.
    /// </summary>
    public int AgentCount { get; set; }

    /// <summary>
    /// The workflow pattern used in composition.
    /// </summary>
    public string WorkflowPattern { get; set; } = string.Empty;
}
