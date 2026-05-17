using System.Text.Json;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Responses;
using Microsoft.Extensions.Caching.Memory;
using SdkModelInfo = GitHub.Copilot.SDK.ModelInfo;

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
/// Falls back to a minimal static list only if the SDK model lookup is unavailable.
/// Caches the models list for one week.
/// </summary>
public class ModelsService : IModelsService
{
    private const string CacheKey = "AvailableModels";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);
    private static readonly string[] PreferredModelOrder =
    {
        "gpt-5-mini",
        "gpt-5.4-mini",
        "claude-sonnet-4.6",
        "claude-sonnet-4.5",
        "claude-sonnet-4",
        "gpt-5.2-codex",
        "gpt-5.3-codex",
        "gpt-5.2",
        "gpt-5.4",
        "gpt-5.5"
    };

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
        new ModelInfo { Value = "claude-sonnet-4", Label = "Claude Sonnet 4", Description = "Default fast model for app creation and coding tasks" },
        new ModelInfo { Value = "gpt-5-mini", Label = "GPT-5 Mini", Description = "Fast model for iterative coding and app creation" },
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
            var status = _clientManager.Status;
            if (!status.IsConnected)
            {
                _logger.LogInformation("Copilot client is not connected; starting it before loading models");
                await _clientManager.StartAsync(cancellationToken);
            }

            var sdkModels = await _clientManager.ListModelsAsync(cancellationToken);
            models = sdkModels
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .Select(MapSdkModel)
                .OrderBy(GetModelSortRank)
                .ThenBy(m => m.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (models.Count == 0)
            {
                _logger.LogWarning("SDK returned no models, using fallback list");
                models = LoadModelsFromConfig();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching models from Copilot SDK, using fallback list");
            models = LoadModelsFromConfig();
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

    private static ModelInfo MapSdkModel(SdkModelInfo model)
    {
        var supportsVision = model.Capabilities?.Supports?.Vision == true;
        var contextWindow = model.Capabilities?.Limits?.MaxContextWindowTokens ?? 0;
        var descriptionParts = new List<string>();

        if (contextWindow > 0)
        {
            descriptionParts.Add($"Context window: {contextWindow:N0} tokens");
        }

        descriptionParts.Add(supportsVision ? "Supports vision inputs" : "Text-only model");

        if (model.Billing?.Multiplier > 0)
        {
            descriptionParts.Add($"Billing multiplier: {model.Billing.Multiplier:0.##}x");
        }

        if (!string.IsNullOrWhiteSpace(model.Policy?.State))
        {
            descriptionParts.Add($"Policy: {model.Policy.State}");
        }

        return new ModelInfo
        {
            Value = model.Id,
            Label = string.IsNullOrWhiteSpace(model.Name) ? FormatModelLabel(model.Id) : model.Name,
            Description = string.Join(". ", descriptionParts)
        };
    }

    private static int GetModelSortRank(ModelInfo model)
    {
        var preferredIndex = Array.FindIndex(
            PreferredModelOrder,
            preferred => string.Equals(preferred, model.Value, StringComparison.OrdinalIgnoreCase));

        if (preferredIndex >= 0)
        {
            return preferredIndex;
        }

        if (string.Equals(model.Value, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return 900;
        }

        if (model.Value.Contains("opus", StringComparison.OrdinalIgnoreCase))
        {
            return 800;
        }

        return 500;
    }

    private static string FormatModelLabel(string modelId)
    {
        return string.Join(" ", modelId
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length <= 3 ? part.ToUpperInvariant() : char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
