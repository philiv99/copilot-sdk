using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for AgentTeamController.
/// </summary>
public class AgentTeamControllerTests
{
    private readonly Mock<IAgentTeamService> _mockService;
    private readonly Mock<ILogger<AgentTeamController>> _mockLogger;
    private readonly AgentTeamController _controller;

    public AgentTeamControllerTests()
    {
        _mockService = new Mock<IAgentTeamService>();
        _mockLogger = new Mock<ILogger<AgentTeamController>>();
        _controller = new AgentTeamController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAgents

    [Fact]
    public async Task GetAgents_ReturnsAgentList()
    {
        var agents = new List<AgentDefinition>
        {
            new() { Id = "coder", Name = "Coder", Role = "coder", Category = "traditional" },
            new() { Id = "tester", Name = "Tester", Role = "tester", Category = "traditional" }
        };
        _mockService.Setup(s => s.GetAgentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(agents);

        var result = await _controller.GetAgents(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AgentListResponse>(okResult.Value);
        Assert.Equal(2, response.Agents.Count);
    }

    [Fact]
    public async Task GetAgents_ReturnsEmptyList_WhenNoAgents()
    {
        _mockService.Setup(s => s.GetAgentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentDefinition>());

        var result = await _controller.GetAgents(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AgentListResponse>(okResult.Value);
        Assert.Empty(response.Agents);
    }

    #endregion

    #region GetAgentDetail

    [Fact]
    public async Task GetAgentDetail_ReturnsDetail_WhenFound()
    {
        var detail = new AgentDetailResponse
        {
            Agent = new AgentDefinition { Id = "coder", Name = "Coder" },
            PromptContent = "# Role: Coder"
        };
        _mockService.Setup(s => s.GetAgentDetailAsync("coder", It.IsAny<CancellationToken>())).ReturnsAsync(detail);

        var result = await _controller.GetAgentDetail("coder", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AgentDetailResponse>(okResult.Value);
        Assert.Equal("coder", response.Agent.Id);
        Assert.Equal("# Role: Coder", response.PromptContent);
    }

    [Fact]
    public async Task GetAgentDetail_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.GetAgentDetailAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((AgentDetailResponse?)null);

        var result = await _controller.GetAgentDetail("missing", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    #endregion

    #region GetTeams

    [Fact]
    public async Task GetTeams_ReturnsTeamList()
    {
        var teams = new List<TeamDefinition>
        {
            new() { Id = "full-team", Name = "Full Team", Agents = new List<string> { "coder", "tester" } },
        };
        _mockService.Setup(s => s.GetTeamsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(teams);

        var result = await _controller.GetTeams(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TeamListResponse>(okResult.Value);
        Assert.Single(response.Teams);
    }

    #endregion

    #region GetTeamDetail

    [Fact]
    public async Task GetTeamDetail_ReturnsDetail_WhenFound()
    {
        var detail = new TeamDetailResponse
        {
            Team = new TeamDefinition { Id = "full-team", Name = "Full Team" },
            ResolvedAgents = new List<AgentDefinition> { new() { Id = "coder" } }
        };
        _mockService.Setup(s => s.GetTeamDetailAsync("full-team", It.IsAny<CancellationToken>())).ReturnsAsync(detail);

        var result = await _controller.GetTeamDetail("full-team", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TeamDetailResponse>(okResult.Value);
        Assert.Equal("full-team", response.Team.Id);
        Assert.Single(response.ResolvedAgents);
    }

    [Fact]
    public async Task GetTeamDetail_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.GetTeamDetailAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((TeamDetailResponse?)null);

        var result = await _controller.GetTeamDetail("missing", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    #endregion

    #region ComposeTeamMessage

    [Fact]
    public async Task ComposeTeamMessage_ReturnsComposedMessage()
    {
        var request = new ComposeTeamMessageRequest
        {
            AgentIds = new List<string> { "coder" },
            WorkflowPattern = "sequential"
        };
        _mockService.Setup(s => s.ComposeTeamSystemMessageAsync(It.IsAny<ComposeTeamMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComposeTeamMessageResponse
            {
                ComposedContent = "# Team\nCoder prompt...",
                AgentCount = 1,
                WorkflowPattern = "sequential"
            });

        var result = await _controller.ComposeTeamMessage(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ComposeTeamMessageResponse>(okResult.Value);
        Assert.Equal(1, response.AgentCount);
        Assert.Contains("Coder prompt", response.ComposedContent);
    }

    [Fact]
    public async Task ComposeTeamMessage_Returns400_WhenNoAgents()
    {
        var request = new ComposeTeamMessageRequest
        {
            AgentIds = new List<string>(),
            WorkflowPattern = "sequential"
        };

        var result = await _controller.ComposeTeamMessage(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    #endregion
}
