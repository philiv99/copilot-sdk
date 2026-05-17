using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Hubs;
using CopilotSdk.Api.Models.Domain;
using GitHub.Copilot.SDK;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for SessionEventDispatcher.
/// </summary>
public class SessionEventDispatcherTests
{
    private readonly Mock<IHubContext<SessionHub>> _hubContextMock;
    private readonly Mock<ILogger<SessionEventDispatcher>> _loggerMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly SessionEventDispatcher _dispatcher;

    public SessionEventDispatcherTests()
    {
        _hubContextMock = new Mock<IHubContext<SessionHub>>();
        _loggerMock = new Mock<ILogger<SessionEventDispatcher>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(_hubClientsMock.Object);

        _dispatcher = new SessionEventDispatcher(_hubContextMock.Object, _loggerMock.Object);
    }

    private static async Task WaitForAsync(Func<bool> predicate, int timeoutMs = 1000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate()) return;
            await Task.Delay(10);
        }
    }

    #region CreateHandler Tests

    [Fact]
    public void CreateHandler_ReturnsNonNullHandler()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var handler = _dispatcher.CreateHandler(sessionId);

        // Assert
        Assert.NotNull(handler);
    }

    #endregion

    #region DispatchEventAsync Tests - Session Events

    [Fact]
    public async Task DispatchEventAsync_SessionIdleEvent_SendsToCorrectGroup()
    {
        // Arrange
        var sessionId = "test-session";
        var expectedGroup = $"session-{sessionId}";
        SessionEventDto? capturedDto = null;
        string? capturedSessionId = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                var dto = args[1] as SessionEventDto;
                if (dto?.Type == "session.idle")
                {
                    capturedSessionId = args[0] as string;
                    capturedDto = dto;
                }
            })
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, CreateSessionIdleEvent());

        // Assert
        _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.AtLeastOnce);
        Assert.Equal(sessionId, capturedSessionId);
        Assert.NotNull(capturedDto);
        Assert.Equal("session.idle", capturedDto.Type);
    }

    [Fact]
    public async Task DispatchEventAsync_SessionStartEvent_MapsDataCorrectly()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[1] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        var startEvent = CreateSessionStartEvent("test-session", "gpt-4");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, startEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("session.start", capturedDto.Type);
        var data = Assert.IsType<SessionStartDataDto>(capturedDto.Data);
        Assert.Equal("test-session", data.SessionId);
        Assert.Equal("gpt-4", data.SelectedModel);
    }

    [Fact]
    public async Task DispatchEventAsync_SessionErrorEvent_MapsDataCorrectly()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                var dto = args[1] as SessionEventDto;
                if (dto?.Type == "session.error") capturedDto = dto;
            })
            .Returns(Task.CompletedTask);

        var errorEvent = CreateSessionErrorEvent("TestError", "Something went wrong");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, errorEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("session.error", capturedDto.Type);
        var data = Assert.IsType<SessionErrorDataDto>(capturedDto.Data);
        Assert.Equal("TestError", data.ErrorType);
        Assert.Equal("Something went wrong", data.Message);
    }

    #endregion

    #region DispatchEventAsync Tests - Message Events

    [Fact]
    public async Task DispatchEventAsync_UserMessageEvent_MapsDataCorrectly()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[1] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        var userMessageEvent = CreateUserMessageEvent("Hello, world!");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, userMessageEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("user.message", capturedDto.Type);
        var data = Assert.IsType<UserMessageDataDto>(capturedDto.Data);
        Assert.Equal("Hello, world!", data.Content);
    }

    [Fact]
    public async Task DispatchEventAsync_AssistantMessageEvent_MapsDataCorrectly()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[1] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        var assistantMessageEvent = CreateAssistantMessageEvent("msg-123", "Hello! How can I help?");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, assistantMessageEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("assistant.message", capturedDto.Type);
        var data = Assert.IsType<AssistantMessageDataDto>(capturedDto.Data);
        Assert.Equal("msg-123", data.MessageId);
        Assert.Equal("Hello! How can I help?", data.Content);
    }

    #endregion

    #region DispatchEventAsync Tests - Delta Events (Streaming)

    [Fact]
    public async Task DispatchEventAsync_AssistantMessageDeltaEvent_SendsToStreamingChannel()
    {
        // Arrange
        var sessionId = "test-session";
        var expectedGroup = $"session-{sessionId}";
        StreamingDeltaDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnStreamingDelta", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[0] as StreamingDeltaDto;
            })
            .Returns(Task.CompletedTask);

        var deltaEvent = CreateAssistantMessageDeltaEvent("msg-123", "Hello");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, deltaEvent);

        // Assert
        _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.NotNull(capturedDto);
        Assert.Equal("message", capturedDto.Type);
        Assert.Equal("msg-123", capturedDto.Id);
        Assert.Equal("Hello", capturedDto.Content);
        Assert.Equal(sessionId, capturedDto.SessionId);
    }

    [Fact]
    public async Task DispatchEventAsync_AssistantReasoningDeltaEvent_SendsToStreamingChannel()
    {
        // Arrange
        var sessionId = "test-session";
        StreamingDeltaDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnStreamingDelta", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[0] as StreamingDeltaDto;
            })
            .Returns(Task.CompletedTask);

        var deltaEvent = CreateAssistantReasoningDeltaEvent("reasoning-123", "Thinking...");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, deltaEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("reasoning", capturedDto.Type);
        Assert.Equal("reasoning-123", capturedDto.Id);
        Assert.Equal("Thinking...", capturedDto.Content);
        Assert.Equal(sessionId, capturedDto.SessionId);
    }

    #endregion

    #region DispatchEventAsync Tests - Tool Events

    [Fact]
    public async Task DispatchEventAsync_ToolExecutionStartEvent_MapsDataCorrectly()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                var dto = args[1] as SessionEventDto;
                if (dto?.Type == "tool.execution_start") capturedDto = dto;
            })
            .Returns(Task.CompletedTask);

        var toolEvent = CreateToolExecutionStartEvent("call-123", "search_files");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, toolEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("tool.execution_start", capturedDto.Type);
        var data = Assert.IsType<ToolExecutionStartDataDto>(capturedDto.Data);
        Assert.Equal("call-123", data.ToolCallId);
        Assert.Equal("search_files", data.ToolName);
    }

    [Fact]
    public async Task DispatchEventAsync_ToolExecutionCompleteEvent_MapsDataCorrectly()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                var dto = args[1] as SessionEventDto;
                if (dto?.Type == "tool.execution_complete") capturedDto = dto;
            })
            .Returns(Task.CompletedTask);

        var toolEvent = CreateToolExecutionCompleteEvent("call-123", "Found 5 files");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, toolEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("tool.execution_complete", capturedDto.Type);
        var data = Assert.IsType<ToolExecutionCompleteDataDto>(capturedDto.Data);
        Assert.Equal("call-123", data.ToolCallId);
        Assert.Equal("Found 5 files", data.Result);
    }

    #endregion

    #region CreateRelayHandler Tests

    [Fact]
    public async Task CreateRelayHandler_ProjectsAssistantMessage_OnParentGroup_WithAgentId()
    {
        // Arrange
        var parentSessionId = "parent-1";
        var agentId = "coder";
        SessionEventDto? capturedDto = null;
        string? capturedSessionId = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedSessionId = args[0] as string;
                capturedDto = args[1] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        var handler = _dispatcher.CreateRelayHandler(parentSessionId, agentId);

        // Act
        handler(CreateAssistantMessageEvent("msg-x", "child output"));
        // Handler is fire-and-forget; give the background task a moment to run.
        await WaitForAsync(() => capturedDto != null);

        // Assert: events were routed to the PARENT group, not the child's, and tagged with the agent id.
        _hubClientsMock.Verify(c => c.Group($"session-{parentSessionId}"), Times.AtLeastOnce);
        Assert.Equal(parentSessionId, capturedSessionId);
        Assert.NotNull(capturedDto);
        Assert.Equal(agentId, capturedDto.AgentId);
    }

    [Fact]
    public async Task CreateRelayHandler_ProjectsStreamingDelta_OnParentGroup_WithAgentId()
    {
        var parentSessionId = "parent-2";
        var agentId = "code-reviewer";
        StreamingDeltaDto? capturedDelta = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnStreamingDelta", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDelta = args[0] as StreamingDeltaDto;
            })
            .Returns(Task.CompletedTask);

        var handler = _dispatcher.CreateRelayHandler(parentSessionId, agentId);

        handler(CreateAssistantMessageDeltaEvent("msg-y", "chunk"));
        await WaitForAsync(() => capturedDelta != null);

        _hubClientsMock.Verify(c => c.Group($"session-{parentSessionId}"), Times.AtLeastOnce);
        Assert.NotNull(capturedDelta);
        Assert.Equal(parentSessionId, capturedDelta.SessionId);
        Assert.Equal(agentId, capturedDelta.AgentId);
    }

    [Fact]
    public async Task CreateRelayHandler_ToolStart_EmitsProgressWithAgentAndCommandSummary()
    {
        var parentSessionId = "parent-3";
        var agentId = "coder";
        SessionEventDto? capturedProgress = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                var dto = args[1] as SessionEventDto;
                if (dto?.Type == "session.progress")
                {
                    capturedProgress = dto;
                }
            })
            .Returns(Task.CompletedTask);

        var handler = _dispatcher.CreateRelayHandler(parentSessionId, agentId);

        handler(CreateToolExecutionStartEvent(
            "call-456",
            "run_in_terminal",
            new Dictionary<string, object?> { ["command"] = "git init" }));

        await WaitForAsync(() => capturedProgress != null);

        Assert.NotNull(capturedProgress);
        Assert.Equal("session.progress", capturedProgress.Type);
        Assert.Equal(agentId, capturedProgress.AgentId);
        var data = Assert.IsType<SessionProgressDataDto>(capturedProgress.Data);
        Assert.Equal(agentId, data.AgentId);
        Assert.Equal("tool", data.Phase);
        Assert.Equal("run_in_terminal", data.ToolName);
        Assert.Equal("call-456", data.ToolCallId);
        Assert.Contains("git init", data.Message);
    }

    [Fact]
    public void ExtractToolActionSummary_ReadsCommandArgument()
    {
        var summary = SessionEventDispatcher.ExtractToolActionSummary(
            new Dictionary<string, object?> { ["Command"] = "npm create vite@latest my-app -- --template react-ts" });

        Assert.Equal("npm create vite@latest my-app -- --template react-ts", summary);
    }

    #endregion

    #region Helper Methods for Creating Test Events

    private static SessionIdleEvent CreateSessionIdleEvent()
    {
        return new SessionIdleEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new SessionIdleData()
        };
    }

    private static SessionStartEvent CreateSessionStartEvent(string sessionId, string? selectedModel = null)
    {
        return new SessionStartEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new SessionStartData
            {
                SessionId = sessionId,
                Version = 1.0,
                Producer = "test",
                CopilotVersion = "1.0.0",
                StartTime = DateTimeOffset.UtcNow,
                SelectedModel = selectedModel ?? "gpt-4"
            }
        };
    }

    private static SessionErrorEvent CreateSessionErrorEvent(string errorType, string message)
    {
        return new SessionErrorEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new SessionErrorData
            {
                ErrorType = errorType,
                Message = message
            }
        };
    }

    private static UserMessageEvent CreateUserMessageEvent(string content)
    {
        return new UserMessageEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new UserMessageData
            {
                Content = content
            }
        };
    }

    private static AssistantMessageEvent CreateAssistantMessageEvent(string messageId, string content)
    {
        return new AssistantMessageEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new AssistantMessageData
            {
                MessageId = messageId,
                Content = content
            }
        };
    }

    private static AssistantMessageDeltaEvent CreateAssistantMessageDeltaEvent(string messageId, string deltaContent)
    {
        return new AssistantMessageDeltaEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new AssistantMessageDeltaData
            {
                MessageId = messageId,
                DeltaContent = deltaContent
            }
        };
    }

    private static AssistantReasoningDeltaEvent CreateAssistantReasoningDeltaEvent(string reasoningId, string deltaContent)
    {
        return new AssistantReasoningDeltaEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new AssistantReasoningDeltaData
            {
                ReasoningId = reasoningId,
                DeltaContent = deltaContent
            }
        };
    }

    private static ToolExecutionStartEvent CreateToolExecutionStartEvent(string toolCallId, string toolName, object? arguments = null)
    {
        return new ToolExecutionStartEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new ToolExecutionStartData
            {
                ToolCallId = toolCallId,
                ToolName = toolName,
                Arguments = arguments
            }
        };
    }

    private static ToolExecutionCompleteEvent CreateToolExecutionCompleteEvent(string toolCallId, string result)
    {
        return new ToolExecutionCompleteEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new ToolExecutionCompleteData
            {
                ToolCallId = toolCallId,
                Success = true,
                Result = new ToolExecutionCompleteDataResult
                {
                    Content = result
                }
            }
        };
    }

    #endregion
}
