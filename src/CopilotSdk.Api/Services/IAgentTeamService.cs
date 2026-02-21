using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for discovering agents and teams from the filesystem and composing team system messages.
/// </summary>
public interface IAgentTeamService
{
    /// <summary>
    /// Gets all available agent definitions.
    /// </summary>
    Task<List<AgentDefinition>> GetAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific agent's details including prompt content.
    /// </summary>
    Task<AgentDetailResponse?> GetAgentDetailAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available team preset definitions.
    /// </summary>
    Task<List<TeamDefinition>> GetTeamsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific team's details with resolved agent list.
    /// </summary>
    Task<TeamDetailResponse?> GetTeamDetailAsync(string teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Composes a system message from template, agent selections, and custom content.
    /// </summary>
    Task<ComposeTeamMessageResponse> ComposeTeamSystemMessageAsync(ComposeTeamMessageRequest request, CancellationToken cancellationToken = default);
}
