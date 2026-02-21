using System.Text.Json;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for AgentTeamService.
/// </summary>
public class AgentTeamServiceTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly Mock<ILogger<AgentTeamService>> _mockLogger;
    private readonly AgentTeamService _service;

    public AgentTeamServiceTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"agent-team-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBasePath);
        _mockLogger = new Mock<ILogger<AgentTeamService>>();
        _service = new AgentTeamService(_mockLogger.Object, _testBasePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }

    #region Helpers

    private void CreateAgent(string id, string name, string role, string category = "traditional", string icon = "🔧", List<string>? tags = null)
    {
        var agentDir = Path.Combine(_testBasePath, "docs", "agents", id);
        Directory.CreateDirectory(agentDir);

        var agent = new
        {
            id,
            name,
            role,
            description = $"Test agent: {name}",
            icon,
            tags = tags ?? new List<string> { "test" },
            category
        };

        File.WriteAllText(Path.Combine(agentDir, "agent.json"), JsonSerializer.Serialize(agent));
        File.WriteAllText(Path.Combine(agentDir, "prompt.md"), $"# Role: {name}\n\nYou are the {name}.");
    }

    private void CreateTeam(string id, string name, List<string> agents, string workflowPattern = "sequential")
    {
        var teamsDir = Path.Combine(_testBasePath, "docs", "teams");
        Directory.CreateDirectory(teamsDir);

        var team = new
        {
            id,
            name,
            description = $"Test team: {name}",
            icon = "👥",
            agents,
            workflowPattern,
            workflowDescription = "test workflow"
        };

        File.WriteAllText(Path.Combine(teamsDir, $"{id}.json"), JsonSerializer.Serialize(team));
    }

    #endregion

    #region GetAgentsAsync

    [Fact]
    public async Task GetAgentsAsync_ReturnsEmptyList_WhenFolderDoesNotExist()
    {
        var agents = await _service.GetAgentsAsync();
        Assert.Empty(agents);
    }

    [Fact]
    public async Task GetAgentsAsync_ReturnsAgents_FromFileSystem()
    {
        CreateAgent("coder", "Coder", "coder");
        CreateAgent("tester", "Tester", "tester", "specialist");

        var agents = await _service.GetAgentsAsync();

        Assert.Equal(2, agents.Count);
        Assert.Contains(agents, a => a.Id == "coder" && a.Category == "traditional");
        Assert.Contains(agents, a => a.Id == "tester" && a.Category == "specialist");
    }

    [Fact]
    public async Task GetAgentsAsync_SkipsFolders_WithoutAgentJson()
    {
        CreateAgent("valid", "Valid Agent", "valid");
        var emptyDir = Path.Combine(_testBasePath, "docs", "agents", "invalid");
        Directory.CreateDirectory(emptyDir);

        var agents = await _service.GetAgentsAsync();

        Assert.Single(agents);
        Assert.Equal("valid", agents[0].Id);
    }

    [Fact]
    public async Task GetAgentsAsync_SkipsMalformedJson()
    {
        CreateAgent("valid", "Valid Agent", "valid");
        var badDir = Path.Combine(_testBasePath, "docs", "agents", "bad");
        Directory.CreateDirectory(badDir);
        File.WriteAllText(Path.Combine(badDir, "agent.json"), "NOT JSON{{{");

        var agents = await _service.GetAgentsAsync();

        Assert.Single(agents);
    }

    #endregion

    #region GetAgentDetailAsync

    [Fact]
    public async Task GetAgentDetailAsync_ReturnsDetail_WithPromptContent()
    {
        CreateAgent("coder", "Coder", "coder");

        var detail = await _service.GetAgentDetailAsync("coder");

        Assert.NotNull(detail);
        Assert.Equal("coder", detail!.Agent.Id);
        Assert.Equal("Coder", detail.Agent.Name);
        Assert.Contains("You are the Coder", detail.PromptContent);
    }

    [Fact]
    public async Task GetAgentDetailAsync_ReturnsNull_WhenNotFound()
    {
        var detail = await _service.GetAgentDetailAsync("nonexistent");
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetAgentDetailAsync_ReturnsNull_WhenIdIsNull()
    {
        var detail = await _service.GetAgentDetailAsync(null!);
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetAgentDetailAsync_ReturnsEmptyPrompt_WhenNoPromptMd()
    {
        var agentDir = Path.Combine(_testBasePath, "docs", "agents", "no-prompt");
        Directory.CreateDirectory(agentDir);
        var agent = new { id = "no-prompt", name = "No Prompt", role = "test", description = "test", icon = "🔧", tags = new[] { "test" }, category = "traditional" };
        File.WriteAllText(Path.Combine(agentDir, "agent.json"), JsonSerializer.Serialize(agent));

        var detail = await _service.GetAgentDetailAsync("no-prompt");

        Assert.NotNull(detail);
        Assert.Equal("", detail!.PromptContent);
    }

    [Fact]
    public async Task GetAgentDetailAsync_SanitizesPathTraversal()
    {
        var detail = await _service.GetAgentDetailAsync("../../../etc/passwd");
        Assert.Null(detail);
    }

    #endregion

    #region GetTeamsAsync

    [Fact]
    public async Task GetTeamsAsync_ReturnsEmptyList_WhenFolderDoesNotExist()
    {
        var teams = await _service.GetTeamsAsync();
        Assert.Empty(teams);
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsTeams_FromFileSystem()
    {
        CreateTeam("full-team", "Full Team", new List<string> { "coder", "tester" });
        CreateTeam("frontend", "Frontend Squad", new List<string> { "coder" }, "parallel");

        var teams = await _service.GetTeamsAsync();

        Assert.Equal(2, teams.Count);
        Assert.Contains(teams, t => t.Id == "full-team" && t.WorkflowPattern == "sequential");
        Assert.Contains(teams, t => t.Id == "frontend" && t.WorkflowPattern == "parallel");
    }

    [Fact]
    public async Task GetTeamsAsync_SkipsMalformedJson()
    {
        var teamsDir = Path.Combine(_testBasePath, "docs", "teams");
        Directory.CreateDirectory(teamsDir);
        File.WriteAllText(Path.Combine(teamsDir, "bad.json"), "NOT JSON{{{");
        CreateTeam("valid", "Valid Team", new List<string> { "coder" });

        var teams = await _service.GetTeamsAsync();

        Assert.Single(teams);
        Assert.Equal("valid", teams[0].Id);
    }

    #endregion

    #region GetTeamDetailAsync

    [Fact]
    public async Task GetTeamDetailAsync_ReturnsDetail_WithResolvedAgents()
    {
        CreateAgent("coder", "Coder", "coder");
        CreateAgent("tester", "Tester", "tester");
        CreateTeam("dev-team", "Dev Team", new List<string> { "coder", "tester" });

        var detail = await _service.GetTeamDetailAsync("dev-team");

        Assert.NotNull(detail);
        Assert.Equal("dev-team", detail!.Team.Id);
        Assert.Equal(2, detail.ResolvedAgents.Count);
    }

    [Fact]
    public async Task GetTeamDetailAsync_ReturnsNull_WhenNotFound()
    {
        var detail = await _service.GetTeamDetailAsync("nonexistent");
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetTeamDetailAsync_ResolvesOnlyExistingAgents()
    {
        CreateAgent("coder", "Coder", "coder");
        CreateTeam("partial", "Partial Team", new List<string> { "coder", "nonexistent" });

        var detail = await _service.GetTeamDetailAsync("partial");

        Assert.NotNull(detail);
        Assert.Single(detail!.ResolvedAgents);
        Assert.Equal("coder", detail.ResolvedAgents[0].Id);
    }

    #endregion

    #region ComposeTeamSystemMessageAsync

    [Fact]
    public async Task ComposeTeamSystemMessageAsync_ComposesMessage_FromAgents()
    {
        CreateAgent("coder", "Coder", "coder");
        CreateAgent("tester", "Tester", "tester");

        var request = new ComposeTeamMessageRequest
        {
            AgentIds = new List<string> { "coder", "tester" },
            WorkflowPattern = "sequential"
        };

        var result = await _service.ComposeTeamSystemMessageAsync(request);

        Assert.Equal(2, result.AgentCount);
        Assert.Equal("sequential", result.WorkflowPattern);
        Assert.Contains("Team Configuration", result.ComposedContent);
        Assert.Contains("You are the Coder", result.ComposedContent);
        Assert.Contains("You are the Tester", result.ComposedContent);
        Assert.Contains("**Workflow Pattern**: sequential", result.ComposedContent);
    }

    [Fact]
    public async Task ComposeTeamSystemMessageAsync_IncludesCustomContent()
    {
        CreateAgent("coder", "Coder", "coder");

        var request = new ComposeTeamMessageRequest
        {
            AgentIds = new List<string> { "coder" },
            WorkflowPattern = "sequential",
            CustomContent = "Always use TypeScript."
        };

        var result = await _service.ComposeTeamSystemMessageAsync(request);

        Assert.Contains("Additional Instructions", result.ComposedContent);
        Assert.Contains("Always use TypeScript.", result.ComposedContent);
    }

    [Fact]
    public async Task ComposeTeamSystemMessageAsync_IncludesTemplate_WhenPresent()
    {
        // Create a template
        var templateDir = Path.Combine(_testBasePath, "docs", "system_prompts", "test-template");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "copilot-instructions.md"), "# Base Template\n\nBase instructions here.");

        CreateAgent("coder", "Coder", "coder");

        var request = new ComposeTeamMessageRequest
        {
            TemplateName = "test-template",
            AgentIds = new List<string> { "coder" },
            WorkflowPattern = "sequential"
        };

        var result = await _service.ComposeTeamSystemMessageAsync(request);

        Assert.Contains("Base Template", result.ComposedContent);
        Assert.Contains("Base instructions here", result.ComposedContent);
        Assert.Contains("You are the Coder", result.ComposedContent);
    }

    [Fact]
    public async Task ComposeTeamSystemMessageAsync_SkipsUnknownAgents()
    {
        CreateAgent("coder", "Coder", "coder");

        var request = new ComposeTeamMessageRequest
        {
            AgentIds = new List<string> { "coder", "nonexistent" },
            WorkflowPattern = "parallel"
        };

        var result = await _service.ComposeTeamSystemMessageAsync(request);

        Assert.Equal(2, result.AgentCount); // count includes all requested IDs
        Assert.Contains("You are the Coder", result.ComposedContent);
    }

    [Fact]
    public async Task ComposeTeamSystemMessageAsync_ReturnsEmpty_WhenNoAgents()
    {
        var request = new ComposeTeamMessageRequest
        {
            AgentIds = new List<string>(),
            WorkflowPattern = "sequential"
        };

        var result = await _service.ComposeTeamSystemMessageAsync(request);

        Assert.Equal(0, result.AgentCount);
        Assert.True(string.IsNullOrWhiteSpace(result.ComposedContent) || !result.ComposedContent.Contains("Team Configuration"));
    }

    #endregion
}
