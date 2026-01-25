using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for retrieving available AI models.
/// </summary>
[ApiController]
[Route("api/copilot/models")]
[Produces("application/json")]
public class ModelsController : ControllerBase
{
    private readonly IModelsService _modelsService;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(IModelsService modelsService, ILogger<ModelsController> logger)
    {
        _modelsService = modelsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of available AI models.
    /// The list is cached for one week.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing available models.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ModelsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelsResponse>> GetModels(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching available models");
        var response = await _modelsService.GetModelsAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Forces a refresh of the cached models list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing refreshed models.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ModelsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelsResponse>> RefreshModels(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing models cache");
        var response = await _modelsService.RefreshModelsAsync(cancellationToken);
        return Ok(response);
    }
}
