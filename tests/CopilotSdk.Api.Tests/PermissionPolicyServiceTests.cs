using CopilotSdk.Api.Hubs;
using System.Text.Json;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CopilotSdk.Api.Tests;

public class PermissionPolicyServiceTests
{
    [Fact]
    public async Task HandlePermissionRequestAsync_ApprovesShellCommandUnderAllowedRoot()
    {
        var service = CreateService();
        var request = ShellRequest("mkdir C:\\development\\repos\\linkittydo-fencing-duel\ncd C:\\development\\repos\\linkittydo-fencing-duel\ngit init");

        var result = await service.HandlePermissionRequestAsync(request, Invocation());

        Assert.Equal("approved", result.Kind);
    }

    [Fact]
    public async Task HandlePermissionRequestAsync_ApprovesShellCommandWithoutAbsolutePath()
    {
        var service = CreateService();
        var request = ShellRequest("npm install\nnpm run build");

        var result = await service.HandlePermissionRequestAsync(request, Invocation());

        Assert.Equal("approved", result.Kind);
    }

    [Fact]
    public async Task HandlePermissionRequestAsync_DeniesShellCommandOutsideAllowedRoot()
    {
        var service = CreateService();
        var request = ShellRequest("mkdir C:\\Users\\phili\\Desktop\\outside-project");

        var result = await service.HandlePermissionRequestAsync(request, Invocation());

        Assert.Equal("denied-by-rules", result.Kind);
    }

    [Fact]
    public async Task HandlePermissionRequestAsync_DeniesDisallowedKind()
    {
        var service = CreateService();
        var request = new PermissionRequest { Kind = "url" };

        var result = await service.HandlePermissionRequestAsync(request, Invocation());

        Assert.Equal("denied-by-rules", result.Kind);
    }

    [Fact]
    public async Task HandlePermissionRequestAsync_DeniesWhenPolicyModeDisabled()
    {
        var service = CreateService(new PermissionPolicyOptions { Mode = "Deny" });
        var request = ShellRequest("mkdir C:\\development\\repos\\linkittydo-fencing-duel");

        var result = await service.HandlePermissionRequestAsync(request, Invocation());

        Assert.Equal("denied-by-rules", result.Kind);
    }

    [Fact]
    public async Task HandlePermissionRequestAsync_DeniesMatchingCommandPattern()
    {
        var service = CreateService(new PermissionPolicyOptions
        {
            Mode = "AutoApproveLocalExecutor",
            AllowedRoot = @"C:\development\repos",
            AllowedKinds = new() { "shell" },
            DeniedCommandPatterns = new() { "Remove-Item\\s+-Recurse" }
        });
        var request = ShellRequest("Remove-Item -Recurse C:\\development\\repos\\linkittydo-fencing-duel");

        var result = await service.HandlePermissionRequestAsync(request, Invocation());

        Assert.Equal("denied-by-rules", result.Kind);
    }

    private static PermissionPolicyService CreateService(PermissionPolicyOptions? options = null)
    {
        options ??= new PermissionPolicyOptions
        {
            Mode = "AutoApproveLocalExecutor",
            AllowedRoot = @"C:\development\repos",
            AllowedKinds = new() { "shell", "write", "read" }
        };

        return new PermissionPolicyService(
            Options.Create(options),
            NullLogger<PermissionPolicyService>.Instance,
            CreateHubContext());
    }

    private static IHubContext<SessionHub> CreateHubContext()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<SessionHub>>();
        hubContext
            .SetupGet(c => c.Clients)
            .Returns(clients.Object);

        return hubContext.Object;
    }

    private static PermissionInvocation Invocation() => new() { SessionId = "session-1" };

    private static PermissionRequest ShellRequest(string command)
    {
        return new PermissionRequest
        {
            Kind = "shell",
            ToolCallId = "tool-1",
            ExtensionData = new Dictionary<string, object>
            {
                ["command"] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(command))
            }
        };
    }
}
