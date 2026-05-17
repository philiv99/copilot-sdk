using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for the multi-agent orchestration service. Direct delegation flow tests
/// require mocking <see cref="CopilotSession"/> (sealed/SDK-controlled), so we test the
/// public surface that is fully isolatable: tool construction, allowed-agent validation,
/// and idempotent cleanup operations.
/// </summary>
public class AgentOrchestrationServiceTests
{
    private readonly Mock<ILogger<AgentOrchestrationService>> _loggerMock = new();
    private readonly Mock<IAgentTeamService> _agentTeamServiceMock = new();
    private readonly Mock<IPermissionPolicyService> _permissionPolicyMock = new();
    private readonly Mock<ILogger<CopilotClientManager>> _clientManagerLoggerMock = new();
    private readonly Mock<IPersistenceService> _persistenceMock = new();
    private readonly Mock<Microsoft.AspNetCore.SignalR.IHubContext<Hubs.SessionHub>> _hubContextMock = new();
    private readonly Mock<ILogger<SessionEventDispatcher>> _dispatcherLoggerMock = new();

    private AgentOrchestrationService BuildSut()
    {
        var clientManager = new CopilotClientManager(_clientManagerLoggerMock.Object, _persistenceMock.Object);
        var dispatcher = new SessionEventDispatcher(_hubContextMock.Object, _dispatcherLoggerMock.Object);

        return new AgentOrchestrationService(
            clientManager,
            _agentTeamServiceMock.Object,
            dispatcher,
            _permissionPolicyMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void BuildDelegateTool_ReturnsAIFunction_WithDelegateToAgentName()
    {
        var sut = BuildSut();

        var tool = sut.BuildDelegateTool("parent-1", new[] { "coder", "code-reviewer" }, "claude-sonnet-4");

        Assert.NotNull(tool);
        Assert.Equal("delegate_to_agent", tool.Name);
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
    }

    [Fact]
    public void BuildDelegateTool_PublishesPermissiveSchema()
    {
        var sut = BuildSut();

        var tool = sut.BuildDelegateTool("parent-1", new[] { "coder", "code-reviewer" }, "claude-sonnet-4");
        var schema = tool.JsonSchema.GetRawText();

        Assert.Contains("agent_id", schema);
        Assert.Contains("task", schema);
        Assert.Contains("prompt", schema);
        Assert.DoesNotContain("required", schema);
        Assert.DoesNotContain("additionalProperties", schema);
        Assert.DoesNotContain("\"enum\"", schema);
        Assert.DoesNotContain("\"args\"", schema);
    }

    [Fact]
    public async Task DelegationProbeTool_ReturnsImmediateDiagnosticResult()
    {
        var sut = BuildSut();

        var tool = sut.BuildDelegationProbeTool("parent-1");
        var result = await tool.InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments(), CancellationToken.None);

        Assert.Equal("delegation_probe", tool.Name);
        Assert.Contains("backend custom tool callback received", result?.ToString() ?? string.Empty);
    }

    [Fact]
    public void BuildDelegateTool_StripsOrchestrator_FromAllowedAgents()
    {
        var sut = BuildSut();

        // Should not throw and should accept an empty list after orchestrator removal.
        var tool = sut.BuildDelegateTool("parent-1", new[] { "orchestrator", "Orchestrator" }, "claude-sonnet-4");

        Assert.NotNull(tool);
    }

    [Fact]
    public async Task AbortChildrenAsync_NoChildren_DoesNotThrow()
    {
        var sut = BuildSut();
        await sut.AbortChildrenAsync("parent-with-no-children");
    }

    [Fact]
    public async Task DisposeChildrenAsync_NoChildren_DoesNotThrow()
    {
        var sut = BuildSut();
        await sut.DisposeChildrenAsync("parent-with-no-children");
    }

    [Fact]
    public async Task DelegateTool_RejectsAgentNotInAllowedList()
    {
        var sut = BuildSut();
        var tool = sut.BuildDelegateTool("parent-1", new[] { "coder" }, "claude-sonnet-4");

        var args = new Dictionary<string, object?>
        {
            ["agent_id"] = "evil-agent",
            ["task"] = "do something"
        };

        var result = await tool.InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments(args), CancellationToken.None);
        var resultText = result?.ToString() ?? string.Empty;

        Assert.Contains("not configured", resultText);
        Assert.Contains("coder", resultText);
    }

    [Fact]
    public async Task DelegateTool_RejectsEmptyAgentId()
    {
        var sut = BuildSut();
        var tool = sut.BuildDelegateTool("parent-1", new[] { "coder" }, "claude-sonnet-4");

        var args = new Dictionary<string, object?>
        {
            ["agent_id"] = "",
            ["task"] = "do something"
        };

        var result = await tool.InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments(args), CancellationToken.None);
        Assert.Contains("required", result?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task DelegateTool_RejectsEmptyTask()
    {
        var sut = BuildSut();
        var tool = sut.BuildDelegateTool("parent-1", new[] { "coder" }, "claude-sonnet-4");

        var args = new Dictionary<string, object?>
        {
            ["agent_id"] = "coder",
            ["task"] = ""
        };

        var result = await tool.InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments(args), CancellationToken.None);
        Assert.Contains("required", result?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task DelegateTool_ReturnsClearError_WhenSpecialistNotPrepared()
    {
        var sut = BuildSut();
        var tool = sut.BuildDelegateTool("parent-1", new[] { "coder" }, "claude-sonnet-4");

        var args = new Dictionary<string, object?>
        {
            ["agent_id"] = "coder",
            ["task"] = "Create a project folder."
        };

        var result = await tool.InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments(args), CancellationToken.None);
        var resultText = result?.ToString() ?? string.Empty;

        Assert.Contains("was not prepared", resultText);
    }
}
