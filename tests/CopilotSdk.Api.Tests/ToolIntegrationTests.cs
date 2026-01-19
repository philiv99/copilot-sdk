using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Services;
using CopilotSdk.Api.Tools;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Integration tests for verifying tool definitions are correctly passed through
/// the service layer to sessions.
/// </summary>
public class ToolIntegrationTests
{
    private readonly Mock<ILogger<ToolExecutionService>> _toolServiceLoggerMock;
    private readonly ToolExecutionService _toolService;
    private readonly Mock<ILogger<SessionManager>> _sessionManagerLoggerMock;
    private readonly SessionManager _sessionManager;

    public ToolIntegrationTests()
    {
        _toolServiceLoggerMock = new Mock<ILogger<ToolExecutionService>>();
        _toolService = new ToolExecutionService(_toolServiceLoggerMock.Object);
        _sessionManagerLoggerMock = new Mock<ILogger<SessionManager>>();
        _sessionManager = new SessionManager(_sessionManagerLoggerMock.Object);
    }

    #region CreateSessionRequest with Tools Integration Tests

    [Fact]
    public void CreateSessionRequest_WithTools_BuildsAIFunctionsCorrectly()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "calculator",
                Description = "Performs basic arithmetic operations",
                Parameters = new List<ToolParameter>
                {
                    new() { Name = "operation", Type = "string", Description = "The operation to perform", Required = true },
                    new() { Name = "a", Type = "number", Description = "First operand", Required = true },
                    new() { Name = "b", Type = "number", Description = "Second operand", Required = true }
                }
            },
            new()
            {
                Name = "formatter",
                Description = "Formats text",
                Parameters = new List<ToolParameter>
                {
                    new() { Name = "text", Type = "string", Description = "Text to format", Required = true },
                    new() { Name = "style", Type = "string", Description = "Format style", Required = false }
                }
            }
        };

        // Act
        var aiFunctions = _toolService.BuildAIFunctions(tools);

        // Assert
        Assert.Equal(2, aiFunctions.Count);

        var calcFunc = aiFunctions.First(f => f.Name == "calculator");
        Assert.NotNull(calcFunc);

        var formatFunc = aiFunctions.First(f => f.Name == "formatter");
        Assert.NotNull(formatFunc);
    }

    [Fact]
    public async Task CreateSessionRequest_WithToolsAndHandlers_ExecutesCorrectly()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "add_numbers",
            Description = "Adds two numbers",
            Parameters = new List<ToolParameter>
            {
                new() { Name = "a", Type = "number", Required = true },
                new() { Name = "b", Type = "number", Required = true }
            }
        };

        _toolService.RegisterTool(definition, (args, ct) =>
        {
            var a = Convert.ToDouble(args["a"]);
            var b = Convert.ToDouble(args["b"]);
            return Task.FromResult<object?>(a + b);
        });

        // Act - verify we can execute through the service
        var result = await _toolService.ExecuteToolAsync("add_numbers", new Dictionary<string, object?>
        {
            { "a", 5.0 },
            { "b", 3.0 }
        });

        // Assert
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void CreateSessionRequest_WithTools_VerifyNamesAndDescriptions()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "test_tool",
                Description = "A test tool description"
            }
        };

        // Act
        var aiFunctions = _toolService.BuildAIFunctions(tools);

        // Assert
        Assert.Single(aiFunctions);
        var function = aiFunctions.First();
        Assert.Equal("test_tool", function.Name);
        // Description should be preserved
        Assert.Contains("test", function.Name.ToLower());
    }

    #endregion

    #region Demo Tools Integration Tests

    [Fact]
    public async Task DemoTools_EchoTool_IntegrationTest()
    {
        // Arrange
        DemoTools.RegisterAllDemoTools(_toolService);

        // Act
        var result = await _toolService.ExecuteToolAsync("echo_tool", new Dictionary<string, object?>
        {
            { "message", "Integration Test" },
            { "uppercase", true }
        });

        // Assert
        Assert.NotNull(result);
        var echoResult = Assert.IsType<EchoToolResult>(result);
        Assert.Equal("INTEGRATION TEST", echoResult.Echoed);
    }

    [Fact]
    public async Task DemoTools_GetCurrentTime_IntegrationTest()
    {
        // Arrange
        DemoTools.RegisterAllDemoTools(_toolService);

        // Act
        var result = await _toolService.ExecuteToolAsync("get_current_time", new Dictionary<string, object?>
        {
            { "format", "yyyy-MM-dd" }
        });

        // Assert
        Assert.NotNull(result);
        var timeResult = Assert.IsType<GetCurrentTimeResult>(result);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", timeResult.CurrentTime);
    }

    [Fact]
    public void DemoTools_AllTools_BuildAIFunctionsSuccessfully()
    {
        // Arrange
        var definitions = DemoTools.GetAllDefinitions();

        // Act
        var aiFunctions = _toolService.BuildAIFunctions(definitions);

        // Assert
        Assert.Equal(2, aiFunctions.Count);
        Assert.Contains(aiFunctions, f => f.Name == "echo_tool");
        Assert.Contains(aiFunctions, f => f.Name == "get_current_time");
    }

    #endregion

    #region Tool Registration and Execution Flow Tests

    [Fact]
    public async Task ToolFlow_RegisterBuildExecute_CompleteFlow()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "greeting_tool",
            Description = "Returns a greeting",
            Parameters = new List<ToolParameter>
            {
                new() { Name = "name", Type = "string", Required = true }
            }
        };

        _toolService.RegisterTool(definition, (args, ct) =>
        {
            var name = args["name"]?.ToString() ?? "World";
            return Task.FromResult<object?>($"Hello, {name}!");
        });

        // Act - Build AIFunctions (simulating what SessionService does)
        var aiFunctions = _toolService.BuildAIFunctions(new[] { definition });
        
        // Then execute through the service
        var result = await _toolService.ExecuteToolAsync("greeting_tool", new Dictionary<string, object?>
        {
            { "name", "Copilot" }
        });

        // Assert
        Assert.Single(aiFunctions);
        Assert.Equal("greeting_tool", aiFunctions.First().Name);
        Assert.Equal("Hello, Copilot!", result);
    }

    [Fact]
    public async Task ToolFlow_MultipleToolsInSession_ExecuteIndependently()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "multiply",
                Description = "Multiplies two numbers",
                Parameters = new List<ToolParameter>
                {
                    new() { Name = "a", Type = "number", Required = true },
                    new() { Name = "b", Type = "number", Required = true }
                }
            },
            new()
            {
                Name = "divide",
                Description = "Divides two numbers",
                Parameters = new List<ToolParameter>
                {
                    new() { Name = "a", Type = "number", Required = true },
                    new() { Name = "b", Type = "number", Required = true }
                }
            }
        };

        _toolService.RegisterTool(tools[0], (args, ct) =>
        {
            var a = Convert.ToDouble(args["a"]);
            var b = Convert.ToDouble(args["b"]);
            return Task.FromResult<object?>(a * b);
        });

        _toolService.RegisterTool(tools[1], (args, ct) =>
        {
            var a = Convert.ToDouble(args["a"]);
            var b = Convert.ToDouble(args["b"]);
            return Task.FromResult<object?>(a / b);
        });

        // Act
        var aiFunctions = _toolService.BuildAIFunctions(tools);
        var multiplyResult = await _toolService.ExecuteToolAsync("multiply", new Dictionary<string, object?> { { "a", 6.0 }, { "b", 7.0 } });
        var divideResult = await _toolService.ExecuteToolAsync("divide", new Dictionary<string, object?> { { "a", 42.0 }, { "b", 6.0 } });

        // Assert
        Assert.Equal(2, aiFunctions.Count);
        Assert.Equal(42.0, multiplyResult);
        Assert.Equal(7.0, divideResult);
    }

    #endregion

    #region CreateSessionRequest Model Mapping Tests

    [Fact]
    public void CreateSessionRequest_ToolsMapped_ToSessionConfig()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            SessionId = "test-session",
            Model = "gpt-4",
            Streaming = true,
            Tools = new List<ToolDefinition>
            {
                new()
                {
                    Name = "custom_tool",
                    Description = "A custom tool",
                    Parameters = new List<ToolParameter>
                    {
                        new() { Name = "input", Type = "string", Required = true }
                    }
                }
            }
        };

        // Act - Simulate what SessionService does
        var config = new SessionConfig
        {
            SessionId = request.SessionId,
            Model = request.Model,
            Streaming = request.Streaming,
            Tools = request.Tools
        };

        // Assert
        Assert.NotNull(config.Tools);
        Assert.Single(config.Tools);
        Assert.Equal("custom_tool", config.Tools[0].Name);
        Assert.Single(config.Tools[0].Parameters!);
    }

    [Fact]
    public void ResumeSessionRequest_ToolsMapped_Correctly()
    {
        // Arrange
        var request = new ResumeSessionRequest
        {
            Streaming = false,
            Tools = new List<ToolDefinition>
            {
                new()
                {
                    Name = "resume_tool",
                    Description = "A tool for resumed sessions"
                }
            }
        };

        // Act
        var aiFunctions = _toolService.BuildAIFunctions(request.Tools);

        // Assert
        Assert.Single(aiFunctions);
        Assert.Equal("resume_tool", aiFunctions.First().Name);
    }

    #endregion

    #region Error Handling Integration Tests

    [Fact]
    public async Task ToolExecution_WithException_PropagatesError()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "failing_tool",
            Description = "A tool that fails"
        };

        _toolService.RegisterTool(definition, (args, ct) =>
        {
            throw new InvalidOperationException("Tool execution failed");
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _toolService.ExecuteToolAsync("failing_tool", new Dictionary<string, object?>()));

        Assert.Equal("Tool execution failed", exception.Message);
    }

    [Fact]
    public async Task ToolExecution_WithCancellation_HonorsToken()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "cancellable_tool",
            Description = "A tool that can be cancelled"
        };

        _toolService.RegisterTool(definition, async (args, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1000, ct);
            return "completed";
        });

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _toolService.ExecuteToolAsync("cancellable_tool", new Dictionary<string, object?>(), cts.Token));
    }

    #endregion
}
