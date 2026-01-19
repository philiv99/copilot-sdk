using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for CopilotClientService.
/// </summary>
public class CopilotClientServiceTests
{
    private readonly Mock<ILogger<CopilotClientManager>> _managerLoggerMock;
    private readonly Mock<ILogger<CopilotClientService>> _serviceLoggerMock;
    private readonly CopilotClientManager _clientManager;
    private readonly CopilotClientService _service;

    public CopilotClientServiceTests()
    {
        _managerLoggerMock = new Mock<ILogger<CopilotClientManager>>();
        _serviceLoggerMock = new Mock<ILogger<CopilotClientService>>();
        _clientManager = new CopilotClientManager(_managerLoggerMock.Object);
        _service = new CopilotClientService(_clientManager, _serviceLoggerMock.Object);
    }

    [Fact]
    public void GetStatus_ReturnsDisconnectedState_WhenClientNotStarted()
    {
        // Act
        var status = _service.GetStatus();

        // Assert
        Assert.Equal("Disconnected", status.ConnectionState);
        Assert.False(status.IsConnected);
        Assert.Null(status.ConnectedAt);
        Assert.Null(status.Error);
    }

    [Fact]
    public void GetConfig_ReturnsDefaultConfiguration_WhenNotConfigured()
    {
        // Act
        var config = _service.GetConfig();

        // Assert
        Assert.Null(config.CliPath);
        Assert.Null(config.CliArgs);
        Assert.Null(config.CliUrl);
        Assert.Equal(0, config.Port);
        Assert.True(config.UseStdio);
        Assert.Equal("info", config.LogLevel);
        Assert.True(config.AutoStart);
        Assert.True(config.AutoRestart);
        Assert.Null(config.Cwd);
        Assert.Null(config.Environment);
    }

    [Fact]
    public void UpdateConfig_UpdatesCliPath_WhenProvided()
    {
        // Arrange
        var request = new UpdateClientConfigRequest
        {
            CliPath = "/usr/local/bin/copilot"
        };

        // Act
        _service.UpdateConfig(request);
        var config = _service.GetConfig();

        // Assert
        Assert.Equal("/usr/local/bin/copilot", config.CliPath);
    }

    [Fact]
    public void UpdateConfig_UpdatesMultipleSettings_WhenProvided()
    {
        // Arrange
        var request = new UpdateClientConfigRequest
        {
            CliPath = "/path/to/cli",
            CliArgs = new[] { "--verbose" },
            Port = 8080,
            UseStdio = false,
            LogLevel = "debug",
            AutoStart = false,
            AutoRestart = false,
            Cwd = "/working/dir",
            Environment = new Dictionary<string, string> { { "TEST_VAR", "test_value" } }
        };

        // Act
        _service.UpdateConfig(request);
        var config = _service.GetConfig();

        // Assert
        Assert.Equal("/path/to/cli", config.CliPath);
        Assert.Single(config.CliArgs!);
        Assert.Equal("--verbose", config.CliArgs![0]);
        Assert.Equal(8080, config.Port);
        Assert.False(config.UseStdio);
        Assert.Equal("debug", config.LogLevel);
        Assert.False(config.AutoStart);
        Assert.False(config.AutoRestart);
        Assert.Equal("/working/dir", config.Cwd);
        Assert.NotNull(config.Environment);
        Assert.Equal("test_value", config.Environment!["TEST_VAR"]);
    }

    [Fact]
    public void UpdateConfig_PreservesExistingValues_WhenNotProvided()
    {
        // Arrange - first update
        var firstRequest = new UpdateClientConfigRequest
        {
            CliPath = "/path/to/cli",
            LogLevel = "debug"
        };
        _service.UpdateConfig(firstRequest);

        // Arrange - second update (only changes LogLevel)
        var secondRequest = new UpdateClientConfigRequest
        {
            LogLevel = "warn"
        };

        // Act
        _service.UpdateConfig(secondRequest);
        var config = _service.GetConfig();

        // Assert - CliPath should be preserved, LogLevel should be updated
        Assert.Equal("/path/to/cli", config.CliPath);
        Assert.Equal("warn", config.LogLevel);
    }

    [Fact]
    public void UpdateConfig_SetsCliUrl_WhenProvided()
    {
        // Arrange
        var request = new UpdateClientConfigRequest
        {
            CliUrl = "http://localhost:3000"
        };

        // Act
        _service.UpdateConfig(request);
        var config = _service.GetConfig();

        // Assert
        Assert.Equal("http://localhost:3000", config.CliUrl);
    }
}
