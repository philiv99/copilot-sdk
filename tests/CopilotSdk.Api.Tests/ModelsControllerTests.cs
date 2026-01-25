using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for the ModelsController.
/// </summary>
public class ModelsControllerTests
{
    private readonly Mock<IModelsService> _mockModelsService;
    private readonly Mock<ILogger<ModelsController>> _mockLogger;
    private readonly ModelsController _controller;

    public ModelsControllerTests()
    {
        _mockModelsService = new Mock<IModelsService>();
        _mockLogger = new Mock<ILogger<ModelsController>>();
        _controller = new ModelsController(_mockModelsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetModels_ReturnsOkWithModels()
    {
        // Arrange
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>
            {
                new ModelInfo { Value = "gpt-4o", Label = "GPT-4o", Description = "Most capable" },
                new ModelInfo { Value = "gpt-4o-mini", Label = "GPT-4o Mini", Description = "Fast" },
            },
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _mockModelsService.Setup(s => s.GetModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetModels(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ModelsResponse>(okResult.Value);
        Assert.Equal(2, response.Models.Count);
        Assert.Equal("gpt-4o", response.Models[0].Value);
    }

    [Fact]
    public async Task GetModels_CallsModelsService()
    {
        // Arrange
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>(),
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _mockModelsService.Setup(s => s.GetModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetModels(CancellationToken.None);

        // Assert
        _mockModelsService.Verify(s => s.GetModelsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshModels_ReturnsOkWithRefreshedModels()
    {
        // Arrange
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>
            {
                new ModelInfo { Value = "gpt-4o", Label = "GPT-4o", Description = "Most capable" },
            },
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _mockModelsService.Setup(s => s.RefreshModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshModels(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ModelsResponse>(okResult.Value);
        Assert.Single(response.Models);
    }

    [Fact]
    public async Task RefreshModels_CallsRefreshModelsAsync()
    {
        // Arrange
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>(),
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _mockModelsService.Setup(s => s.RefreshModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.RefreshModels(CancellationToken.None);

        // Assert
        _mockModelsService.Verify(s => s.RefreshModelsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetModels_ReturnsModelsWithValidStructure()
    {
        // Arrange
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>
            {
                new ModelInfo { Value = "gpt-4o", Label = "GPT-4o", Description = "Most capable model" },
            },
            CachedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)
        };
        _mockModelsService.Setup(s => s.GetModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetModels(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ModelsResponse>(okResult.Value);
        
        Assert.NotEmpty(response.Models[0].Value);
        Assert.NotEmpty(response.Models[0].Label);
        Assert.NotEmpty(response.Models[0].Description);
        Assert.True(response.ExpiresAt > response.CachedAt);
    }

    [Fact]
    public async Task GetModels_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>(),
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _mockModelsService.Setup(s => s.GetModelsAsync(cts.Token))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetModels(cts.Token);

        // Assert
        _mockModelsService.Verify(s => s.GetModelsAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task RefreshModels_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var expectedResponse = new ModelsResponse
        {
            Models = new List<ModelInfo>(),
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _mockModelsService.Setup(s => s.RefreshModelsAsync(cts.Token))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.RefreshModels(cts.Token);

        // Assert
        _mockModelsService.Verify(s => s.RefreshModelsAsync(cts.Token), Times.Once);
    }
}
