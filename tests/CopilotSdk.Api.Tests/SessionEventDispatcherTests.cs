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

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnSessionEvent", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[0] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, CreateSessionIdleEvent());

        // Assert
        _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.Once);
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
                capturedDto = args[0] as SessionEventDto;
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
                capturedDto = args[0] as SessionEventDto;
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
                capturedDto = args[0] as SessionEventDto;
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
                capturedDto = args[0] as SessionEventDto;
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
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnStreamingDelta", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[0] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        var deltaEvent = CreateAssistantMessageDeltaEvent("msg-123", "Hello");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, deltaEvent);

        // Assert
        _hubClientsMock.Verify(c => c.Group(expectedGroup), Times.Once);
        Assert.NotNull(capturedDto);
        Assert.Equal("assistant.message_delta", capturedDto.Type);
    }

    [Fact]
    public async Task DispatchEventAsync_AssistantReasoningDeltaEvent_SendsToStreamingChannel()
    {
        // Arrange
        var sessionId = "test-session";
        SessionEventDto? capturedDto = null;

        _clientProxyMock
            .Setup(c => c.SendCoreAsync("OnStreamingDelta", It.IsAny<object[]>(), default))
            .Callback<string, object[], CancellationToken>((_, args, _) =>
            {
                capturedDto = args[0] as SessionEventDto;
            })
            .Returns(Task.CompletedTask);

        var deltaEvent = CreateAssistantReasoningDeltaEvent("reasoning-123", "Thinking...");

        // Act
        await _dispatcher.DispatchEventAsync(sessionId, deltaEvent);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("assistant.reasoning_delta", capturedDto.Type);
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
                capturedDto = args[0] as SessionEventDto;
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
                capturedDto = args[0] as SessionEventDto;
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

    private static ToolExecutionStartEvent CreateToolExecutionStartEvent(string toolCallId, string toolName)
    {
        return new ToolExecutionStartEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Data = new ToolExecutionStartData
            {
                ToolCallId = toolCallId,
                ToolName = toolName
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
