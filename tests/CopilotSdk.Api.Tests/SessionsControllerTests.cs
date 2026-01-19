using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for SessionsController.
/// </summary>
public class SessionsControllerTests
{
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger<SessionsController>> _loggerMock;
    private readonly SessionsController _controller;

    public SessionsControllerTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger<SessionsController>>();
        _controller = new SessionsController(_sessionServiceMock.Object, _loggerMock.Object);
    }

    #region ListSessions Tests

    [Fact]
    public async Task ListSessions_ReturnsOkResult_WithSessionList()
    {
        // Arrange
        var expectedResponse = new SessionListResponse
        {
            Sessions = new List<SessionInfoResponse>
            {
                new() { SessionId = "session-1", Model = "gpt-4", Status = "Active" },
                new() { SessionId = "session-2", Model = "gpt-3.5-turbo", Status = "Active" }
            },
            TotalCount = 2
        };
        _sessionServiceMock.Setup(s => s.ListSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ListSessions(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<SessionListResponse>(okResult.Value);
        Assert.Equal(2, actualResponse.TotalCount);
        Assert.Equal(2, actualResponse.Sessions.Count);
    }

    [Fact]
    public async Task ListSessions_ReturnsEmptyList_WhenNoSessions()
    {
        // Arrange
        var expectedResponse = new SessionListResponse
        {
            Sessions = new List<SessionInfoResponse>(),
            TotalCount = 0
        };
        _sessionServiceMock.Setup(s => s.ListSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ListSessions(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<SessionListResponse>(okResult.Value);
        Assert.Empty(actualResponse.Sessions);
        Assert.Equal(0, actualResponse.TotalCount);
    }

    #endregion

    #region CreateSession Tests

    [Fact]
    public async Task CreateSession_ReturnsCreatedResult_WithSessionInfo()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Model = "gpt-4",
            Streaming = true
        };
        var expectedResponse = new SessionInfoResponse
        {
            SessionId = "new-session-123",
            Model = "gpt-4",
            Streaming = true,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        _sessionServiceMock.Setup(s => s.CreateSessionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateSession(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetSession", createdResult.ActionName);
        var actualResponse = Assert.IsType<SessionInfoResponse>(createdResult.Value);
        Assert.Equal("new-session-123", actualResponse.SessionId);
        Assert.Equal("gpt-4", actualResponse.Model);
        Assert.True(actualResponse.Streaming);
    }

    [Fact]
    public async Task CreateSession_ReturnsBadRequest_WhenModelIsEmpty()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Model = ""
        };

        // Act
        var result = await _controller.CreateSession(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Invalid Request", problemDetails.Title);
        Assert.Contains("Model is required", problemDetails.Detail);
    }

    [Fact]
    public async Task CreateSession_ReturnsBadRequest_WhenModelIsWhitespace()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            Model = "   "
        };

        // Act
        var result = await _controller.CreateSession(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Model is required", problemDetails.Detail);
    }

    [Fact]
    public async Task CreateSession_SetsCorrectRouteValues()
    {
        // Arrange
        var request = new CreateSessionRequest { Model = "gpt-4" };
        var expectedResponse = new SessionInfoResponse
        {
            SessionId = "session-abc",
            Model = "gpt-4"
        };
        _sessionServiceMock.Setup(s => s.CreateSessionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateSession(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("session-abc", createdResult.RouteValues?["sessionId"]);
    }

    #endregion

    #region GetSession Tests

    [Fact]
    public async Task GetSession_ReturnsOkResult_WhenSessionExists()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedResponse = new SessionInfoResponse
        {
            SessionId = sessionId,
            Model = "gpt-4",
            Status = "Active"
        };
        _sessionServiceMock.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSession(sessionId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<SessionInfoResponse>(okResult.Value);
        Assert.Equal(sessionId, actualResponse.SessionId);
    }

    [Fact]
    public async Task GetSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        _sessionServiceMock.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionInfoResponse?)null);

        // Act
        var result = await _controller.GetSession(sessionId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Session Not Found", problemDetails.Title);
        Assert.Contains(sessionId, problemDetails.Detail);
    }

    #endregion

    #region DeleteSession Tests

    [Fact]
    public async Task DeleteSession_ReturnsNoContent_WhenSessionDeleted()
    {
        // Arrange
        var sessionId = "session-to-delete";
        _sessionServiceMock.Setup(s => s.DeleteSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSession(sessionId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        _sessionServiceMock.Setup(s => s.DeleteSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteSession(sessionId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Session Not Found", problemDetails.Title);
    }

    [Fact]
    public async Task DeleteSession_CallsServiceWithCorrectSessionId()
    {
        // Arrange
        var sessionId = "specific-session-id";
        _sessionServiceMock.Setup(s => s.DeleteSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteSession(sessionId, CancellationToken.None);

        // Assert
        _sessionServiceMock.Verify(s => s.DeleteSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ResumeSession Tests

    [Fact]
    public async Task ResumeSession_ReturnsOkResult_WhenSessionResumed()
    {
        // Arrange
        var sessionId = "session-to-resume";
        var request = new ResumeSessionRequest { Streaming = true };
        var expectedResponse = new SessionInfoResponse
        {
            SessionId = sessionId,
            Model = "gpt-4",
            Streaming = true,
            Status = "Active"
        };
        _sessionServiceMock.Setup(s => s.ResumeSessionAsync(sessionId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResumeSession(sessionId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<SessionInfoResponse>(okResult.Value);
        Assert.Equal(sessionId, actualResponse.SessionId);
        Assert.True(actualResponse.Streaming);
    }

    [Fact]
    public async Task ResumeSession_ReturnsOkResult_WithNullRequest()
    {
        // Arrange
        var sessionId = "session-to-resume";
        var expectedResponse = new SessionInfoResponse
        {
            SessionId = sessionId,
            Model = "gpt-4",
            Status = "Active"
        };
        _sessionServiceMock.Setup(s => s.ResumeSessionAsync(sessionId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResumeSession(sessionId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<SessionInfoResponse>(okResult.Value);
        Assert.Equal(sessionId, actualResponse.SessionId);
    }

    [Fact]
    public async Task ResumeSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        var request = new ResumeSessionRequest();
        _sessionServiceMock.Setup(s => s.ResumeSessionAsync(sessionId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Session does not exist"));

        // Act
        var result = await _controller.ResumeSession(sessionId, request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains(sessionId, problemDetails.Detail);
    }

    #endregion

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_ReturnsOkResult_WhenMessageSent()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new SendMessageRequest { Prompt = "Hello, world!" };
        var expectedResponse = new SendMessageResponse
        {
            SessionId = sessionId,
            MessageId = "msg-123",
            Success = true
        };
        _sessionServiceMock.Setup(s => s.SendMessageAsync(sessionId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(sessionId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<SendMessageResponse>(okResult.Value);
        Assert.True(actualResponse.Success);
        Assert.Equal("msg-123", actualResponse.MessageId);
    }

    [Fact]
    public async Task SendMessage_ReturnsBadRequest_WhenPromptIsEmpty()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new SendMessageRequest { Prompt = "" };

        // Act
        var result = await _controller.SendMessage(sessionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Invalid Request", problemDetails.Title);
        Assert.Contains("Prompt is required", problemDetails.Detail);
    }

    [Fact]
    public async Task SendMessage_ReturnsBadRequest_WhenPromptIsWhitespace()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new SendMessageRequest { Prompt = "   " };

        // Act
        var result = await _controller.SendMessage(sessionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Prompt is required", problemDetails.Detail);
    }

    [Fact]
    public async Task SendMessage_ReturnsNotFound_WhenSessionNotFound()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        var request = new SendMessageRequest { Prompt = "Hello" };
        var expectedResponse = new SendMessageResponse
        {
            SessionId = sessionId,
            Success = false,
            Error = "Session 'nonexistent-session' not found or not active"
        };
        _sessionServiceMock.Setup(s => s.SendMessageAsync(sessionId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(sessionId, request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Session Not Found", problemDetails.Title);
    }

    [Fact]
    public async Task SendMessage_ReturnsBadRequest_WhenSendFails()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new SendMessageRequest { Prompt = "Hello" };
        var expectedResponse = new SendMessageResponse
        {
            SessionId = sessionId,
            Success = false,
            Error = "Some other error"
        };
        _sessionServiceMock.Setup(s => s.SendMessageAsync(sessionId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(sessionId, request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Send Message Failed", problemDetails.Title);
    }

    #endregion

    #region GetMessages Tests

    [Fact]
    public async Task GetMessages_ReturnsOkResult_WithMessages()
    {
        // Arrange
        var sessionId = "test-session";
        var sessionInfo = new SessionInfoResponse { SessionId = sessionId, Model = "gpt-4" };
        var expectedResponse = new MessagesResponse
        {
            SessionId = sessionId,
            Events = new List<Models.Domain.SessionEventDto>
            {
                new() { Id = Guid.NewGuid(), Type = "user.message" },
                new() { Id = Guid.NewGuid(), Type = "assistant.message" }
            },
            TotalCount = 2
        };
        _sessionServiceMock.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionInfo);
        _sessionServiceMock.Setup(s => s.GetMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMessages(sessionId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<MessagesResponse>(okResult.Value);
        Assert.Equal(2, actualResponse.TotalCount);
        Assert.Equal(2, actualResponse.Events.Count);
    }

    [Fact]
    public async Task GetMessages_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        _sessionServiceMock.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionInfoResponse?)null);

        // Act
        var result = await _controller.GetMessages(sessionId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Session Not Found", problemDetails.Title);
    }

    [Fact]
    public async Task GetMessages_ReturnsEmptyList_WhenNoMessages()
    {
        // Arrange
        var sessionId = "empty-session";
        var sessionInfo = new SessionInfoResponse { SessionId = sessionId, Model = "gpt-4" };
        var expectedResponse = new MessagesResponse
        {
            SessionId = sessionId,
            Events = new List<Models.Domain.SessionEventDto>(),
            TotalCount = 0
        };
        _sessionServiceMock.Setup(s => s.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionInfo);
        _sessionServiceMock.Setup(s => s.GetMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMessages(sessionId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<MessagesResponse>(okResult.Value);
        Assert.Empty(actualResponse.Events);
        Assert.Equal(0, actualResponse.TotalCount);
    }

    #endregion

    #region AbortSession Tests

    [Fact]
    public async Task AbortSession_ReturnsNoContent_WhenAbortSucceeds()
    {
        // Arrange
        var sessionId = "test-session";
        _sessionServiceMock.Setup(s => s.AbortAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AbortSession(sessionId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AbortSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        _sessionServiceMock.Setup(s => s.AbortAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AbortSession(sessionId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Session Not Found", problemDetails.Title);
    }

    [Fact]
    public async Task AbortSession_CallsServiceWithCorrectSessionId()
    {
        // Arrange
        var sessionId = "specific-session";
        _sessionServiceMock.Setup(s => s.AbortAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.AbortSession(sessionId, CancellationToken.None);

        // Assert
        _sessionServiceMock.Verify(s => s.AbortAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
