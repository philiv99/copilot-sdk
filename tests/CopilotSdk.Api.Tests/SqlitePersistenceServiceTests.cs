using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for the SqlitePersistenceService class.
/// </summary>
public class SqlitePersistenceServiceTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly Mock<ILogger<SqlitePersistenceService>> _loggerMock;
    private readonly IPersistenceService _service;

    public SqlitePersistenceServiceTests()
    {
        // Create a unique test directory for each test run
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"CopilotSdkSqliteTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataDirectory);

        _loggerMock = new Mock<ILogger<SqlitePersistenceService>>();

        var dbPath = Path.Combine(_testDataDirectory, "copilot-sdk.db");
        var connectionString = $"Data Source={dbPath}";

        _service = new SqlitePersistenceService(_loggerMock.Object, _testDataDirectory, connectionString);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDataDirectory))
        {
            try
            {
                // Force SQLite to release all connections
                SqliteConnection.ClearAllPools();
                Directory.Delete(_testDataDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    #region Client Configuration Tests

    [Fact]
    public async Task SaveClientConfigAsync_SavesConfigurationToSQLite()
    {
        // Arrange
        var config = new CopilotClientConfig
        {
            CliPath = "/usr/bin/copilot",
            AutoStart = true,
            LogLevel = "debug",
            Port = 8080
        };

        // Act
        await _service.SaveClientConfigAsync(config);

        // Assert - verify it can be loaded back
        var loaded = await _service.LoadClientConfigAsync();
        Assert.NotNull(loaded);
        Assert.Equal("/usr/bin/copilot", loaded.CliPath);
    }

    [Fact]
    public async Task LoadClientConfigAsync_ReturnsNullWhenNoConfigExists()
    {
        // Act
        var result = await _service.LoadClientConfigAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadClientConfigAsync_ReturnsConfigWhenExists()
    {
        // Arrange
        var config = new CopilotClientConfig
        {
            CliPath = "/usr/bin/copilot",
            AutoStart = true,
            LogLevel = "debug",
            Port = 8080,
            Environment = new Dictionary<string, string> { { "TEST", "value" } }
        };
        await _service.SaveClientConfigAsync(config);

        // Act
        var result = await _service.LoadClientConfigAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/usr/bin/copilot", result.CliPath);
        Assert.True(result.AutoStart);
        Assert.Equal("debug", result.LogLevel);
        Assert.Equal(8080, result.Port);
        Assert.NotNull(result.Environment);
        Assert.Equal("value", result.Environment["TEST"]);
    }

    [Fact]
    public async Task SaveClientConfigAsync_OverwritesExistingConfig()
    {
        // Arrange
        var config1 = new CopilotClientConfig { CliPath = "/path/v1" };
        var config2 = new CopilotClientConfig { CliPath = "/path/v2" };

        // Act
        await _service.SaveClientConfigAsync(config1);
        await _service.SaveClientConfigAsync(config2);
        var result = await _service.LoadClientConfigAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/path/v2", result.CliPath);
    }

    #endregion

    #region Session Data Tests

    [Fact]
    public async Task SaveSessionAsync_SavesSessionToSQLite()
    {
        // Arrange
        var sessionData = CreateTestSessionData("test-session-1");

        // Act
        await _service.SaveSessionAsync(sessionData);

        // Assert
        var loaded = await _service.LoadSessionAsync("test-session-1");
        Assert.NotNull(loaded);
        Assert.Equal("test-session-1", loaded.SessionId);
    }

    [Fact]
    public async Task LoadSessionAsync_ReturnsNullWhenSessionDoesNotExist()
    {
        // Act
        var result = await _service.LoadSessionAsync("non-existent-session");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadSessionAsync_ReturnsSessionWhenExists()
    {
        // Arrange
        var sessionData = CreateTestSessionData("test-session-2");
        sessionData.Summary = "Test session summary";
        await _service.SaveSessionAsync(sessionData);

        // Act
        var result = await _service.LoadSessionAsync("test-session-2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-session-2", result.SessionId);
        Assert.Equal("Test session summary", result.Summary);
    }

    [Fact]
    public async Task LoadAllSessionsAsync_ReturnsEmptyWhenNoSessions()
    {
        // Act
        var result = await _service.LoadAllSessionsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAllSessionsAsync_ReturnsAllSessions()
    {
        // Arrange
        await _service.SaveSessionAsync(CreateTestSessionData("session-1"));
        await _service.SaveSessionAsync(CreateTestSessionData("session-2"));
        await _service.SaveSessionAsync(CreateTestSessionData("session-3"));

        // Act
        var result = await _service.LoadAllSessionsAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.SessionId == "session-1");
        Assert.Contains(result, s => s.SessionId == "session-2");
        Assert.Contains(result, s => s.SessionId == "session-3");
    }

    [Fact]
    public async Task DeleteSessionAsync_ReturnsFalseWhenSessionDoesNotExist()
    {
        // Act
        var result = await _service.DeleteSessionAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSessionAsync_ReturnsTrueAndDeletesSession()
    {
        // Arrange
        await _service.SaveSessionAsync(CreateTestSessionData("session-to-delete"));

        // Act
        var result = await _service.DeleteSessionAsync("session-to-delete");
        var loadedSession = await _service.LoadSessionAsync("session-to-delete");

        // Assert
        Assert.True(result);
        Assert.Null(loadedSession);
    }

    [Fact]
    public async Task DeleteSessionAsync_CascadeDeletesMessages()
    {
        // Arrange
        var sessionData = CreateTestSessionData("session-cascade");
        await _service.SaveSessionAsync(sessionData);
        var messages = new List<PersistedMessage>
        {
            new PersistedMessage { Content = "Message 1" },
            new PersistedMessage { Content = "Message 2" }
        };
        await _service.AppendMessagesAsync("session-cascade", messages);

        // Act
        await _service.DeleteSessionAsync("session-cascade");
        var loadedMessages = await _service.GetMessagesAsync("session-cascade");

        // Assert
        Assert.Empty(loadedMessages);
    }

    [Fact]
    public void SessionExists_ReturnsFalseWhenSessionDoesNotExist()
    {
        // Act
        var result = _service.SessionExists("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SessionExists_ReturnsTrueWhenSessionExists()
    {
        // Arrange
        await _service.SaveSessionAsync(CreateTestSessionData("existing-session"));

        // Act
        var result = _service.SessionExists("existing-session");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetPersistedSessionIds_ReturnsAllSessionIds()
    {
        // Arrange
        await _service.SaveSessionAsync(CreateTestSessionData("id-1"));
        await _service.SaveSessionAsync(CreateTestSessionData("id-2"));

        // Act
        var result = _service.GetPersistedSessionIds();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("id-1", result);
        Assert.Contains("id-2", result);
    }

    [Fact]
    public async Task SaveSessionAsync_UpsertUpdatesExistingSession()
    {
        // Arrange
        var sessionData = CreateTestSessionData("upsert-test");
        sessionData.Summary = "Version 1";
        await _service.SaveSessionAsync(sessionData);

        // Act
        sessionData.Summary = "Version 2";
        sessionData.MessageCount = 5;
        await _service.SaveSessionAsync(sessionData);

        // Assert
        var result = await _service.LoadSessionAsync("upsert-test");
        Assert.NotNull(result);
        Assert.Equal("Version 2", result.Summary);
        Assert.Equal(5, result.MessageCount);
    }

    #endregion

    #region Message Persistence Tests

    [Fact]
    public async Task AppendMessagesAsync_AppendsMessagesToExistingSession()
    {
        // Arrange
        var sessionData = CreateTestSessionData("session-with-messages");
        await _service.SaveSessionAsync(sessionData);

        var messages = new List<PersistedMessage>
        {
            new PersistedMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Role = "user",
                Content = "Hello!"
            },
            new PersistedMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Role = "assistant",
                Content = "Hi there!"
            }
        };

        // Act
        await _service.AppendMessagesAsync("session-with-messages", messages);
        var result = await _service.GetMessagesAsync("session-with-messages");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Hello!", result[0].Content);
        Assert.Equal("Hi there!", result[1].Content);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsEmptyListWhenSessionDoesNotExist()
    {
        // Act
        var result = await _service.GetMessagesAsync("non-existent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AppendMessagesAsync_UpdatesMessageCount()
    {
        // Arrange
        var sessionData = CreateTestSessionData("session-count-test");
        await _service.SaveSessionAsync(sessionData);

        var messages = new List<PersistedMessage>
        {
            new PersistedMessage { Content = "Message 1" },
            new PersistedMessage { Content = "Message 2" },
            new PersistedMessage { Content = "Message 3" }
        };

        // Act
        await _service.AppendMessagesAsync("session-count-test", messages);
        var result = await _service.LoadSessionAsync("session-count-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.MessageCount);
    }

    [Fact]
    public async Task AppendMessagesAsync_PreservesMessageOrder()
    {
        // Arrange
        var sessionData = CreateTestSessionData("order-test");
        await _service.SaveSessionAsync(sessionData);

        var batch1 = new List<PersistedMessage>
        {
            new PersistedMessage { Content = "First" },
            new PersistedMessage { Content = "Second" }
        };
        var batch2 = new List<PersistedMessage>
        {
            new PersistedMessage { Content = "Third" },
            new PersistedMessage { Content = "Fourth" }
        };

        // Act
        await _service.AppendMessagesAsync("order-test", batch1);
        await _service.AppendMessagesAsync("order-test", batch2);
        var result = await _service.GetMessagesAsync("order-test");

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("First", result[0].Content);
        Assert.Equal("Second", result[1].Content);
        Assert.Equal("Third", result[2].Content);
        Assert.Equal("Fourth", result[3].Content);
    }

    [Fact]
    public async Task AppendMessagesAsync_PreservesMessageFields()
    {
        // Arrange
        var sessionData = CreateTestSessionData("fields-test");
        await _service.SaveSessionAsync(sessionData);

        var msgId = Guid.NewGuid();
        var messages = new List<PersistedMessage>
        {
            new PersistedMessage
            {
                Id = msgId,
                Timestamp = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                Role = "assistant",
                Content = "Response here",
                TransformedContent = "Transformed",
                MessageId = "msg-123",
                ToolCallId = "tc-1",
                ToolName = "echo_tool",
                ToolResult = "echoed",
                ToolError = null,
                ReasoningContent = "Thinking...",
                ParentToolCallId = "ptc-1",
                Source = "copilot",
                Attachments = new List<PersistedAttachment>
                {
                    new PersistedAttachment { Type = "file", Path = "/tmp/test.txt", DisplayName = "test.txt" }
                },
                ToolRequests = new List<PersistedToolRequest>
                {
                    new PersistedToolRequest { ToolCallId = "tc-2", ToolName = "get_weather", Arguments = "{\"location\":\"NYC\"}" }
                }
            }
        };

        // Act
        await _service.AppendMessagesAsync("fields-test", messages);
        var result = await _service.GetMessagesAsync("fields-test");

        // Assert
        Assert.Single(result);
        var msg = result[0];
        Assert.Equal(msgId, msg.Id);
        Assert.Equal("assistant", msg.Role);
        Assert.Equal("Response here", msg.Content);
        Assert.Equal("Transformed", msg.TransformedContent);
        Assert.Equal("msg-123", msg.MessageId);
        Assert.Equal("tc-1", msg.ToolCallId);
        Assert.Equal("echo_tool", msg.ToolName);
        Assert.Equal("echoed", msg.ToolResult);
        Assert.Null(msg.ToolError);
        Assert.Equal("Thinking...", msg.ReasoningContent);
        Assert.Equal("ptc-1", msg.ParentToolCallId);
        Assert.Equal("copilot", msg.Source);

        Assert.NotNull(msg.Attachments);
        Assert.Single(msg.Attachments);
        Assert.Equal("file", msg.Attachments[0].Type);
        Assert.Equal("/tmp/test.txt", msg.Attachments[0].Path);
        Assert.Equal("test.txt", msg.Attachments[0].DisplayName);

        Assert.NotNull(msg.ToolRequests);
        Assert.Single(msg.ToolRequests);
        Assert.Equal("tc-2", msg.ToolRequests[0].ToolCallId);
        Assert.Equal("get_weather", msg.ToolRequests[0].ToolName);
        Assert.Equal("{\"location\":\"NYC\"}", msg.ToolRequests[0].Arguments);
    }

    #endregion

    #region Session Config Persistence Tests

    [Fact]
    public async Task SaveSessionAsync_PersistsSessionConfig()
    {
        // Arrange
        var sessionData = CreateTestSessionData("session-with-config");
        sessionData.Config = new PersistedSessionConfig
        {
            Model = "gpt-4",
            Streaming = true,
            SystemMessage = new PersistedSystemMessageConfig
            {
                Mode = "append",
                Content = "You are a helpful assistant."
            },
            AvailableTools = new List<string> { "tool1", "tool2" },
            Provider = new PersistedProviderConfig
            {
                Type = "openai",
                BaseUrl = "https://api.openai.com"
            }
        };

        // Act
        await _service.SaveSessionAsync(sessionData);
        var result = await _service.LoadSessionAsync("session-with-config");

        // Assert
        Assert.NotNull(result?.Config);
        Assert.Equal("gpt-4", result.Config.Model);
        Assert.True(result.Config.Streaming);
        Assert.NotNull(result.Config.SystemMessage);
        Assert.Equal("append", result.Config.SystemMessage.Mode);
        Assert.Equal("You are a helpful assistant.", result.Config.SystemMessage.Content);
        Assert.Equal(2, result.Config.AvailableTools?.Count);
        Assert.NotNull(result.Config.Provider);
        Assert.Equal("openai", result.Config.Provider.Type);
    }

    [Fact]
    public async Task SaveSessionAsync_PersistsToolDefinitions()
    {
        // Arrange
        var sessionData = CreateTestSessionData("session-with-tools");
        sessionData.Config = new PersistedSessionConfig
        {
            Model = "gpt-4",
            Tools = new List<PersistedToolDefinition>
            {
                new PersistedToolDefinition
                {
                    Name = "get_weather",
                    Description = "Gets the current weather",
                    Parameters = new List<PersistedToolParameter>
                    {
                        new PersistedToolParameter
                        {
                            Name = "location",
                            Description = "The city name",
                            Type = "string",
                            Required = true
                        }
                    }
                }
            }
        };

        // Act
        await _service.SaveSessionAsync(sessionData);
        var result = await _service.LoadSessionAsync("session-with-tools");

        // Assert
        Assert.NotNull(result?.Config?.Tools);
        Assert.Single(result.Config.Tools);
        Assert.Equal("get_weather", result.Config.Tools[0].Name);
        Assert.NotNull(result.Config.Tools[0].Parameters);
        Assert.Single(result.Config.Tools[0].Parameters);
        Assert.Equal("location", result.Config.Tools[0].Parameters[0].Name);
        Assert.True(result.Config.Tools[0].Parameters[0].Required);
    }

    #endregion

    #region SQLite-Specific Tests

    [Fact]
    public async Task ConcurrentWrites_DoNotCorruptData()
    {
        // Arrange - pre-create sessions
        for (int i = 0; i < 5; i++)
        {
            await _service.SaveSessionAsync(CreateTestSessionData($"concurrent-{i}"));
        }

        // Act - concurrent message appends to different sessions
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var sessionId = $"concurrent-{i}";
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    await _service.AppendMessagesAsync(sessionId, new[]
                    {
                        new PersistedMessage { Content = $"Message {j}" }
                    });
                }
            }));
        }
        await Task.WhenAll(tasks);

        // Assert - each session should have all 10 messages
        for (int i = 0; i < 5; i++)
        {
            var messages = await _service.GetMessagesAsync($"concurrent-{i}");
            Assert.Equal(10, messages.Count);
        }
    }

    [Fact]
    public async Task DatabaseIsCreatedAutomatically()
    {
        // Assert - the database file should exist
        var dbPath = Path.Combine(_testDataDirectory, "copilot-sdk.db");
        Assert.True(File.Exists(dbPath));
    }

    [Fact]
    public void DataDirectory_ReturnsConfiguredDirectory()
    {
        // Assert
        Assert.Equal(_testDataDirectory, _service.DataDirectory);
    }

    [Fact]
    public async Task SaveSessionAsync_WithMessages_PersistsAllData()
    {
        // Arrange
        var sessionData = CreateTestSessionData("full-save-test");
        sessionData.Messages = new List<PersistedMessage>
        {
            new PersistedMessage { Role = "user", Content = "Hello" },
            new PersistedMessage { Role = "assistant", Content = "Hi!" }
        };
        sessionData.MessageCount = 2;

        // Act - full save with messages
        await _service.SaveSessionAsync(sessionData);
        var result = await _service.LoadSessionAsync("full-save-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("Hello", result.Messages[0].Content);
        Assert.Equal("Hi!", result.Messages[1].Content);
    }

    [Fact]
    public async Task LoadAllSessionsAsync_ReturnsSessionsOrderedByCreatedAt()
    {
        // Arrange
        var s1 = CreateTestSessionData("oldest");
        s1.CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var s2 = CreateTestSessionData("middle");
        s2.CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var s3 = CreateTestSessionData("newest");
        s3.CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        await _service.SaveSessionAsync(s1);
        await _service.SaveSessionAsync(s2);
        await _service.SaveSessionAsync(s3);

        // Act
        var result = await _service.LoadAllSessionsAsync();

        // Assert - should be ordered descending by CreatedAt
        Assert.Equal(3, result.Count);
        Assert.Equal("newest", result[0].SessionId);
        Assert.Equal("middle", result[1].SessionId);
        Assert.Equal("oldest", result[2].SessionId);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithConfiguration_CreatesDatabase()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"SqliteCtorTest_{Guid.NewGuid()}");
        var loggerMock = new Mock<ILogger<SqlitePersistenceService>>();

        var configData = new Dictionary<string, string?>
        {
            { "Persistence:DataDirectory", tempDir }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        try
        {
            // Act
            var service = new SqlitePersistenceService(loggerMock.Object, configuration);

            // Assert
            Assert.Equal(tempDir, service.DataDirectory);
            Assert.True(File.Exists(Path.Combine(tempDir, "copilot-sdk.db")));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Helper Methods

    private static PersistedSessionData CreateTestSessionData(string sessionId)
    {
        return new PersistedSessionData
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            MessageCount = 0,
            Messages = new List<PersistedMessage>()
        };
    }

    #endregion
}
