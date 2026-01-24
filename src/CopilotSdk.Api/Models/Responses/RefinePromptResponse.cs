namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response model containing the refined prompt content.
/// </summary>
public class RefinePromptResponse
{
    /// <summary>
    /// The refined and improved system message content.
    /// </summary>
    public string RefinedContent { get; set; } = string.Empty;

    /// <summary>
    /// The original content that was submitted for refinement.
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>
    /// Tracks how many times this content has been refined.
    /// Starts at 0 and increments with each successful refinement.
    /// </summary>
    public int IterationCount { get; set; }

    /// <summary>
    /// Indicates whether the refinement was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the refinement failed. Null if successful.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
