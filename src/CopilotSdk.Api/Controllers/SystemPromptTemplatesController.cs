using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CopilotSdk.Api.Controllers;

/// <summary>
/// Controller for managing system prompt templates.
/// </summary>
[ApiController]
[Route("api/copilot/system-prompt-templates")]
[Produces("application/json")]
public class SystemPromptTemplatesController : ControllerBase
{
    private readonly ISystemPromptTemplateService _templateService;
    private readonly ILogger<SystemPromptTemplatesController> _logger;

    public SystemPromptTemplatesController(
        ISystemPromptTemplateService templateService,
        ILogger<SystemPromptTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a list of available system prompt templates.
    /// </summary>
    /// <returns>List of templates with their names and display names.</returns>
    /// <response code="200">Returns the list of templates.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SystemPromptTemplatesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemPromptTemplatesResponse>> GetTemplates()
    {
        _logger.LogInformation("Getting list of system prompt templates");
        var templates = await _templateService.GetTemplatesAsync();
        
        return Ok(new SystemPromptTemplatesResponse
        {
            Templates = templates.Select(t => new SystemPromptTemplateDto
            {
                Name = t.Name,
                DisplayName = t.DisplayName
            }).ToList()
        });
    }

    /// <summary>
    /// Gets the content of a specific system prompt template.
    /// </summary>
    /// <param name="name">The name of the template.</param>
    /// <returns>The template content.</returns>
    /// <response code="200">Returns the template content.</response>
    /// <response code="404">Template not found.</response>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(SystemPromptTemplateContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemPromptTemplateContentResponse>> GetTemplateContent(string name)
    {
        _logger.LogInformation("Getting content for template: {Name}", name);
        var content = await _templateService.GetTemplateContentAsync(name);
        
        if (content == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Template not found",
                Detail = $"System prompt template '{name}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new SystemPromptTemplateContentResponse
        {
            Name = name,
            Content = content
        });
    }
}

/// <summary>
/// Response containing a list of system prompt templates.
/// </summary>
public class SystemPromptTemplatesResponse
{
    /// <summary>
    /// The list of available templates.
    /// </summary>
    public List<SystemPromptTemplateDto> Templates { get; set; } = new();
}

/// <summary>
/// Data transfer object for a system prompt template.
/// </summary>
public class SystemPromptTemplateDto
{
    /// <summary>
    /// The unique name of the template (folder name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A display-friendly name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Response containing the content of a system prompt template.
/// </summary>
public class SystemPromptTemplateContentResponse
{
    /// <summary>
    /// The name of the template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The content of the template (copilot-instructions.md).
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
