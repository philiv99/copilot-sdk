using System.Text.Json;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Responses;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotSdk.Api.Services;

/// <summary>
/// JSON structure for models.json configuration file.
/// </summary>
internal class ModelsConfigFile
{
    public DateTime LastUpdated { get; set; }
    public List<ModelInfo> Models { get; set; } = new();
}

/// <summary>
/// Service for retrieving available AI models from the Copilot SDK.
/// Loads the default model list from an external models.json configuration file,
/// falling back to a minimal hardcoded list only if the file is missing or unreadable.
/// Caches the models list for one week.
/// </summary>
public class ModelsService : IModelsService
{
    private const string CacheKey = "AvailableModels";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

    /// <summary>
    /// Relative path to the external models configuration file.
    /// Can be overridden in tests.
    /// </summary>
    internal static string ModelsConfigPath = "models.json";

    private readonly ICopilotClientManager _clientManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ModelsService> _logger;

    /// <summary>
    /// Minimal hardcoded fallback used only when the external models.json file
    /// cannot be loaded (missing, corrupt, or inaccessible).
    /// </summary>
    private static readonly List<ModelInfo> HardcodedFallbackModels = new()
    {
        new ModelInfo { Value = "gpt-4o", Label = "GPT-4o", Description = "Most capable GPT-4o model for complex tasks" },
        new ModelInfo { Value = "claude-sonnet-4", Label = "Claude Sonnet 4", Description = "Balanced performance and speed from Anthropic" },
        new ModelInfo { Value = "gemini-2.5-pro", Label = "Gemini 2.5 Pro", Description = "Google's most capable model" },
    };

    /// <summary>
    /// Timestamp from the last successfully loaded models.json file.
    /// </summary>
    private DateTime? _modelsFileLastUpdated;

    public ModelsService(
        ICopilotClientManager clientManager,
        IMemoryCache cache,
        ILogger<ModelsService> logger)
    {
        _clientManager = clientManager;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out ModelsResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogDebug("Returning cached models list (expires at {ExpiresAt})", cachedResponse.ExpiresAt);
            return cachedResponse;
        }

        return await FetchAndCacheModelsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ModelsResponse> RefreshModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forcing refresh of models cache");
        _cache.Remove(CacheKey);
        return await FetchAndCacheModelsAsync(cancellationToken);
    }

    /// <summary>
    /// Loads models from the external models.json configuration file.
    /// Falls back to <see cref="HardcodedFallbackModels"/> if the file cannot be read.
    /// </summary>
    internal List<ModelInfo> LoadModelsFromConfig()
    {
        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, ModelsConfigPath);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Models config file not found at {Path}, using hardcoded fallback", filePath);
                return HardcodedFallbackModels;
            }

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<ModelsConfigFile>(json, options);

            if (config?.Models == null || config.Models.Count == 0)
            {
                _logger.LogWarning("Models config file at {Path} is empty or invalid, using hardcoded fallback", filePath);
                return HardcodedFallbackModels;
            }

            _modelsFileLastUpdated = config.LastUpdated;
            _logger.LogInformation(
                "Loaded {Count} models from config file (last updated {LastUpdated})",
                config.Models.Count,
                config.LastUpdated);

            return config.Models;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading models config file, using hardcoded fallback");
            return HardcodedFallbackModels;
        }
    }

    private async Task<ModelsResponse> FetchAndCacheModelsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(CacheDuration);

        List<ModelInfo> models;

        try
        {
            // Check if the client is connected
            var status = _clientManager.Status;
            if (status.IsConnected)
            {
                // Currently the SDK doesn't have a GetModels method,
                // so we load from the external config file.
                // When the SDK adds this capability, we can call it here instead.
                _logger.LogDebug("Client is connected, loading models from config file (SDK doesn't have GetModels endpoint yet)");
                models = LoadModelsFromConfig();
            }
            else
            {
                _logger.LogDebug("Client is not connected, loading models from config file");
                models = LoadModelsFromConfig();
            }

            // Simulate async operation for consistency
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching models, using hardcoded fallback list");
            models = HardcodedFallbackModels;
        }

        var response = new ModelsResponse
        {
            Models = models,
            CachedAt = now,
            ExpiresAt = expiresAt,
            ModelsLastUpdated = _modelsFileLastUpdated
        };

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);

        _cache.Set(CacheKey, response, cacheOptions);

        _logger.LogInformation("Models cached until {ExpiresAt} ({ModelCount} models)", expiresAt, models.Count);

        return response;
    }
}
