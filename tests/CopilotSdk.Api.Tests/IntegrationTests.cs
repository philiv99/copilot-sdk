using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using CopilotSdk.Api.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Integration tests that exercise the coordination between services and controllers.
/// These tests verify the application layer logic without requiring actual SDK connections.
/// </summary>
public class IntegrationTests
{
    #region Test Infrastructure

    private readonly Mock<ILogger<CopilotClientService>> _clientServiceLoggerMock;
    private readonly Mock<ILogger<ToolExecutionService>> _toolServiceLoggerMock;
    private readonly Mock<ILogger<CopilotClientController>> _clientControllerLoggerMock;
    private readonly Mock<ILogger<SessionsController>> _sessionsControllerLoggerMock;
    private readonly Mock<ICopilotClientService> _clientServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly ToolExecutionService _toolExecutionService;
    private readonly CopilotClientController _clientController;
    private readonly SessionsController _sessionsController;
    private readonly CancellationToken _ct = CancellationToken.None;

    public IntegrationTests()
    {
        // Setup loggers
        _clientServiceLoggerMock = new Mock<ILogger<CopilotClientService>>();
        _toolServiceLoggerMock = new Mock<ILogger<ToolExecutionService>>();
        _clientControllerLoggerMock = new Mock<ILogger<CopilotClientController>>();
        _sessionsControllerLoggerMock = new Mock<ILogger<SessionsController>>();

        // Create service mocks
        _clientServiceMock = new Mock<ICopilotClientService>();
        _sessionServiceMock = new Mock<ISessionService>();

        // Create real tool service
        _toolExecutionService = new ToolExecutionService(_toolServiceLoggerMock.Object);

        // Create controllers with mocked services
        _clientController = new CopilotClientController(_clientServiceMock.Object, _clientControllerLoggerMock.Object);
        _sessionsController = new SessionsController(_sessionServiceMock.Object, _sessionsControllerLoggerMock.Object);
    }

    #endregion

    #region Client Configuration Integration Tests

    [Fact]
    public void FullFlow_ClientConfigurationAndStatus_WorksEndToEnd()
    {
        // Arrange - Setup initial status
        _clientServiceMock.Setup(s => s.GetStatus()).Returns(new ClientStatusResponse
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act - Get status
        var statusResult = _clientController.GetStatus();

        // Assert
        var statusOk = Assert.IsType<OkObjectResult>(statusResult.Result);
        var statusResponse = Assert.IsType<ClientStatusResponse>(statusOk.Value);
        Assert.False(statusResponse.IsConnected);
        Assert.Equal("Disconnected", statusResponse.ConnectionState);
    }

    [Fact]
    public void FullFlow_UpdateConfigAndGetConfig_WorksEndToEnd()
    {
        // Arrange
        var configRequest = new UpdateClientConfigRequest
        {
            CliPath = "/usr/bin/copilot-cli",
            Port = 9999,
            UseStdio = true,
            LogLevel = "debug",
            AutoStart = true,
            AutoRestart = true,
            Cwd = "/app",
            CliArgs = new[] { "--verbose" },
            Environment = new Dictionary<string, string> { { "DEBUG", "true" } }
        };

        _clientServiceMock.Setup(s => s.GetConfig()).Returns(new ClientConfigResponse
        {
            CliPath = "/usr/bin/copilot-cli",
            Port = 9999,
            UseStdio = true,
            LogLevel = "debug",
            AutoStart = true,
            AutoRestart = true,
            Cwd = "/app",
            CliArgs = new[] { "--verbose" },
            Environment = new Dictionary<string, string> { { "DEBUG", "true" } }
        });

        // Act
        var updateResult = _clientController.UpdateConfig(configRequest);

        // Assert config updated
        var updateOk = Assert.IsType<OkObjectResult>(updateResult.Result);
        var configResponse = Assert.IsType<ClientConfigResponse>(updateOk.Value);
        Assert.Equal("/usr/bin/copilot-cli", configResponse.CliPath);
        Assert.Equal(9999, configResponse.Port);
        Assert.True(configResponse.UseStdio);
        Assert.Equal("debug", configResponse.LogLevel);

        // Verify service was called
        _clientServiceMock.Verify(s => s.UpdateConfig(It.IsAny<UpdateClientConfigRequest>()), Times.Once);
    }

    #endregion

    #region Session Creation Integration Tests

    [Fact]
    public async Task FullFlow_SessionCreation_WithBasicConfig_WorksEndToEnd()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            SessionId = "integration-test-session",
            Model = "gpt-5",
            Streaming = true,
            SystemMessage = new SystemMessageConfig
            {
                Mode = "Append",
                Content = "You are a helpful assistant."
            }
        };

