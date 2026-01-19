using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for CopilotClientManager.
/// </summary>
public class CopilotClientManagerTests
{
    private readonly Mock<ILogger<CopilotClientManager>> _loggerMock;
    private readonly CopilotClientManager _manager;

    public CopilotClientManagerTests()
    {
        _loggerMock = new Mock<ILogger<CopilotClientManager>>();
        _manager = new CopilotClientManager(_loggerMock.Object);
    }

    [Fact]
    public void Status_ReturnsDisconnected_WhenClientNotStarted()
    {
        // Act
        var status = _manager.Status;

        // Assert
        Assert.Equal("Disconnected", status.ConnectionState);
        Assert.False(status.IsConnected);
        Assert.Null(status.ConnectedAt);
        Assert.Null(status.Error);
    }

    [Fact]
    public void Config_ReturnsDefaultConfiguration_WhenNotConfigured()
    {
        // Act
        var config = _manager.Config;

        // Assert
        Assert.Null(config.CliPath);
        Assert.True(config.UseStdio);
        Assert.Equal("info", config.LogLevel);
        Assert.True(config.AutoStart);
        Assert.True(config.AutoRestart);
    }

    [Fact]
    public void UpdateConfig_SetsNewConfiguration()
    {
        // Arrange
        var newConfig = new CopilotClientConfig
        {
            CliPath = "/test/path",
            LogLevel = "debug",
            AutoStart = false
        };

        // Act
        _manager.UpdateConfig(newConfig);
        var config = _manager.Config;

        // Assert
        Assert.Equal("/test/path", config.CliPath);
        Assert.Equal("debug", config.LogLevel);
        Assert.False(config.AutoStart);
    }

    [Fact]
    public void Client_ReturnsNull_WhenNotStarted()
    {
        // Act
        var client = _manager.Client;

        // Assert
        Assert.Null(client);
    }

    [Fact]
    public void UpdateConfig_SetsEnvironmentVariables()
    {
        // Arrange
        var newConfig = new CopilotClientConfig
        {
            Environment = new Dictionary<string, string>
            {
                { "API_KEY", "secret" },
                { "DEBUG", "true" }
            }
        };

        // Act
        _manager.UpdateConfig(newConfig);
        var config = _manager.Config;

        // Assert
        Assert.NotNull(config.Environment);
        Assert.Equal(2, config.Environment!.Count);
        Assert.Equal("secret", config.Environment["API_KEY"]);
        Assert.Equal("true", config.Environment["DEBUG"]);
    }

    [Fact]
    public void UpdateConfig_SetsCliArgs()
    {
        // Arrange
        var newConfig = new CopilotClientConfig
        {
            CliArgs = new[] { "--verbose", "--debug" }
        };

        // Act
        _manager.UpdateConfig(newConfig);
        var config = _manager.Config;

        // Assert
        Assert.NotNull(config.CliArgs);
        Assert.Equal(2, config.CliArgs!.Length);
        Assert.Equal("--verbose", config.CliArgs[0]);
        Assert.Equal("--debug", config.CliArgs[1]);
    }

    [Fact]
    public async Task DisposeAsync_CompletesSuccessfully_WhenClientNotStarted()
    {
        // Act & Assert - should not throw
        await _manager.DisposeAsync();
    }
}
