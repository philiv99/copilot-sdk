namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Information about an available AI model.
/// </summary>
public class ModelInfo
{
    /// <summary>
    /// The model identifier used in API calls.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Description of the model's capabilities.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Response containing available AI models.
/// </summary>
public class ModelsResponse
{
    /// <summary>
    /// List of available models.
    /// </summary>
    public List<ModelInfo> Models { get; set; } = new();

    /// <summary>
    /// Timestamp when the models list was cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Timestamp when the cache will expire.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Timestamp when the models.json configuration file was last updated.
    /// Null if the models were loaded from the hardcoded fallback.
    /// </summary>
    public DateTime? ModelsLastUpdated { get; set; }
}
