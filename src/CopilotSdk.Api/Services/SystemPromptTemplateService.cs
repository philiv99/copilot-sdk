using System.Text.RegularExpressions;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for managing system prompt templates stored in docs/system_prompts.
/// </summary>
public class SystemPromptTemplateService : ISystemPromptTemplateService
{
    private const string SystemPromptsFolder = "docs/system_prompts";
    private const string InstructionsFileName = "copilot-instructions.md";

    private readonly ILogger<SystemPromptTemplateService> _logger;
    private readonly string _basePath;

    public SystemPromptTemplateService(
        ILogger<SystemPromptTemplateService> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        // Navigate from the API project to the repository root
        _basePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..\\.."));
    }

    /// <summary>
    /// Constructor for testing with custom base path.
    /// </summary>
    internal SystemPromptTemplateService(ILogger<SystemPromptTemplateService> logger, string basePath)
    {
        _logger = logger;
        _basePath = basePath;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemPromptTemplate>> GetTemplatesAsync()
    {
        var templates = new List<SystemPromptTemplate>();
        var systemPromptsPath = Path.Combine(_basePath, SystemPromptsFolder);

        _logger.LogDebug("Looking for system prompt templates in: {Path}", systemPromptsPath);

        if (!Directory.Exists(systemPromptsPath))
        {
            _logger.LogWarning("System prompts folder not found: {Path}", systemPromptsPath);
            return Task.FromResult<IReadOnlyList<SystemPromptTemplate>>(templates);
        }

        foreach (var directory in Directory.GetDirectories(systemPromptsPath))
        {
            var instructionsPath = Path.Combine(directory, InstructionsFileName);
            if (File.Exists(instructionsPath))
            {
                var folderName = Path.GetFileName(directory);
                templates.Add(new SystemPromptTemplate
                {
                    Name = folderName,
                    DisplayName = ToDisplayName(folderName)
                });
                _logger.LogDebug("Found template: {Name}", folderName);
            }
        }

        _logger.LogInformation("Found {Count} system prompt templates", templates.Count);
        return Task.FromResult<IReadOnlyList<SystemPromptTemplate>>(templates);
    }

    /// <inheritdoc />
    public async Task<string?> GetTemplateContentAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return null;
        }

        // Sanitize the template name to prevent path traversal
        var sanitizedName = Path.GetFileName(templateName);
        var instructionsPath = Path.Combine(_basePath, SystemPromptsFolder, sanitizedName, InstructionsFileName);

        _logger.LogDebug("Looking for template content at: {Path}", instructionsPath);

        if (!File.Exists(instructionsPath))
        {
            _logger.LogWarning("Template not found: {Name}", templateName);
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(instructionsPath);
            _logger.LogInformation("Loaded template {Name}, {Length} characters", templateName, content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read template {Name}", templateName);
            return null;
        }
    }

    /// <summary>
    /// Converts a folder name like "game_development" to "Game Development".
    /// </summary>
    private static string ToDisplayName(string folderName)
    {
        // Replace underscores with spaces and capitalize each word
        var words = folderName.Split('_', '-');
        var capitalizedWords = words.Select(word =>
            string.IsNullOrEmpty(word) ? word :
            char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant());
        return string.Join(" ", capitalizedWords);
    }
}
