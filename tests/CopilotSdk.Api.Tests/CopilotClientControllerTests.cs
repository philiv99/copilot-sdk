using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for CopilotClientController.
/// </summary>
public class CopilotClientControllerTests
{
    private readonly Mock<ICopilotClientService> _serviceMock;
    private readonly Mock<ILogger<CopilotClientController>> _loggerMock;
    private readonly CopilotClientController _controller;

    public CopilotClientControllerTests()
    {
        _serviceMock = new Mock<ICopilotClientService>();
        _loggerMock = new Mock<ILogger<CopilotClientController>>();
        _controller = new CopilotClientController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetStatus Tests

    [Fact]
    public void GetStatus_ReturnsOkResult_WithClientStatus()
    {
        // Arrange
        var expectedStatus = new ClientStatusResponse
        {
            ConnectionState = "Connected",
            IsConnected = true,
            ConnectedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.GetStatus()).Returns(expectedStatus);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualStatus = Assert.IsType<ClientStatusResponse>(okResult.Value);
        Assert.Equal("Connected", actualStatus.ConnectionState);
        Assert.True(actualStatus.IsConnected);
    }

    [Fact]
    public void GetStatus_ReturnsDisconnectedState_WhenClientNotStarted()
    {
        // Arrange
        var expectedStatus = new ClientStatusResponse
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        };
        _serviceMock.Setup(s => s.GetStatus()).Returns(expectedStatus);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualStatus = Assert.IsType<ClientStatusResponse>(okResult.Value);
        Assert.Equal("Disconnected", actualStatus.ConnectionState);
        Assert.False(actualStatus.IsConnected);
    }

    #endregion

    #region GetConfig Tests

    [Fact]
    public void GetConfig_ReturnsOkResult_WithClientConfig()
    {
        // Arrange
        var expectedConfig = new ClientConfigResponse
        {
            CliPath = "/path/to/cli",
            LogLevel = "debug",
            AutoStart = true
        };
        _serviceMock.Setup(s => s.GetConfig()).Returns(expectedConfig);

        // Act
        var result = _controller.GetConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualConfig = Assert.IsType<ClientConfigResponse>(okResult.Value);
        Assert.Equal("/path/to/cli", actualConfig.CliPath);
        Assert.Equal("debug", actualConfig.LogLevel);
        Assert.True(actualConfig.AutoStart);
    }

    #endregion

    #region UpdateConfig Tests

    [Fact]
    public void UpdateConfig_ReturnsOkResult_WithUpdatedConfig()
    {
        // Arrange
        var request = new UpdateClientConfigRequest
        {
            CliPath = "/new/path/to/cli",
            LogLevel = "warn"
        };
        var expectedConfig = new ClientConfigResponse
        {
            CliPath = "/new/path/to/cli",
            LogLevel = "warn"
        };
        _serviceMock.Setup(s => s.GetConfig()).Returns(expectedConfig);

        // Act
        var result = _controller.UpdateConfig(request);

        // Assert
        _serviceMock.Verify(s => s.UpdateConfig(request), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualConfig = Assert.IsType<ClientConfigResponse>(okResult.Value);
        Assert.Equal("/new/path/to/cli", actualConfig.CliPath);
    }

    #endregion

    #region Start Tests

    [Fact]
    public async Task Start_ReturnsOkResult_WithConnectedStatus()
    {
        // Arrange
        var expectedStatus = new ClientStatusResponse
        {
            ConnectionState = "Connected",
            IsConnected = true,
            ConnectedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _serviceMock.Setup(s => s.GetStatus()).Returns(expectedStatus);

        // Act
        var result = await _controller.Start(CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualStatus = Assert.IsType<ClientStatusResponse>(okResult.Value);
        Assert.Equal("Connected", actualStatus.ConnectionState);
        Assert.True(actualStatus.IsConnected);
    }

    #endregion

    #region Stop Tests

    [Fact]
    public async Task Stop_ReturnsOkResult_WithDisconnectedStatus()
    {
        // Arrange
        var expectedStatus = new ClientStatusResponse
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        };
        _serviceMock.Setup(s => s.StopAsync()).Returns(Task.CompletedTask);
        _serviceMock.Setup(s => s.GetStatus()).Returns(expectedStatus);

        // Act
        var result = await _controller.Stop();

        // Assert
        _serviceMock.Verify(s => s.StopAsync(), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualStatus = Assert.IsType<ClientStatusResponse>(okResult.Value);
        Assert.Equal("Disconnected", actualStatus.ConnectionState);
        Assert.False(actualStatus.IsConnected);
    }

    #endregion

    #region ForceStop Tests

    [Fact]
    public async Task ForceStop_ReturnsOkResult_WithDisconnectedStatus()
    {
        // Arrange
        var expectedStatus = new ClientStatusResponse
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        };
        _serviceMock.Setup(s => s.ForceStopAsync()).Returns(Task.CompletedTask);
        _serviceMock.Setup(s => s.GetStatus()).Returns(expectedStatus);

        // Act
        var result = await _controller.ForceStop();

        // Assert
        _serviceMock.Verify(s => s.ForceStopAsync(), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualStatus = Assert.IsType<ClientStatusResponse>(okResult.Value);
        Assert.Equal("Disconnected", actualStatus.ConnectionState);
    }

    #endregion

    #region Ping Tests

    [Fact]
    public async Task Ping_ReturnsOkResult_WithPingResponse()
    {
        // Arrange
        var request = new PingRequest { Message = "test" };
        var expectedResponse = new PingResponse
        {
            Message = "test",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LatencyMs = 50,
            ProtocolVersion = 1
        };
        _serviceMock.Setup(s => s.PingAsync(It.IsAny<PingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Ping(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualResponse = Assert.IsType<PingResponse>(okResult.Value);
        Assert.Equal("test", actualResponse.Message);
        Assert.Equal(50, actualResponse.LatencyMs);
    }

    [Fact]
    public async Task Ping_WithNullRequest_UsesEmptyPingRequest()
    {
        // Arrange
        var expectedResponse = new PingResponse
        {
            Message = "",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LatencyMs = 25
        };
        _serviceMock.Setup(s => s.PingAsync(It.IsAny<PingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Ping(null, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.PingAsync(It.Is<PingRequest>(r => r != null), It.IsAny<CancellationToken>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
