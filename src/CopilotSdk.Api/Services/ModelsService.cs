using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Responses;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for retrieving available AI models from the Copilot SDK.
/// Caches the models list for one week.
/// </summary>
public class ModelsService : IModelsService
{
    private const string CacheKey = "AvailableModels";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

    private readonly ICopilotClientManager _clientManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ModelsService> _logger;

    /// <summary>
    /// Default models list used when the SDK doesn't provide a models endpoint
    /// or when the client is not connected.
    /// </summary>
    private static readonly List<ModelInfo> DefaultModels = new()
    {
       
        new ModelInfo
        {
            Value = "gpt-4o",
            Label = "GPT-4o",
            Description = "Most capable model for complex tasks"
        },
        new ModelInfo
        {
            Value = "gpt-4o-mini",
            Label = "GPT-4o Mini",
            Description = "Fast and efficient for simpler tasks"
        },
        new ModelInfo
        {
            Value = "gpt-4.1",
            Label = "GPT-4.1",
            Description = "Advanced GPT-4 variant with improved performance"
        },
        new ModelInfo
        {
            Value = "gpt-4.1-mini",
            Label = "GPT-4.1 Mini",
            Description = "Efficient GPT-4.1 variant for faster responses"
        },
        new ModelInfo
        {
            Value = "gpt-4.1-nano",
            Label = "GPT-4.1 Nano",
            Description = "Lightweight GPT-4.1 for quick tasks"
        },
        new ModelInfo
        {
            Value = "o1",
            Label = "O1",
            Description = "Advanced reasoning model for complex problems"
        },
        new ModelInfo
        {
            Value = "o1-mini",
            Label = "O1 Mini",
            Description = "Efficient reasoning model"
        },
        new ModelInfo
        {
            Value = "o1-pro",
            Label = "O1 Pro",
            Description = "Professional-grade reasoning model"
        },
        new ModelInfo
        {
            Value = "o3",
            Label = "O3",
            Description = "Latest reasoning model with enhanced capabilities"
        },
        new ModelInfo
        {
            Value = "o3-mini",
            Label = "O3 Mini",
            Description = "Compact O3 model for everyday reasoning"
        },
        new ModelInfo
        {
            Value = "o4-mini",
            Label = "O4 Mini",
            Description = "Next-generation compact reasoning model"
        },
        new ModelInfo
        {
            Value = "claude-sonnet-4",
            Label = "Claude Sonnet 4",
            Description = "Balanced performance and speed from Anthropic"
        },
        new ModelInfo
        {
            Value = "claude-3.5-sonnet",
            Label = "Claude 3.5 Sonnet",
            Description = "Balanced Claude model for general tasks"
        },
        new ModelInfo
        {
            Value = "claude-3.7-sonnet",
            Label = "Claude 3.7 Sonnet",
            Description = "Enhanced Claude model with improved reasoning"
        }, 
        new ModelInfo
        {
            Value = "claude-opus-4.5",
            Label = "Claude Opus 4.5",
            Description = "Most capable model for complex tasks"
        },
        new ModelInfo
        {
            Value = "gemini-2.0-flash",
            Label = "Gemini 2.0 Flash",
            Description = "Google's fast multimodal model"
        },
        new ModelInfo
        {
            Value = "gemini-2.5-pro",
            Label = "Gemini 2.5 Pro",
            Description = "Google's most capable model"
        }
    };

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
                // so we use the default list. When the SDK adds this capability,
                // we can extend ICopilotClientManager to expose GetModelsAsync.
                _logger.LogDebug("Client is connected, using default models list (SDK doesn't have GetModels endpoint yet)");
                models = DefaultModels;
            }
            else
            {
                _logger.LogDebug("Client is not connected, using default models list");
                models = DefaultModels;
            }

            // Simulate async operation for consistency
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching models from SDK, using default list");
            models = DefaultModels;
        }

        var response = new ModelsResponse
        {
            Models = models,
            CachedAt = now,
            ExpiresAt = expiresAt
        };

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);

        _cache.Set(CacheKey, response, cacheOptions);

        _logger.LogInformation("Models cached until {ExpiresAt} ({ModelCount} models)", expiresAt, models.Count);

        return response;
    }
}
