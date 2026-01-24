using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for prompt refinement operations using the Copilot LLM.
/// </summary>
[ApiController]
[Route("api/copilot")]
[Produces("application/json")]
public class PromptRefinementController : ControllerBase
{
    private readonly IPromptRefinementService _refinementService;
    private readonly ILogger<PromptRefinementController> _logger;

    public PromptRefinementController(
        IPromptRefinementService refinementService,
        ILogger<PromptRefinementController> logger)
    {
        _refinementService = refinementService;
        _logger = logger;
    }

    /// <summary>
    /// Refines a system message prompt using the Copilot LLM.
    /// Sends the content to an LLM with instructions to expand and improve it
    /// as a clearer requirements statement.
    /// </summary>
    /// <param name="request">The refinement request containing the content to refine.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refined content or an error response.</returns>
    /// <response code="200">Refinement completed (check Success property for result).</response>
    /// <response code="400">Invalid request (empty content, content too long).</response>
    /// <response code="503">Copilot client not connected.</response>
    [HttpPost("refine-prompt")]
    [ProducesResponseType(typeof(RefinePromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RefinePromptResponse>> RefinePrompt(
        [FromBody] RefinePromptRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[REFINE CONTROLLER] === REQUEST RECEIVED === Content length: {Length}, Focus: {Focus}", 
            request.Content?.Length ?? 0, request.RefinementFocus ?? "none");

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["content"] = new[] { "Content is required and cannot be empty." }
            }));
        }

        // Validate refinement focus if provided
        if (!string.IsNullOrWhiteSpace(request.RefinementFocus))
        {
            var validFocuses = new[] { "clarity", "detail", "constraints", "all" };
            if (!validFocuses.Contains(request.RefinementFocus, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["refinementFocus"] = new[] { $"Invalid refinement focus. Valid values are: {string.Join(", ", validFocuses)}" }
                }));
            }
        }

        var response = await _refinementService.RefinePromptAsync(request, cancellationToken);

        _logger.LogInformation("[REFINE CONTROLLER] === RESPONSE === Success: {Success}, Error: {Error}, RefLength: {Length}",
            response.Success, response.ErrorMessage ?? "none", response.RefinedContent?.Length ?? 0);

        // Return 503 if client is not connected
        if (!response.Success && response.ErrorMessage?.Contains("not connected") == true)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Service Unavailable",
                Detail = response.ErrorMessage,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }

        return Ok(response);
    }
}
