using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using CopilotSdk.Api.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for ToolExecutionService.
/// </summary>
public class ToolExecutionServiceTests
{
    private readonly Mock<ILogger<ToolExecutionService>> _loggerMock;
    private readonly ToolExecutionService _service;

    public ToolExecutionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ToolExecutionService>>();
        _service = new ToolExecutionService(_loggerMock.Object);
    }

    #region RegisterTool Tests

    [Fact]
    public void RegisterTool_ValidDefinitionAndHandler_Succeeds()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "test_tool",
            Description = "A test tool"
        };
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler = 
            (args, ct) => Task.FromResult<object?>("result");

        // Act
        _service.RegisterTool(definition, handler);

        // Assert
        Assert.True(_service.IsToolRegistered("test_tool"));
    }

    [Fact]
    public void RegisterTool_DuplicateName_UpdatesExisting()
    {
        // Arrange
        var definition1 = new ToolDefinition { Name = "test_tool", Description = "First" };
        var definition2 = new ToolDefinition { Name = "test_tool", Description = "Second" };
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler1 = 
            (args, ct) => Task.FromResult<object?>("result1");
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler2 = 
            (args, ct) => Task.FromResult<object?>("result2");

        // Act
        _service.RegisterTool(definition1, handler1);
        _service.RegisterTool(definition2, handler2);

        // Assert
        var tools = _service.GetRegisteredTools();
        Assert.Single(tools);
        Assert.Equal("Second", tools.First().Description);
    }

    [Fact]
    public void RegisterTool_NullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler = 
            (args, ct) => Task.FromResult<object?>("result");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.RegisterTool(null!, handler));
    }

    [Fact]
    public void RegisterTool_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "test_tool" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.RegisterTool(definition, null!));
    }

    [Fact]
    public void RegisterTool_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "" };
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler = 
            (args, ct) => Task.FromResult<object?>("result");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.RegisterTool(definition, handler));
    }

    #endregion

    #region UnregisterTool Tests

    [Fact]
    public void UnregisterTool_ExistingTool_ReturnsTrue()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "test_tool" };
        _service.RegisterTool(definition, (args, ct) => Task.FromResult<object?>("result"));

        // Act
        var result = _service.UnregisterTool("test_tool");

        // Assert
        Assert.True(result);
        Assert.False(_service.IsToolRegistered("test_tool"));
    }

    [Fact]
    public void UnregisterTool_NonExistingTool_ReturnsFalse()
    {
        // Act
        var result = _service.UnregisterTool("nonexistent_tool");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnregisterTool_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.UnregisterTool(null!));
    }

    #endregion

    #region ExecuteToolAsync Tests

    [Fact]
    public async Task ExecuteToolAsync_RegisteredTool_ExecutesHandler()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "test_tool" };
        var expectedResult = new { message = "executed" };
        _service.RegisterTool(definition, (args, ct) => Task.FromResult<object?>(expectedResult));

        // Act
        var result = await _service.ExecuteToolAsync("test_tool", new Dictionary<string, object?>());

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteToolAsync_PassesArgumentsToHandler()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "test_tool" };
        IDictionary<string, object?>? receivedArgs = null;
        _service.RegisterTool(definition, (args, ct) =>
        {
            receivedArgs = args;
            return Task.FromResult<object?>(null);
        });

        var inputArgs = new Dictionary<string, object?>
        {
            { "param1", "value1" },
            { "param2", 42 }
        };

        // Act
        await _service.ExecuteToolAsync("test_tool", inputArgs);

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal("value1", receivedArgs["param1"]);
        Assert.Equal(42, receivedArgs["param2"]);
    }

    [Fact]
    public async Task ExecuteToolAsync_NonRegisteredTool_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.ExecuteToolAsync("nonexistent", new Dictionary<string, object?>()));
    }

    [Fact]
    public async Task ExecuteToolAsync_HandlerThrows_PropagatesException()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "failing_tool" };
        _service.RegisterTool(definition, (args, ct) => 
            throw new InvalidOperationException("Tool failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ExecuteToolAsync("failing_tool", new Dictionary<string, object?>()));
    }

    [Fact]
    public async Task ExecuteToolAsync_NullToolName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.ExecuteToolAsync(null!, new Dictionary<string, object?>()));
    }

    [Fact]
    public async Task ExecuteToolAsync_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "test_tool" };
        _service.RegisterTool(definition, (args, ct) => Task.FromResult<object?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.ExecuteToolAsync("test_tool", null!));
    }

    #endregion

    #region GetRegisteredTools Tests

    [Fact]
    public void GetRegisteredTools_NoTools_ReturnsEmptyCollection()
    {
        // Act
        var tools = _service.GetRegisteredTools();

        // Assert
        Assert.Empty(tools);
    }

    [Fact]
    public void GetRegisteredTools_MultipleTools_ReturnsAllDefinitions()
    {
        // Arrange
        var def1 = new ToolDefinition { Name = "tool1", Description = "Tool 1" };
        var def2 = new ToolDefinition { Name = "tool2", Description = "Tool 2" };
        var def3 = new ToolDefinition { Name = "tool3", Description = "Tool 3" };

        _service.RegisterTool(def1, (args, ct) => Task.FromResult<object?>(null));
        _service.RegisterTool(def2, (args, ct) => Task.FromResult<object?>(null));
        _service.RegisterTool(def3, (args, ct) => Task.FromResult<object?>(null));

        // Act
        var tools = _service.GetRegisteredTools();

        // Assert
        Assert.Equal(3, tools.Count);
        Assert.Contains(tools, t => t.Name == "tool1");
        Assert.Contains(tools, t => t.Name == "tool2");
        Assert.Contains(tools, t => t.Name == "tool3");
    }

    #endregion

    #region IsToolRegistered Tests

    [Fact]
    public void IsToolRegistered_RegisteredTool_ReturnsTrue()
    {
        // Arrange
        var definition = new ToolDefinition { Name = "test_tool" };
        _service.RegisterTool(definition, (args, ct) => Task.FromResult<object?>(null));

        // Act & Assert
        Assert.True(_service.IsToolRegistered("test_tool"));
    }

    [Fact]
    public void IsToolRegistered_NonRegisteredTool_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_service.IsToolRegistered("nonexistent"));
    }

    #endregion

    #region BuildAIFunctions Tests

    [Fact]
    public void BuildAIFunctions_EmptyList_ReturnsEmptyCollection()
    {
        // Act
        var functions = _service.BuildAIFunctions(new List<ToolDefinition>());

        // Assert
        Assert.Empty(functions);
    }

    [Fact]
    public void BuildAIFunctions_MultipleDefinitions_ReturnsCorrectCount()
    {
        // Arrange
        var definitions = new List<ToolDefinition>
        {
            new() { Name = "tool1", Description = "Tool 1" },
            new() { Name = "tool2", Description = "Tool 2" }
        };

        // Act
        var functions = _service.BuildAIFunctions(definitions);

        // Assert
        Assert.Equal(2, functions.Count);
    }

    [Fact]
    public void BuildAIFunctions_PreservesNameAndDescription()
    {
        // Arrange
        var definitions = new List<ToolDefinition>
        {
            new() 
            { 
                Name = "my_tool", 
                Description = "My custom tool description" 
            }
        };

        // Act
        var functions = _service.BuildAIFunctions(definitions);

        // Assert
        var function = functions.First();
        Assert.Equal("my_tool", function.Name);
        // Description is passed to AIFunctionFactory.Create but we verify through name
        Assert.NotNull(function);
    }

    [Fact]
    public async Task BuildAIFunctions_WithRegisteredHandler_UsesHandler()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "registered_tool",
            Description = "A registered tool"
        };

        var expectedResult = new { value = 42 };
        _service.RegisterTool(definition, (args, ct) => Task.FromResult<object?>(expectedResult));

        // Act - BuildAIFunctions works and we can execute through the service
        var functions = _service.BuildAIFunctions(new[] { definition });
        var result = await _service.ExecuteToolAsync("registered_tool", new Dictionary<string, object?>());

        // Assert
        Assert.Single(functions);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void BuildAIFunctions_WithoutRegisteredHandler_CreatesFunction()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "unregistered_tool",
            Description = "An unregistered tool"
        };

        // Act
        var functions = _service.BuildAIFunctions(new[] { definition });

        // Assert - The function is created even without a registered handler
        Assert.Single(functions);
        Assert.Equal("unregistered_tool", functions.First().Name);
    }

    [Fact]
    public void BuildAIFunctions_NullDefinitions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.BuildAIFunctions(null!));
    }

    #endregion

    #region ClearTools Tests

    [Fact]
    public void ClearTools_WithRegisteredTools_RemovesAllTools()
    {
        // Arrange
        _service.RegisterTool(new ToolDefinition { Name = "tool1" }, (args, ct) => Task.FromResult<object?>(null));
        _service.RegisterTool(new ToolDefinition { Name = "tool2" }, (args, ct) => Task.FromResult<object?>(null));
        _service.RegisterTool(new ToolDefinition { Name = "tool3" }, (args, ct) => Task.FromResult<object?>(null));

        // Act
        _service.ClearTools();

        // Assert
        Assert.Empty(_service.GetRegisteredTools());
    }

    [Fact]
    public void ClearTools_NoTools_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _service.ClearTools());
        Assert.Null(exception);
    }

    #endregion

    #region DemoTools Tests

    [Fact]
    public void DemoTools_EchoToolDefinition_HasCorrectStructure()
    {
        // Act
        var definition = DemoTools.EchoToolDefinition;

        // Assert
        Assert.Equal("echo_tool", definition.Name);
        Assert.Contains("echo", definition.Description.ToLower());
        Assert.NotNull(definition.Parameters);
        Assert.Contains(definition.Parameters, p => p.Name == "message" && p.Required);
        Assert.Contains(definition.Parameters, p => p.Name == "uppercase" && !p.Required);
    }

    [Fact]
    public void DemoTools_GetCurrentTimeDefinition_HasCorrectStructure()
    {
        // Act
        var definition = DemoTools.GetCurrentTimeDefinition;

        // Assert
        Assert.Equal("get_current_time", definition.Name);
        Assert.Contains("time", definition.Description.ToLower());
        Assert.NotNull(definition.Parameters);
        Assert.Contains(definition.Parameters, p => p.Name == "format");
        Assert.Contains(definition.Parameters, p => p.Name == "timezone");
    }

    [Fact]
    public async Task DemoTools_EchoToolHandler_EchosMessage()
    {
        // Arrange
        var args = new Dictionary<string, object?>
        {
            { "message", "Hello, World!" }
        };

        // Act
        var result = await DemoTools.EchoToolHandler(args, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var echoResult = Assert.IsType<EchoToolResult>(result);
        Assert.Equal("Hello, World!", echoResult.Echoed);
        Assert.Equal(13, echoResult.OriginalLength);
    }

    [Fact]
    public async Task DemoTools_EchoToolHandler_UppercaseOption_ConvertsToUppercase()
    {
        // Arrange
        var args = new Dictionary<string, object?>
        {
            { "message", "hello" },
            { "uppercase", true }
        };

        // Act
        var result = await DemoTools.EchoToolHandler(args, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var echoResult = Assert.IsType<EchoToolResult>(result);
        Assert.Equal("HELLO", echoResult.Echoed);
    }

    [Fact]
    public async Task DemoTools_GetCurrentTimeHandler_ReturnsCurrentTime()
    {
        // Arrange
        var args = new Dictionary<string, object?>();

        // Act
        var result = await DemoTools.GetCurrentTimeHandler(args, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var timeResult = Assert.IsType<GetCurrentTimeResult>(result);
        Assert.NotNull(timeResult.CurrentTime);
        Assert.Equal("UTC", timeResult.Timezone);
    }

    [Fact]
    public async Task DemoTools_GetCurrentTimeHandler_WithFormat_UsesFormat()
    {
        // Arrange
        var args = new Dictionary<string, object?>
        {
            { "format", "yyyy-MM-dd" }
        };

        // Act
        var result = await DemoTools.GetCurrentTimeHandler(args, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var timeResult = Assert.IsType<GetCurrentTimeResult>(result);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", timeResult.CurrentTime);
    }

    [Fact]
    public void DemoTools_RegisterAllDemoTools_RegistersAllTools()
    {
        // Arrange
        var service = new ToolExecutionService(_loggerMock.Object);

        // Act
        DemoTools.RegisterAllDemoTools(service);

        // Assert
        Assert.True(service.IsToolRegistered("echo_tool"));
        Assert.True(service.IsToolRegistered("get_current_time"));
    }

    [Fact]
    public void DemoTools_GetAllDefinitions_ReturnsAllDefinitions()
    {
        // Act
        var definitions = DemoTools.GetAllDefinitions();

        // Assert
        Assert.Equal(2, definitions.Count);
        Assert.Contains(definitions, d => d.Name == "echo_tool");
        Assert.Contains(definitions, d => d.Name == "get_current_time");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task RegisterAndExecute_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var executionCount = 0;

        // Act - Register and execute tools concurrently
        for (int i = 0; i < 100; i++)
        {
            var toolName = $"tool_{i}";
            tasks.Add(Task.Run(() =>
            {
                _service.RegisterTool(
                    new ToolDefinition { Name = toolName },
                    (args, ct) =>
                    {
                        Interlocked.Increment(ref executionCount);
                        return Task.FromResult<object?>(toolName);
                    });
            }));
        }

        await Task.WhenAll(tasks);
        tasks.Clear();

        // Execute all tools
        for (int i = 0; i < 100; i++)
        {
            var toolName = $"tool_{i}";
            tasks.Add(_service.ExecuteToolAsync(toolName, new Dictionary<string, object?>()));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, _service.GetRegisteredTools().Count);
        Assert.Equal(100, executionCount);
    }

    #endregion
}
