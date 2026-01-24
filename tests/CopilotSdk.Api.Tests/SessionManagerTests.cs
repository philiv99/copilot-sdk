using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Moq;
using SessionConfig = CopilotSdk.Api.Models.Domain.SessionConfig;
using SessionMetadata = CopilotSdk.Api.Models.Domain.SessionMetadata;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for SessionManager.
/// Tests focus on session registration and persistence operations.
/// </summary>
public class SessionManagerTests
{
    private readonly Mock<ILogger<SessionManager>> _loggerMock;
    private readonly Mock<IPersistenceService> _persistenceServiceMock;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _loggerMock = new Mock<ILogger<SessionManager>>();
        _persistenceServiceMock = new Mock<IPersistenceService>();
        _sessionManager = new SessionManager(_loggerMock.Object, _persistenceServiceMock.Object);
    }

    #region RegisterSessionAsync Tests

    [Fact]
    public async Task RegisterSessionAsync_AddsSessionToActiveTracking()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4", Streaming = true };

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.RegisterSessionAsync(sessionId, mockSession, config);

        // Assert
        Assert.True(_sessionManager.IsSessionActive(sessionId));
        Assert.Equal(1, _sessionManager.ActiveSessionCount);
    }

    [Fact]
    public async Task RegisterSessionAsync_PersistsSessionToFile()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4", Streaming = true };
        PersistedSessionData? savedData = null;

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Callback<PersistedSessionData, CancellationToken>((data, _) => savedData = data)
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.RegisterSessionAsync(sessionId, mockSession, config);

        // Assert
        Assert.NotNull(savedData);
        Assert.Equal(sessionId, savedData.SessionId);
        Assert.Equal("gpt-4", savedData.Config?.Model);
        Assert.True(savedData.Config?.Streaming);
        Assert.Equal(0, savedData.MessageCount);
        Assert.True(savedData.CreatedAt > DateTime.MinValue); // CreatedAt is a DateTime value type
        _persistenceServiceMock.Verify(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterSessionAsync_OverwritesExistingActiveSession()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession1 = CreateMockSession(sessionId);
        var mockSession2 = CreateMockSession(sessionId);
        var config1 = new SessionConfig { Model = "gpt-3.5-turbo" };
        var config2 = new SessionConfig { Model = "gpt-4" };

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.RegisterSessionAsync(sessionId, mockSession1, config1);
        await _sessionManager.RegisterSessionAsync(sessionId, mockSession2, config2);

        // Assert
        Assert.Equal(1, _sessionManager.ActiveSessionCount);
        // Both save calls should have been made
        _persistenceServiceMock.Verify(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region GetSession Tests

    [Fact]
    public async Task GetSession_ReturnsStoredValue_WhenActive()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sessionManager.RegisterSessionAsync(sessionId, mockSession, config);

        // Act
        var session = _sessionManager.GetSession(sessionId);

        // Assert
        Assert.Equal(mockSession, session);
    }

    [Fact]
    public void GetSession_ReturnsNull_WhenNotActive()
    {
        // Act
        var session = _sessionManager.GetSession("nonexistent-session");

        // Assert
        Assert.Null(session);
    }

    #endregion

    #region GetMetadataAsync Tests

    [Fact]
    public async Task GetMetadataAsync_ReturnsMetadataFromPersistence()
    {
        // Arrange
        var sessionId = "test-session-1";
        var persistedData = new PersistedSessionData
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            MessageCount = 5,
            Config = new PersistedSessionConfig { Model = "gpt-4", Streaming = true }
        };

        _persistenceServiceMock
            .Setup(p => p.LoadSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedData);

        // Act
        var metadata = await _sessionManager.GetMetadataAsync(sessionId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(sessionId, metadata.SessionId);
        Assert.Equal("gpt-4", metadata.Config?.Model);
        Assert.True(metadata.Config?.Streaming);
        Assert.Equal(5, metadata.MessageCount);
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsNull_WhenNotInPersistence()
    {
        // Arrange
        _persistenceServiceMock
            .Setup(p => p.LoadSessionAsync("nonexistent-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersistedSessionData?)null);

        // Act
        var metadata = await _sessionManager.GetMetadataAsync("nonexistent-session");

        // Assert
        Assert.Null(metadata);
    }

    #endregion

    #region GetAllMetadataAsync Tests

    [Fact]
    public async Task GetAllMetadataAsync_ReturnsAllPersistedSessions()
    {
        // Arrange
        var sessions = new List<PersistedSessionData>
        {
            new() { SessionId = "session-1", Config = new PersistedSessionConfig { Model = "gpt-4" } },
            new() { SessionId = "session-2", Config = new PersistedSessionConfig { Model = "gpt-4" } },
            new() { SessionId = "session-3", Config = new PersistedSessionConfig { Model = "gpt-4" } }
        };

        _persistenceServiceMock
            .Setup(p => p.LoadAllSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var allMetadata = await _sessionManager.GetAllMetadataAsync();

        // Assert
        Assert.Equal(3, allMetadata.Count);
        Assert.Contains(allMetadata, m => m.SessionId == "session-1");
        Assert.Contains(allMetadata, m => m.SessionId == "session-2");
        Assert.Contains(allMetadata, m => m.SessionId == "session-3");
    }

    [Fact]
    public async Task GetAllMetadataAsync_ReturnsEmptyList_WhenNoPersistedSessions()
    {
        // Arrange
        _persistenceServiceMock
            .Setup(p => p.LoadAllSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersistedSessionData>());

        // Act
        var allMetadata = await _sessionManager.GetAllMetadataAsync();

        // Assert
        Assert.Empty(allMetadata);
    }

    #endregion

    #region RemoveSessionAsync Tests

    [Fact]
    public async Task RemoveSessionAsync_ReturnsTrue_WhenSessionExistsInPersistence()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _persistenceServiceMock
            .Setup(p => p.DeleteSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sessionManager.RegisterSessionAsync(sessionId, mockSession, config);

        // Act
        var removed = await _sessionManager.RemoveSessionAsync(sessionId);

        // Assert
        Assert.True(removed);
        Assert.False(_sessionManager.IsSessionActive(sessionId));
        Assert.Equal(0, _sessionManager.ActiveSessionCount);
    }

    [Fact]
    public async Task RemoveSessionAsync_ReturnsFalse_WhenSessionNotInPersistence()
    {
        // Arrange
        _persistenceServiceMock
            .Setup(p => p.DeleteSessionAsync("nonexistent-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var removed = await _sessionManager.RemoveSessionAsync("nonexistent-session");

        // Assert
        Assert.False(removed);
    }

    #endregion

    #region UpdateLastActivityAsync Tests

    [Fact]
    public async Task UpdateLastActivityAsync_UpdatesTimestampInPersistence()
    {
        // Arrange
        var sessionId = "test-session-1";
        var originalTime = DateTime.UtcNow.AddMinutes(-10);
        var persistedData = new PersistedSessionData
        {
            SessionId = sessionId,
            LastActivityAt = originalTime
        };
        PersistedSessionData? savedData = null;

        _persistenceServiceMock
            .Setup(p => p.LoadSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedData);
        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Callback<PersistedSessionData, CancellationToken>((data, _) => savedData = data)
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.UpdateLastActivityAsync(sessionId);

        // Assert
        Assert.NotNull(savedData);
        Assert.True(savedData.LastActivityAt > originalTime);
    }

    #endregion

    #region IncrementMessageCountAsync Tests

    [Fact]
    public async Task IncrementMessageCountAsync_IncrementsCountInPersistence()
    {
        // Arrange
        var sessionId = "test-session-1";
        var persistedData = new PersistedSessionData
        {
            SessionId = sessionId,
            MessageCount = 5
        };
        PersistedSessionData? savedData = null;

        _persistenceServiceMock
            .Setup(p => p.LoadSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedData);
        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Callback<PersistedSessionData, CancellationToken>((data, _) => savedData = data)
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.IncrementMessageCountAsync(sessionId);

        // Assert
        Assert.NotNull(savedData);
        Assert.Equal(6, savedData.MessageCount);
    }

    #endregion

    #region UpdateSummaryAsync Tests

    [Fact]
    public async Task UpdateSummaryAsync_UpdatesSummaryInPersistence()
    {
        // Arrange
        var sessionId = "test-session-1";
        var persistedData = new PersistedSessionData
        {
            SessionId = sessionId,
            Summary = "Old summary"
        };
        PersistedSessionData? savedData = null;

        _persistenceServiceMock
            .Setup(p => p.LoadSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedData);
        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Callback<PersistedSessionData, CancellationToken>((data, _) => savedData = data)
            .Returns(Task.CompletedTask);

        // Act
        await _sessionManager.UpdateSummaryAsync(sessionId, "New conversation summary");

        // Assert
        Assert.NotNull(savedData);
        Assert.Equal("New conversation summary", savedData.Summary);
    }

    #endregion

    #region ClearActiveSessions Tests

    [Fact]
    public async Task ClearActiveSessions_RemovesAllActiveSessions_ButNotPersistedData()
    {
        // Arrange
        var mockSession1 = CreateMockSession("session-1");
        var mockSession2 = CreateMockSession("session-2");
        var config = new SessionConfig { Model = "gpt-4" };

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sessionManager.RegisterSessionAsync("session-1", mockSession1, config);
        await _sessionManager.RegisterSessionAsync("session-2", mockSession2, config);

        // Act
        _sessionManager.ClearActiveSessions();

        // Assert
        Assert.Equal(0, _sessionManager.ActiveSessionCount);
        Assert.False(_sessionManager.IsSessionActive("session-1"));
        Assert.False(_sessionManager.IsSessionActive("session-2"));
        // Persistence delete should NOT have been called
        _persistenceServiceMock.Verify(p => p.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region IsSessionActive Tests

    [Fact]
    public async Task IsSessionActive_ReturnsTrue_WhenSessionIsActive()
    {
        // Arrange
        var sessionId = "test-session-1";
        var mockSession = CreateMockSession(sessionId);
        var config = new SessionConfig { Model = "gpt-4" };

        _persistenceServiceMock
            .Setup(p => p.SaveSessionAsync(It.IsAny<PersistedSessionData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sessionManager.RegisterSessionAsync(sessionId, mockSession, config);

        // Act & Assert
        Assert.True(_sessionManager.IsSessionActive(sessionId));
    }

    [Fact]
    public void IsSessionActive_ReturnsFalse_WhenSessionNotActive()
    {
        // Act & Assert
        Assert.False(_sessionManager.IsSessionActive("nonexistent-session"));
    }

    #endregion

    #region SessionExistsInPersistence Tests

    [Fact]
    public void SessionExistsInPersistence_ReturnsTrue_WhenPersistedFileExists()
    {
        // Arrange
        _persistenceServiceMock
            .Setup(p => p.SessionExists("test-session"))
            .Returns(true);

        // Act & Assert
        Assert.True(_sessionManager.SessionExistsInPersistence("test-session"));
    }

    [Fact]
    public void SessionExistsInPersistence_ReturnsFalse_WhenPersistedFileNotExists()
    {
        // Arrange
        _persistenceServiceMock
            .Setup(p => p.SessionExists("test-session"))
            .Returns(false);

        // Act & Assert
        Assert.False(_sessionManager.SessionExistsInPersistence("test-session"));
    }

    #endregion

    #region Helper Methods

    private static CopilotSession CreateMockSession(string sessionId)
    {
        // We can't easily mock CopilotSession as it has internal constructor
        // For these tests, we use null as a placeholder
        return null!;
    }

    #endregion
}
