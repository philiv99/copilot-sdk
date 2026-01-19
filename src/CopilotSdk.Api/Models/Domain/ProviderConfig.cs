namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Configuration for a custom API provider (BYOK - Bring Your Own Key).
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Type of provider (e.g., "openai", "azure").
    /// </summary>
    public string Type { get; set; } = "openai";

    /// <summary>
    /// Base URL for the provider's API.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Bearer token for authentication (takes precedence over ApiKey).
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Wire API format (e.g., "openai", "azure").
    /// </summary>
    public string? WireApi { get; set; }
}
