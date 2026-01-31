namespace CopilotSdk.Api.Services;

/// <summary>
/// Represents a system prompt template available for selection.
/// </summary>
public class SystemPromptTemplate
{
    /// <summary>
    /// The unique name of the template (folder name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A display-friendly name derived from the folder name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Service interface for managing system prompt templates.
/// </summary>
public interface ISystemPromptTemplateService
{
    /// <summary>
    /// Gets a list of available system prompt templates.
    /// Templates are folders in docs/system_prompts that contain a copilot-instructions.md file.
    /// </summary>
    /// <returns>List of available templates.</returns>
    Task<IReadOnlyList<SystemPromptTemplate>> GetTemplatesAsync();

    /// <summary>
    /// Gets the content of a specific system prompt template.
    /// </summary>
    /// <param name="templateName">The name of the template (folder name).</param>
    /// <returns>The content of the copilot-instructions.md file, or null if not found.</returns>
    Task<string?> GetTemplateContentAsync(string templateName);
}
