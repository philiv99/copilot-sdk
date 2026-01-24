using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service interface for refining system message prompts using the Copilot LLM.
/// </summary>
public interface IPromptRefinementService
{
    /// <summary>
    /// Refines a system message prompt by sending it to the LLM with instructions
    /// to expand and improve the content as a clearer requirements statement.
    /// </summary>
    /// <param name="request">The refinement request containing the content to refine.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response containing the refined content.</returns>
    Task<RefinePromptResponse> RefinePromptAsync(RefinePromptRequest request, CancellationToken cancellationToken = default);
}
