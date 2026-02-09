using System.Text.Json;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for the ModelsService.
/// </summary>
public class ModelsServiceTests : IDisposable
{
    private readonly Mock<ICopilotClientManager> _mockClientManager;
    private readonly Mock<ILogger<ModelsService>> _mockLogger;
    private readonly IMemoryCache _cache;
    private readonly ModelsService _service;
    private readonly string _originalConfigPath;

    public ModelsServiceTests()
    {
        _mockClientManager = new Mock<ICopilotClientManager>();
        _mockLogger = new Mock<ILogger<ModelsService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _originalConfigPath = ModelsService.ModelsConfigPath;
        _service = new ModelsService(_mockClientManager.Object, _cache, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Restore original config path after each test
        ModelsService.ModelsConfigPath = _originalConfigPath;
        _cache.Dispose();
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModels()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act
        var result = await _service.GetModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Models);
        Assert.NotEmpty(result.Models);
        Assert.True(result.ExpiresAt > result.CachedAt);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsCachedModels_OnSecondCall()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Connected",
            IsConnected = true
        });

        // Act
        var result1 = await _service.GetModelsAsync();
        var result2 = await _service.GetModelsAsync();

        // Assert
        Assert.Equal(result1.CachedAt, result2.CachedAt);
        Assert.Equal(result1.ExpiresAt, result2.ExpiresAt);
        Assert.Equal(result1.Models.Count, result2.Models.Count);
    }

    [Fact]
    public async Task GetModelsAsync_IncludesExpectedFallbackModels_WhenNoConfigFile()
    {
        // Arrange - point to a non-existent config file to force fallback
        ModelsService.ModelsConfigPath = "nonexistent_models.json";
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act
        var result = await _service.GetModelsAsync();

        // Assert - hardcoded fallback contains gpt-4o, claude-sonnet-4, gemini-2.5-pro
        Assert.Contains(result.Models, m => m.Value == "gpt-4o");
        Assert.Contains(result.Models, m => m.Value == "claude-sonnet-4");
        Assert.Contains(result.Models, m => m.Value == "gemini-2.5-pro");
    }

    [Fact]
    public async Task GetModelsAsync_ModelsHaveRequiredFields()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act
        var result = await _service.GetModelsAsync();

        // Assert
        foreach (var model in result.Models)
        {
            Assert.False(string.IsNullOrEmpty(model.Value), "Model value should not be empty");
            Assert.False(string.IsNullOrEmpty(model.Label), "Model label should not be empty");
            Assert.False(string.IsNullOrEmpty(model.Description), "Model description should not be empty");
        }
    }

    [Fact]
    public async Task GetModelsAsync_CacheDurationIsOneWeek()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act
        var result = await _service.GetModelsAsync();

        // Assert
        var expectedDuration = TimeSpan.FromDays(7);
        var actualDuration = result.ExpiresAt - result.CachedAt;
        Assert.Equal(expectedDuration, actualDuration);
    }

    [Fact]
    public async Task RefreshModelsAsync_ClearsCache()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Get initial models to populate cache
        var initialResult = await _service.GetModelsAsync();

        // Wait a tiny bit to ensure different timestamps
        await Task.Delay(10);

        // Act
        var refreshedResult = await _service.RefreshModelsAsync();

        // Assert
        Assert.True(refreshedResult.CachedAt > initialResult.CachedAt);
    }

    [Fact]
    public async Task RefreshModelsAsync_ReturnsNewModels()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Connected",
            IsConnected = true
        });

        // Act
        var result = await _service.RefreshModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Models);
        Assert.NotEmpty(result.Models);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsDefaultModels_WhenClientNotConnected()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act
        var result = await _service.GetModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Models);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsDefaultModels_WhenClientConnected()
    {
        // Arrange
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Connected",
            IsConnected = true
        });

        // Act
        var result = await _service.GetModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Models);
    }

    [Fact]
    public async Task GetModelsAsync_SetsValidCacheTimestamps()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;
        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        // Act
        var result = await _service.GetModelsAsync();
        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.True(result.CachedAt >= beforeCall && result.CachedAt <= afterCall);
        Assert.True(result.ExpiresAt > result.CachedAt);
    }

    [Fact]
    public void LoadModelsFromConfig_LoadsFromJsonFile()
    {
        // Arrange - write a temp models.json
        var tempFile = Path.Combine(AppContext.BaseDirectory, "test_models_load.json");
        var config = new
        {
            lastUpdated = "2026-02-09T00:00:00Z",
            models = new[]
            {
                new { value = "test-model", label = "Test Model", description = "A test model" },
                new { value = "test-model-2", label = "Test Model 2", description = "Another test model" },
            }
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(config));
        ModelsService.ModelsConfigPath = "test_models_load.json";

        try
        {
            // Act
            var models = _service.LoadModelsFromConfig();

            // Assert
            Assert.Equal(2, models.Count);
            Assert.Contains(models, m => m.Value == "test-model");
            Assert.Contains(models, m => m.Value == "test-model-2");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadModelsFromConfig_ReturnsFallback_WhenFileNotFound()
    {
        // Arrange
        ModelsService.ModelsConfigPath = "does_not_exist.json";

        // Act
        var models = _service.LoadModelsFromConfig();

        // Assert - should get the hardcoded fallback (3 models)
        Assert.Equal(3, models.Count);
        Assert.Contains(models, m => m.Value == "gpt-4o");
    }

    [Fact]
    public void LoadModelsFromConfig_ReturnsFallback_WhenFileIsInvalid()
    {
        // Arrange
        var tempFile = Path.Combine(AppContext.BaseDirectory, "test_models_invalid.json");
        File.WriteAllText(tempFile, "not valid json {{{");
        ModelsService.ModelsConfigPath = "test_models_invalid.json";

        try
        {
            // Act
            var models = _service.LoadModelsFromConfig();

            // Assert
            Assert.Equal(3, models.Count);
            Assert.Contains(models, m => m.Value == "gpt-4o");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadModelsFromConfig_ReturnsFallback_WhenModelsArrayEmpty()
    {
        // Arrange
        var tempFile = Path.Combine(AppContext.BaseDirectory, "test_models_empty.json");
        var config = new { lastUpdated = "2026-01-01T00:00:00Z", models = Array.Empty<object>() };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(config));
        ModelsService.ModelsConfigPath = "test_models_empty.json";

        try
        {
            // Act
            var models = _service.LoadModelsFromConfig();

            // Assert
            Assert.Equal(3, models.Count);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetModelsAsync_SetsModelsLastUpdated_WhenConfigFileLoaded()
    {
        // Arrange
        var tempFile = Path.Combine(AppContext.BaseDirectory, "test_models_timestamp.json");
        var config = new
        {
            lastUpdated = "2026-02-09T00:00:00Z",
            models = new[] { new { value = "m1", label = "M1", description = "Model 1" } }
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(config));
        ModelsService.ModelsConfigPath = "test_models_timestamp.json";

        _mockClientManager.Setup(m => m.Status).Returns(new ClientStatus
        {
            ConnectionState = "Disconnected",
            IsConnected = false
        });

        try
        {
            // Act
            var result = await _service.GetModelsAsync();

            // Assert
            Assert.NotNull(result.ModelsLastUpdated);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
