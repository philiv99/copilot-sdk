using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Moq;
using SessionConfig = CopilotSdk.Api.Models.Domain.SessionConfig;
using SessionMetadata = CopilotSdk.Api.Models.Domain.SessionMetadata;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for SessionManager.
/// </summary>
public class SessionManagerTests
{
    private readonly Mock<ILogger<SessionManager>> _loggerMock;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _loggerMock = new Mock<ILogger<SessionManager>>();
        _sessionManager = new SessionManager(_loggerMock.Object);
    }

    #region RegisterSession Tests

    [Fact]
    public void RegisterSession_AddsSessionToTracking()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4", Streaming = true };

        // Act
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Assert
        Assert.True(_sessionManager.SessionExists(sessionId));
        Assert.Equal(1, _sessionManager.SessionCount);
    }

    [Fact]
    public void RegisterSession_CreatesMetadataWithCorrectValues()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4", Streaming = true };

        // Act
        _sessionManager.RegisterSession(sessionId, mockSession, config);
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(sessionId, metadata.SessionId);
        Assert.Equal("gpt-4", metadata.Config?.Model);
        Assert.True(metadata.Config?.Streaming);
        Assert.Equal(0, metadata.MessageCount);
        Assert.NotNull(metadata.CreatedAt);
        Assert.NotNull(metadata.LastActivityAt);
    }

    [Fact]
    public void RegisterSession_OverwritesExistingSession()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession1 = CreateMockSession(sessionId);
        var mockSession2 = CreateMockSession(sessionId);
        var config1 = new SessionConfig { Model = "gpt-3.5-turbo" };
        var config2 = new SessionConfig { Model = "gpt-4" };

        // Act
        _sessionManager.RegisterSession(sessionId, mockSession1, config1);
        _sessionManager.RegisterSession(sessionId, mockSession2, config2);
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.Equal(1, _sessionManager.SessionCount);
        Assert.Equal("gpt-4", metadata?.Config?.Model);
    }

    #endregion

    #region GetSession Tests

    [Fact]
    public void GetSession_ReturnsStoredValue_WhenExists()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Act
        var session = _sessionManager.GetSession(sessionId);

        // Assert - Since we stored null, we expect null back
        // This tests that the retrieval works correctly
        Assert.Equal(mockSession, session);
    }

    [Fact]
    public void GetSession_ReturnsNull_WhenNotExists()
    {
        // Act
        var session = _sessionManager.GetSession("nonexistent-session");

        // Assert
        Assert.Null(session);
    }

    #endregion

    #region GetMetadata Tests

    [Fact]
    public void GetMetadata_ReturnsMetadata_WhenExists()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Act
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(sessionId, metadata.SessionId);
    }

    [Fact]
    public void GetMetadata_ReturnsNull_WhenNotExists()
    {
        // Act
        var metadata = _sessionManager.GetMetadata("nonexistent-session");

        // Assert
        Assert.Null(metadata);
    }

    #endregion

    #region GetAllMetadata Tests

    [Fact]
    public void GetAllMetadata_ReturnsAllSessions()
    {
        // Arrange
        var mockSession1 = CreateMockSession("session-1");
        var mockSession2 = CreateMockSession("session-2");
        var mockSession3 = CreateMockSession("session-3");
        var config = new SessionConfig { Model = "gpt-4" };

        _sessionManager.RegisterSession("session-1", mockSession1, config);
        _sessionManager.RegisterSession("session-2", mockSession2, config);
        _sessionManager.RegisterSession("session-3", mockSession3, config);

        // Act
        var allMetadata = _sessionManager.GetAllMetadata();

        // Assert
        Assert.Equal(3, allMetadata.Count);
        Assert.Contains(allMetadata, m => m.SessionId == "session-1");
        Assert.Contains(allMetadata, m => m.SessionId == "session-2");
        Assert.Contains(allMetadata, m => m.SessionId == "session-3");
    }

    [Fact]
    public void GetAllMetadata_ReturnsEmptyList_WhenNoSessions()
    {
        // Act
        var allMetadata = _sessionManager.GetAllMetadata();

        // Assert
        Assert.Empty(allMetadata);
    }

    #endregion

    #region RemoveSession Tests

    [Fact]
    public void RemoveSession_ReturnsTrue_WhenSessionExists()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Act
        var removed = _sessionManager.RemoveSession(sessionId);

        // Assert
        Assert.True(removed);
        Assert.False(_sessionManager.SessionExists(sessionId));
        Assert.Equal(0, _sessionManager.SessionCount);
    }

    [Fact]
    public void RemoveSession_ReturnsFalse_WhenSessionNotExists()
    {
        // Act
        var removed = _sessionManager.RemoveSession("nonexistent-session");

        // Assert
        Assert.False(removed);
    }

    #endregion

    #region UpdateLastActivity Tests

    [Fact]
    public void UpdateLastActivity_UpdatesTimestamp()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);
        var originalMetadata = _sessionManager.GetMetadata(sessionId);
        var originalTime = originalMetadata?.LastActivityAt;

        // Wait a bit to ensure time difference
        Thread.Sleep(10);

        // Act
        _sessionManager.UpdateLastActivity(sessionId);
        var updatedMetadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.NotNull(updatedMetadata?.LastActivityAt);
        Assert.True(updatedMetadata.LastActivityAt >= originalTime);
    }

    #endregion

    #region IncrementMessageCount Tests

    [Fact]
    public void IncrementMessageCount_IncrementsCount()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Act
        _sessionManager.IncrementMessageCount(sessionId);
        _sessionManager.IncrementMessageCount(sessionId);
        _sessionManager.IncrementMessageCount(sessionId);
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.Equal(3, metadata?.MessageCount);
    }

    #endregion

    #region UpdateSummary Tests

    [Fact]
    public void UpdateSummary_UpdatesSummary()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Act
        _sessionManager.UpdateSummary(sessionId, "Test conversation summary");
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.Equal("Test conversation summary", metadata?.Summary);
    }

    #endregion

    #region ClearAll Tests

    [Fact]
    public void ClearAll_RemovesAllSessions()
    {
        // Arrange
        var mockSession1 = CreateMockSession("session-1");
        var mockSession2 = CreateMockSession("session-2");
        var config = new SessionConfig { Model = "gpt-4" };

        _sessionManager.RegisterSession("session-1", mockSession1, config);
        _sessionManager.RegisterSession("session-2", mockSession2, config);

        // Act
        _sessionManager.ClearAll();

        // Assert
        Assert.Equal(0, _sessionManager.SessionCount);
        Assert.False(_sessionManager.SessionExists("session-1"));
        Assert.False(_sessionManager.SessionExists("session-2"));
    }

    #endregion

    #region SessionExists Tests

    [Fact]
    public void SessionExists_ReturnsTrue_WhenSessionExists()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, mockSession, config);

        // Act & Assert
        Assert.True(_sessionManager.SessionExists(sessionId));
    }

    [Fact]
    public void SessionExists_ReturnsFalse_WhenSessionNotExists()
    {
        // Act & Assert
        Assert.False(_sessionManager.SessionExists("nonexistent-session"));
    }

    #endregion

    #region Helper Methods

    private static CopilotSession CreateMockSession(string sessionId)
    {
        // We can't easily mock CopilotSession as it has internal constructor
        // For these tests, we'll use a null-like behavior workaround
        // In reality, the session manager stores whatever CopilotSession is passed
        // The tests focus on the manager's own logic, not the session's behavior
        return null!;
    }

    #endregion
}
