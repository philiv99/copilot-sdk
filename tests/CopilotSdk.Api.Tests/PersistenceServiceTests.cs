using System.Text.Json;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for the PersistenceService class.
/// </summary>
public class PersistenceServiceTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly Mock<ILogger<PersistenceService>> _loggerMock;
    private readonly IPersistenceService _service;

    public PersistenceServiceTests()
    {
        // Create a unique test directory for each test run
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"CopilotSdkTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataDirectory);

        _loggerMock = new Mock<ILogger<PersistenceService>>();

        var configurationMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.Value).Returns(_testDataDirectory);
        configurationMock.Setup(c => c.GetSection("Persistence:DataDirectory")).Returns(sectionMock.Object);

        // Create the service with a custom configuration
        var configData = new Dictionary<string, string?>
        {
            { "Persistence:DataDirectory", _testDataDirectory }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new PersistenceService(_loggerMock.Object, configuration);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDataDirectory))
        {
            try
            {
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
    public async Task SaveClientConfigAsync_SavesConfigurationToFile()
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

        // Assert
        var filePath = Path.Combine(_testDataDirectory, "client-config.json");
        Assert.True(File.Exists(filePath));

        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("copilot", json);
    }

    [Fact]
    public async Task LoadClientConfigAsync_ReturnsNullWhenNoFileExists()
    {
        // Act
        var result = await _service.LoadClientConfigAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadClientConfigAsync_ReturnsConfigWhenFileExists()
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
    public async Task SaveSessionAsync_SavesSessionToFile()
    {
        // Arrange
        var sessionData = CreateTestSessionData("test-session-1");

        // Act
        await _service.SaveSessionAsync(sessionData);

        // Assert
        var sessionsDir = Path.Combine(_testDataDirectory, "sessions");
        Assert.True(Directory.Exists(sessionsDir));
        var files = Directory.GetFiles(sessionsDir, "*.json");
        Assert.Single(files);
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
