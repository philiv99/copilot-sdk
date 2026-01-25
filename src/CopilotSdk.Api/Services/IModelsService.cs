using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for retrieving available AI models.
/// </summary>
public interface IModelsService
{
    /// <summary>
    /// Gets the list of available AI models.
    /// The list is cached for one week.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing available models.</returns>
    Task<ModelsResponse> GetModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh of the cached models list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing refreshed models.</returns>
    Task<ModelsResponse> RefreshModelsAsync(CancellationToken cancellationToken = default);
}
