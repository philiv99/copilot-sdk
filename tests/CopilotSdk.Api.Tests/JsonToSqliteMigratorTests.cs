using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using CopilotSdk.Api.Tools;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for the JsonToSqliteMigrator.
/// </summary>
public class JsonToSqliteMigratorTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly string _sessionsDirectory;
    private readonly IPersistenceService _service;
    private readonly Mock<ILogger> _loggerMock;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonToSqliteMigratorTests()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"MigratorTests_{Guid.NewGuid()}");
        _sessionsDirectory = Path.Combine(_testDataDirectory, "sessions");
        Directory.CreateDirectory(_sessionsDirectory);

        var loggerMock = new Mock<ILogger<SqlitePersistenceService>>();
        var dbPath = Path.Combine(_testDataDirectory, "copilot-sdk.db");
        _service = new SqlitePersistenceService(loggerMock.Object, _testDataDirectory, $"Data Source={dbPath}");

        _loggerMock = new Mock<ILogger>();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_testDataDirectory))
        {
            try { Directory.Delete(_testDataDirectory, recursive: true); }
            catch { }
        }
    }

    [Fact]
    public async Task MigrateAsync_MigratesClientConfig()
    {
        // Arrange
        var config = new CopilotClientConfig
        {
            CliPath = "/usr/bin/copilot",
            AutoStart = true,
            Port = 8080
        };
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(_testDataDirectory, "client-config.json"), json);

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.True(result.ClientConfigMigrated);
        Assert.Empty(result.Errors);

        var loaded = await _service.LoadClientConfigAsync();
        Assert.NotNull(loaded);
        Assert.Equal("/usr/bin/copilot", loaded.CliPath);
        Assert.True(loaded.AutoStart);
        Assert.Equal(8080, loaded.Port);
    }

    [Fact]
    public async Task MigrateAsync_SkipsClientConfigIfAlreadyExists()
    {
        // Arrange - save config to SQLite first
        await _service.SaveClientConfigAsync(new CopilotClientConfig { CliPath = "/existing" });

        // Write a different config to JSON
        var config = new CopilotClientConfig { CliPath = "/json-version" };
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(_testDataDirectory, "client-config.json"), json);

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.False(result.ClientConfigMigrated);
        Assert.Single(result.Skipped);
        Assert.Contains("already exists", result.Skipped[0]);

        // Verify original SQLite config is preserved
        var loaded = await _service.LoadClientConfigAsync();
        Assert.Equal("/existing", loaded!.CliPath);
    }

    [Fact]
    public async Task MigrateAsync_MigratesSessionFiles()
    {
        // Arrange
        var sessionData = new PersistedSessionData
        {
            SessionId = "test-session",
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            MessageCount = 2,
            Summary = "Test migration",
            Messages = new List<PersistedMessage>
            {
                new() { Role = "user", Content = "Hello" },
                new() { Role = "assistant", Content = "Hi there!" }
            }
        };
        var json = JsonSerializer.Serialize(sessionData, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(_sessionsDirectory, "test-session.json"), json);

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(1, result.SessionsMigrated);
        Assert.Equal(2, result.TotalMessagesMigrated);
        Assert.Empty(result.Errors);

        var loaded = await _service.LoadSessionAsync("test-session");
        Assert.NotNull(loaded);
        Assert.Equal("Test migration", loaded.Summary);
        Assert.Equal(2, loaded.Messages.Count);
        Assert.Equal("Hello", loaded.Messages[0].Content);
        Assert.Equal("Hi there!", loaded.Messages[1].Content);
    }

    [Fact]
    public async Task MigrateAsync_MigratesMultipleSessions()
    {
        // Arrange
        for (int i = 1; i <= 3; i++)
        {
            var sessionData = new PersistedSessionData
            {
                SessionId = $"session-{i}",
                CreatedAt = DateTime.UtcNow,
                MessageCount = i,
                Messages = Enumerable.Range(0, i)
                    .Select(j => new PersistedMessage { Role = "user", Content = $"Message {j}" })
                    .ToList()
            };
            var json = JsonSerializer.Serialize(sessionData, JsonOptions);
            await File.WriteAllTextAsync(Path.Combine(_sessionsDirectory, $"session-{i}.json"), json);
        }

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(3, result.SessionsMigrated);
        Assert.Equal(6, result.TotalMessagesMigrated); // 1 + 2 + 3
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task MigrateAsync_SkipsAlreadyMigratedSessions()
    {
        // Arrange - save a session to SQLite first
        var existing = new PersistedSessionData
        {
            SessionId = "existing-session",
            CreatedAt = DateTime.UtcNow,
            Messages = new List<PersistedMessage>()
        };
        await _service.SaveSessionAsync(existing);

        // Write same session ID to JSON
        var jsonSession = new PersistedSessionData
        {
            SessionId = "existing-session",
            CreatedAt = DateTime.UtcNow,
            Summary = "This should not overwrite",
            Messages = new List<PersistedMessage>
            {
                new() { Content = "Should not appear" }
            }
        };
        var json = JsonSerializer.Serialize(jsonSession, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(_sessionsDirectory, "existing-session.json"), json);

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(0, result.SessionsMigrated);
        Assert.Single(result.Skipped);

        // Original data preserved
        var loaded = await _service.LoadSessionAsync("existing-session");
        Assert.NotNull(loaded);
        Assert.Null(loaded.Summary); // original had no summary
    }

    [Fact]
    public async Task MigrateAsync_IsIdempotent()
    {
        // Arrange
        var sessionData = new PersistedSessionData
        {
            SessionId = "idempotent-test",
            CreatedAt = DateTime.UtcNow,
            Messages = new List<PersistedMessage>
            {
                new() { Role = "user", Content = "Test" }
            }
        };
        var json = JsonSerializer.Serialize(sessionData, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(_sessionsDirectory, "idempotent-test.json"), json);

        // Act - run twice
        var result1 = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);
        var result2 = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(1, result1.SessionsMigrated);
        Assert.Equal(0, result2.SessionsMigrated);
        Assert.Single(result2.Skipped.Where(s => s.Contains("idempotent-test")));
    }

    [Fact]
    public async Task MigrateAsync_HandlesInvalidJsonGracefully()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_sessionsDirectory, "bad-json.json"), "not valid json {{{");

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(0, result.SessionsMigrated);
        Assert.Single(result.Errors);
        Assert.Contains("bad-json", result.Errors[0]);
    }

    [Fact]
    public async Task MigrateAsync_HandlesNoSessionsDirectory()
    {
        // Arrange - delete the sessions directory
        Directory.Delete(_sessionsDirectory, true);

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(0, result.SessionsMigrated);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task MigrateAsync_MigratesSessionWithConfig()
    {
        // Arrange
        var sessionData = new PersistedSessionData
        {
            SessionId = "config-session",
            CreatedAt = DateTime.UtcNow,
            Config = new PersistedSessionConfig
            {
                Model = "claude-opus-4.5",
                Streaming = true,
                SystemMessage = new PersistedSystemMessageConfig
                {
                    Mode = "Replace",
                    Content = "You are a helpful assistant."
                }
            },
            Messages = new List<PersistedMessage>()
        };
        var json = JsonSerializer.Serialize(sessionData, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(_sessionsDirectory, "config-session.json"), json);

        // Act
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.Equal(1, result.SessionsMigrated);
        var loaded = await _service.LoadSessionAsync("config-session");
        Assert.NotNull(loaded?.Config);
        Assert.Equal("claude-opus-4.5", loaded.Config.Model);
        Assert.True(loaded.Config.Streaming);
        Assert.Equal("Replace", loaded.Config.SystemMessage?.Mode);
    }

    [Fact]
    public async Task MigrateAsync_NoJsonFiles_ReturnsCleanResult()
    {
        // Act - empty directories, no JSON files
        var result = await JsonToSqliteMigrator.MigrateAsync(_service, _testDataDirectory, _loggerMock.Object);

        // Assert
        Assert.False(result.ClientConfigMigrated);
        Assert.Equal(0, result.SessionsMigrated);
        Assert.Equal(0, result.TotalMessagesMigrated);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Skipped);
    }
}