        _sessionServiceMock.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "integration-test-session",
                Model = "gpt-5",
                Streaming = true,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var createResult = await _sessionsController.CreateSession(createRequest, _ct);

        // Assert
        var createdSession = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var sessionResponse = Assert.IsType<SessionInfoResponse>(createdSession.Value);
        Assert.Equal("integration-test-session", sessionResponse.SessionId);
        Assert.Equal("gpt-5", sessionResponse.Model);
        Assert.True(sessionResponse.Streaming);
        Assert.Equal("Active", sessionResponse.Status);

        // Verify service was called with correct params
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r => r.SessionId == "integration-test-session" && r.Model == "gpt-5"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullFlow_SessionCreation_WithCustomTools_PassesToolsCorrectly()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "calculator",
                Description = "Performs arithmetic calculations",
                Parameters = new List<ToolParameter>
                {
                    new() { Name = "operation", Type = "string", Required = true },
                    new() { Name = "a", Type = "number", Required = true },
                    new() { Name = "b", Type = "number", Required = true }
                }
            }
        };

        var createRequest = new CreateSessionRequest
        {
            SessionId = "tools-test-session",
            Model = "gpt-5",
            Tools = tools
        };

        _sessionServiceMock.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "tools-test-session",
                Model = "gpt-5",
                Status = "Active"
            });

        // Act
        var createResult = await _sessionsController.CreateSession(createRequest, _ct);

        // Assert
        Assert.IsType<CreatedAtActionResult>(createResult.Result);

        // Verify service received tools
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r => r.Tools != null && r.Tools.Count == 1 && r.Tools[0].Name == "calculator"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullFlow_SessionCreation_WithBYOK_PassesProviderConfig()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            SessionId = "byok-test-session",
            Model = "custom-model",
            Provider = new ProviderConfig
            {
                Type = "openai",
                BaseUrl = "https://api.custom-provider.com/v1",
                ApiKey = "sk-custom-key-12345"
            }
        };

        _sessionServiceMock.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "byok-test-session",
                Model = "custom-model",
                Status = "Active"
            });

        // Act
        var createResult = await _sessionsController.CreateSession(createRequest, _ct);

        // Assert
        var createdSession = Assert.IsType<CreatedAtActionResult>(createResult.Result);

        // Verify service received provider config
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r => r.Provider != null && r.Provider.Type == "openai"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullFlow_SessionCreation_WithSystemMessage_PassesConfig()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            SessionId = "sysmesg-test-session",
            Model = "gpt-5",
            SystemMessage = new SystemMessageConfig
            {
                Mode = "Replace",
                Content = "You are a security expert."
            }
        };

        _sessionServiceMock.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "sysmesg-test-session",
                Model = "gpt-5",
                Status = "Active"
            });

        // Act
        var createResult = await _sessionsController.CreateSession(createRequest, _ct);

        // Assert
        Assert.IsType<CreatedAtActionResult>(createResult.Result);

        // Verify service received system message config
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r => r.SystemMessage != null && r.SystemMessage.Mode == "Replace"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Session Listing and Retrieval Integration Tests

    [Fact]
    public async Task FullFlow_ListSessions_ReturnsAllSessions()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.ListSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionListResponse
            {
                Sessions = new List<SessionInfoResponse>
                {
                    new() { SessionId = "session-1", Model = "gpt-5", Status = "Active" },
                    new() { SessionId = "session-2", Model = "gpt-5", Status = "Active" },
                    new() { SessionId = "session-3", Model = "claude-4.5", Status = "Idle" }
                },
                TotalCount = 3
            });

        // Act
        var listResult = await _sessionsController.ListSessions(_ct);

        // Assert
        var listOk = Assert.IsType<OkObjectResult>(listResult.Result);
        var sessionList = Assert.IsType<SessionListResponse>(listOk.Value);
        Assert.Equal(3, sessionList.Sessions.Count);
        Assert.Equal(3, sessionList.TotalCount);
    }

    [Fact]
    public async Task FullFlow_GetSession_ReturnsSessionDetails()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.GetSessionAsync("test-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "test-session",
                Model = "gpt-5",
                Streaming = true,
                Status = "Active",
                MessageCount = 5
            });

        // Act
        var getResult = await _sessionsController.GetSession("test-session", _ct);

        // Assert
        var getOk = Assert.IsType<OkObjectResult>(getResult.Result);
        var session = Assert.IsType<SessionInfoResponse>(getOk.Value);
        Assert.Equal("test-session", session.SessionId);
        Assert.Equal(5, session.MessageCount);
    }

    [Fact]
    public async Task FullFlow_GetNonExistentSession_ReturnsNotFound()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.GetSessionAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionInfoResponse?)null);

        // Act
        var getResult = await _sessionsController.GetSession("non-existent", _ct);

        // Assert
        Assert.IsType<NotFoundObjectResult>(getResult.Result);
    }

    #endregion

    #region Session Deletion Integration Tests

    [Fact]
    public async Task FullFlow_DeleteSession_ReturnsNoContent()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.DeleteSessionAsync("to-delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var deleteResult = await _sessionsController.DeleteSession("to-delete", _ct);

        // Assert
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify service was called
        _sessionServiceMock.Verify(s => s.DeleteSessionAsync("to-delete", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullFlow_DeleteNonExistentSession_ReturnsNotFound()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.DeleteSessionAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var deleteResult = await _sessionsController.DeleteSession("non-existent", _ct);

        // Assert
        Assert.IsType<NotFoundObjectResult>(deleteResult);
    }

    #endregion

    #region Session Resume Integration Tests

    [Fact]
    public async Task FullFlow_SessionResume_PassesConfiguration()
    {
        // Arrange
        var resumeRequest = new ResumeSessionRequest
        {
            Streaming = true,
            Tools = new List<ToolDefinition>
            {
                new() { Name = "resume_tool", Description = "Tool for resume" }
            }
        };

        _sessionServiceMock.Setup(s => s.ResumeSessionAsync("resume-session", It.IsAny<ResumeSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "resume-session",
                Model = "gpt-5",
                Streaming = true,
                Status = "Active"
            });

        // Act
        var resumeResult = await _sessionsController.ResumeSession("resume-session", resumeRequest, _ct);

        // Assert
        var resumeOk = Assert.IsType<OkObjectResult>(resumeResult.Result);
        var session = Assert.IsType<SessionInfoResponse>(resumeOk.Value);
        Assert.Equal("resume-session", session.SessionId);
        Assert.True(session.Streaming);

        // Verify service received resume request
        _sessionServiceMock.Verify(s => s.ResumeSessionAsync(
            "resume-session",
            It.Is<ResumeSessionRequest>(r => r.Streaming == true && r.Tools != null && r.Tools.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Messaging Integration Tests

    [Fact]
    public async Task FullFlow_SendMessage_PassesRequest()
    {
        // Arrange
        var messageRequest = new SendMessageRequest
        {
            Prompt = "Hello, Copilot!",
            Mode = "enqueue",
            Attachments = new List<MessageAttachmentDto>
            {
                new() { Type = "file", Path = "/path/to/file.cs" }
            }
        };

        _sessionServiceMock.Setup(s => s.SendMessageAsync("msg-session", It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                MessageId = "msg-123",
                Success = true
            });

        // Act
        var sendResult = await _sessionsController.SendMessage("msg-session", messageRequest, _ct);

        // Assert
        var sendOk = Assert.IsType<OkObjectResult>(sendResult.Result);
        var response = Assert.IsType<SendMessageResponse>(sendOk.Value);
        Assert.True(response.Success);
        Assert.Equal("msg-123", response.MessageId);

        // Verify service received message
        _sessionServiceMock.Verify(s => s.SendMessageAsync(
            "msg-session",
            It.Is<SendMessageRequest>(r => r.Prompt == "Hello, Copilot!" && r.Attachments!.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullFlow_SendMessageToNonExistentSession_ThrowsKeyNotFoundException()
    {
        // Arrange - Controller doesn't catch KeyNotFoundException (middleware handles it)
        _sessionServiceMock.Setup(s => s.SendMessageAsync("non-existent", It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Session not found"));

        // Act & Assert - Exception propagates up (middleware would convert to 404)
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sessionsController.SendMessage("non-existent", new SendMessageRequest { Prompt = "test" }, _ct));
    }

    [Fact]
    public async Task FullFlow_GetMessages_ReturnsEvents()
    {
        // Arrange - Need to mock both GetSessionAsync (for existence check) and GetMessagesAsync
        _sessionServiceMock.Setup(s => s.GetSessionAsync("msg-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse { SessionId = "msg-session", Status = "Active" });

        _sessionServiceMock.Setup(s => s.GetMessagesAsync("msg-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessagesResponse
            {
                Events = new List<SessionEventDto>
                {
                    new() { Type = "UserMessage", Timestamp = DateTime.UtcNow, Data = new { content = "Hello" } },
                    new() { Type = "AssistantMessage", Timestamp = DateTime.UtcNow, Data = new { content = "Hi there!" } }
                }
            });

        // Act
        var getResult = await _sessionsController.GetMessages("msg-session", _ct);

        // Assert
        var getOk = Assert.IsType<OkObjectResult>(getResult.Result);
        var response = Assert.IsType<MessagesResponse>(getOk.Value);
        Assert.Equal(2, response.Events.Count);
    }

    [Fact]
    public async Task FullFlow_AbortSession_CallsService()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.AbortAsync("abort-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var abortResult = await _sessionsController.AbortSession("abort-session", _ct);

        // Assert - Controller returns NoContent on success
        Assert.IsType<NoContentResult>(abortResult);

        // Verify service was called
        _sessionServiceMock.Verify(s => s.AbortAsync("abort-session", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Tool Execution Integration Tests

    [Fact]
    public async Task FullFlow_DemoTools_EchoTool_ExecutesCorrectly()
    {
        // Arrange
        DemoTools.RegisterAllDemoTools(_toolExecutionService);

        // Act
        var result = await _toolExecutionService.ExecuteToolAsync("echo_tool", new Dictionary<string, object?>
        {
            { "message", "Hello Integration" },
            { "uppercase", true }
        });

        // Assert
        Assert.NotNull(result);
        var echoResult = Assert.IsType<EchoToolResult>(result);
        Assert.Equal("HELLO INTEGRATION", echoResult.Echoed);
        Assert.Equal(17, echoResult.OriginalLength);
    }

    [Fact]
    public async Task FullFlow_DemoTools_GetCurrentTime_ExecutesCorrectly()
    {
        // Arrange
        DemoTools.RegisterAllDemoTools(_toolExecutionService);

        // Act
        var result = await _toolExecutionService.ExecuteToolAsync("get_current_time", new Dictionary<string, object?>
        {
            { "format", "yyyy-MM-dd" }
        });

        // Assert
        Assert.NotNull(result);
        var timeResult = Assert.IsType<GetCurrentTimeResult>(result);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", timeResult.CurrentTime);
    }

    [Fact]
    public async Task FullFlow_CustomToolRegistrationAndExecution()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "string_reverse",
            Description = "Reverses a string",
            Parameters = new List<ToolParameter>
            {
                new() { Name = "text", Type = "string", Required = true }
            }
        };

        _toolExecutionService.RegisterTool(definition, (args, ct) =>
        {
            var text = args["text"]?.ToString() ?? "";
            return Task.FromResult<object?>(new string(text.Reverse().ToArray()));
        });

        // Act
        var result = await _toolExecutionService.ExecuteToolAsync("string_reverse", new Dictionary<string, object?>
        {
            { "text", "integration" }
        });

        // Assert
        Assert.Equal("noitargetni", result);
    }

    [Fact]
    public void FullFlow_BuildAIFunctions_CreatesCorrectFunctions()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new()
            {
                Name = "tool1",
                Description = "First tool",
                Parameters = new List<ToolParameter>
                {
                    new() { Name = "param1", Type = "string", Required = true }
                }
            },
            new()
            {
                Name = "tool2",
                Description = "Second tool"
            }
        };

        // Act
        var aiFunctions = _toolExecutionService.BuildAIFunctions(tools);

        // Assert
        Assert.Equal(2, aiFunctions.Count);
        Assert.Contains(aiFunctions, f => f.Name == "tool1");
        Assert.Contains(aiFunctions, f => f.Name == "tool2");
    }

    [Fact]
    public async Task FullFlow_ToolExecutionWithMissingTool_ThrowsKeyNotFound()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _toolExecutionService.ExecuteToolAsync("non_existent_tool", new Dictionary<string, object?>()));

        Assert.Contains("not registered", exception.Message);
    }

    [Fact]
    public async Task FullFlow_ToolExecutionWithCancellation_HonorsToken()
    {
        // Arrange
        var definition = new ToolDefinition
        {
            Name = "cancellable_tool",
            Description = "Tool that supports cancellation"
        };

        _toolExecutionService.RegisterTool(definition, async (args, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1000, ct);
            return "completed";
        });

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _toolExecutionService.ExecuteToolAsync("cancellable_tool", new Dictionary<string, object?>(), cts.Token));
    }

    #endregion

    #region Full Configuration Integration Tests

    [Fact]
    public async Task FullFlow_FullyConfiguredSession_AllOptionsPassedCorrectly()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            SessionId = "fully-configured-session",
            Model = "claude-sonnet-4.5",
            Streaming = true,
            SystemMessage = new SystemMessageConfig
            {
                Mode = "Append",
                Content = "<context>Enterprise assistant</context>"
            },
            AvailableTools = new List<string> { "code_analysis", "file_read" },
            ExcludedTools = new List<string> { "file_delete" },
            Tools = new List<ToolDefinition>
            {
                new()
                {
                    Name = "enterprise_lookup",
                    Description = "Enterprise data lookup",
                    Parameters = new List<ToolParameter>
                    {
                        new() { Name = "query", Type = "string", Required = true }
                    }
                }
            },
            Provider = new ProviderConfig
            {
                Type = "azure",
                BaseUrl = "https://enterprise.azure-api.net/v1",
                ApiKey = "azure-api-key"
            }
        };

        _sessionServiceMock.Setup(s => s.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfoResponse
            {
                SessionId = "fully-configured-session",
                Model = "claude-sonnet-4.5",
                Streaming = true,
                Status = "Active"
            });

        // Act
        var createResult = await _sessionsController.CreateSession(createRequest, _ct);

        // Assert
        var createdSession = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var sessionResponse = Assert.IsType<SessionInfoResponse>(createdSession.Value);
        Assert.Equal("fully-configured-session", sessionResponse.SessionId);
        Assert.Equal("claude-sonnet-4.5", sessionResponse.Model);
        Assert.True(sessionResponse.Streaming);

        // Verify all options were passed to service
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r =>
                r.SessionId == "fully-configured-session" &&
                r.Model == "claude-sonnet-4.5" &&
                r.Streaming == true &&
                r.SystemMessage != null &&
                r.SystemMessage.Mode == "Append" &&
                r.AvailableTools != null &&
                r.AvailableTools.Count == 2 &&
                r.ExcludedTools != null &&
                r.ExcludedTools.Count == 1 &&
                r.Tools != null &&
                r.Tools.Count == 1 &&
                r.Provider != null &&
                r.Provider.Type == "azure"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
