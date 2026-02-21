using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for discovering agents, teams, and composing team system messages.
/// </summary>
[ApiController]
[Route("api/copilot")]
[Produces("application/json")]
public class AgentTeamController : ControllerBase
{
    private readonly IAgentTeamService _agentTeamService;
    private readonly ILogger<AgentTeamController> _logger;

    public AgentTeamController(
        IAgentTeamService agentTeamService,
        ILogger<AgentTeamController> logger)
    {
        _agentTeamService = agentTeamService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available agent definitions.
    /// </summary>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(AgentListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentListResponse>> GetAgents(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting list of agents");
        var agents = await _agentTeamService.GetAgentsAsync(cancellationToken);
        return Ok(new AgentListResponse { Agents = agents });
    }

    /// <summary>
    /// Gets a specific agent's details including prompt content.
    /// </summary>
    [HttpGet("agents/{agentId}")]
    [ProducesResponseType(typeof(AgentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentDetailResponse>> GetAgentDetail(string agentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting agent detail for: {AgentId}", agentId);
        var detail = await _agentTeamService.GetAgentDetailAsync(agentId, cancellationToken);
        if (detail == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Agent not found",
                Detail = $"No agent found with ID '{agentId}'",
                Status = StatusCodes.Status404NotFound
            });
        }
        return Ok(detail);
    }

    /// <summary>
    /// Gets all available team preset definitions.
    /// </summary>
    [HttpGet("teams")]
    [ProducesResponseType(typeof(TeamListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamListResponse>> GetTeams(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting list of teams");
        var teams = await _agentTeamService.GetTeamsAsync(cancellationToken);
        return Ok(new TeamListResponse { Teams = teams });
    }

    /// <summary>
    /// Gets a specific team's details with resolved agent definitions.
    /// </summary>
    [HttpGet("teams/{teamId}")]
    [ProducesResponseType(typeof(TeamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDetailResponse>> GetTeamDetail(string teamId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting team detail for: {TeamId}", teamId);
        var detail = await _agentTeamService.GetTeamDetailAsync(teamId, cancellationToken);
        if (detail == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Team not found",
                Detail = $"No team found with ID '{teamId}'",
                Status = StatusCodes.Status404NotFound
            });
        }
        return Ok(detail);
    }

    /// <summary>
    /// Composes a system message from agent selections, optional template, and custom content.
    /// </summary>
    [HttpPost("teams/compose")]
    [ProducesResponseType(typeof(ComposeTeamMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComposeTeamMessageResponse>> ComposeTeamMessage(
        [FromBody] ComposeTeamMessageRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Composing team message with {AgentCount} agents, pattern: {Pattern}",
            request.AgentIds.Count, request.WorkflowPattern);

        if (request.AgentIds.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No agents specified",
                Detail = "At least one agent ID must be provided for composition.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await _agentTeamService.ComposeTeamSystemMessageAsync(request, cancellationToken);
        return Ok(result);
    }
}
