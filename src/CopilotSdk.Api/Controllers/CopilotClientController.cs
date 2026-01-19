using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for managing the Copilot client lifecycle.
/// </summary>
[ApiController]
[Route("api/copilot/client")]
[Produces("application/json")]
public class CopilotClientController : ControllerBase
{
    private readonly ICopilotClientService _clientService;
    private readonly ILogger<CopilotClientController> _logger;

    public CopilotClientController(ICopilotClientService clientService, ILogger<CopilotClientController> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current status of the Copilot client.
    /// </summary>
    /// <returns>The current client status.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ClientStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<ClientStatusResponse> GetStatus()
    {
        var status = _clientService.GetStatus();
        return Ok(status);
    }

    /// <summary>
    /// Gets the current configuration of the Copilot client.
    /// </summary>
    /// <returns>The current client configuration.</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ClientConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<ClientConfigResponse> GetConfig()
    {
        var config = _clientService.GetConfig();
        return Ok(config);
    }

    /// <summary>
    /// Updates the Copilot client configuration.
    /// </summary>
    /// <param name="request">The configuration updates to apply.</param>
    /// <returns>The updated configuration.</returns>
    [HttpPut("config")]
    [ProducesResponseType(typeof(ClientConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ClientConfigResponse> UpdateConfig([FromBody] UpdateClientConfigRequest request)
    {
        _clientService.UpdateConfig(request);
        var config = _clientService.GetConfig();
        return Ok(config);
    }

    /// <summary>
    /// Starts the Copilot client.
    /// </summary>
    /// <returns>The client status after starting.</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ClientStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClientStatusResponse>> Start(CancellationToken cancellationToken)
    {
        await _clientService.StartAsync(cancellationToken);
        var status = _clientService.GetStatus();
        return Ok(status);
    }

    /// <summary>
    /// Stops the Copilot client gracefully.
    /// </summary>
    /// <returns>The client status after stopping.</returns>
    [HttpPost("stop")]
    [ProducesResponseType(typeof(ClientStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClientStatusResponse>> Stop()
    {
        await _clientService.StopAsync();
        var status = _clientService.GetStatus();
        return Ok(status);
    }

    /// <summary>
    /// Forces an immediate stop of the Copilot client.
    /// </summary>
    /// <returns>The client status after force stopping.</returns>
    [HttpPost("force-stop")]
    [ProducesResponseType(typeof(ClientStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClientStatusResponse>> ForceStop()
    {
        await _clientService.ForceStopAsync();
        var status = _clientService.GetStatus();
        return Ok(status);
    }

    /// <summary>
    /// Pings the Copilot server to check connectivity.
    /// </summary>
    /// <param name="request">Optional ping request with a message.</param>
    /// <returns>The ping response with latency information.</returns>
    [HttpPost("ping")]
    [ProducesResponseType(typeof(PingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PingResponse>> Ping([FromBody] PingRequest? request, CancellationToken cancellationToken)
    {
        var response = await _clientService.PingAsync(request ?? new PingRequest(), cancellationToken);
        return Ok(response);
    }
}
