using System.ComponentModel.DataAnnotations;

namespace CopilotSdk.Api.Models.Requests;

/// <summary>
/// Request model for refining a system message prompt.
/// </summary>
public class RefinePromptRequest
{
    /// <summary>
    /// The user's current system message content to be refined.
    /// </summary>
    [Required(ErrorMessage = "Content is required.")]
    [MinLength(1, ErrorMessage = "Content cannot be empty.")]
    [MaxLength(50000, ErrorMessage = "Content cannot exceed 50,000 characters.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional context about the application being built.
    /// This helps the LLM provide more relevant refinements.
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Context cannot exceed 5,000 characters.")]
    public string? Context { get; set; }

    /// <summary>
    /// Optional focus area for refinement: "clarity", "detail", "constraints", or "all".
    /// Defaults to "all" if not specified.
    /// </summary>
    public string? RefinementFocus { get; set; }
}
