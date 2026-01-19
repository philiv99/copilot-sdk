using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

public class CopilotClientHostedServiceTests
{
    private readonly Mock<ICopilotClientManager> _mockClientManager;
    private readonly Mock<ILogger<CopilotClientHostedService>> _mockLogger;
    private readonly CopilotClientHostedService _hostedService;

    public CopilotClientHostedServiceTests()
    {
        _mockClientManager = new Mock<ICopilotClientManager>();
        _mockLogger = new Mock<ILogger<CopilotClientHostedService>>();
        _hostedService = new CopilotClientHostedService(_mockClientManager.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StartAsync_WhenAutoStartEnabled_StartsClient()
    {
        // Arrange
        var config = new CopilotClientConfig { AutoStart = true };
        _mockClientManager.Setup(m => m.Config).Returns(config);
        _mockClientManager.Setup(m => m.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _hostedService.StartAsync(CancellationToken.None);

        // Assert
        _mockClientManager.Verify(m => m.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenAutoStartDisabled_DoesNotStartClient()
    {
        // Arrange
        var config = new CopilotClientConfig { AutoStart = false };
        _mockClientManager.Setup(m => m.Config).Returns(config);

        // Act
        await _hostedService.StartAsync(CancellationToken.None);

        // Assert
        _mockClientManager.Verify(m => m.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenStartFails_DoesNotThrow()
    {
        // Arrange
        var config = new CopilotClientConfig { AutoStart = true };
        _mockClientManager.Setup(m => m.Config).Returns(config);
        _mockClientManager.Setup(m => m.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() => _hostedService.StartAsync(CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_StopsClient()
    {
        // Arrange
        _mockClientManager.Setup(m => m.StopAsync()).Returns(Task.CompletedTask);

        // Act
        await _hostedService.StopAsync(CancellationToken.None);

        // Assert
        _mockClientManager.Verify(m => m.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenStopFails_DoesNotThrow()
    {
        // Arrange
        _mockClientManager.Setup(m => m.StopAsync())
            .ThrowsAsync(new Exception("Stop failed"));

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() => _hostedService.StopAsync(CancellationToken.None));
        Assert.Null(exception);
    }
}
