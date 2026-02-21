using System.Text;
using System.Text.Json;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for discovering agents and teams from the filesystem and composing team system messages.
/// Reads agent definitions from docs/agents/ and team presets from docs/teams/.
/// </summary>
public class AgentTeamService : IAgentTeamService
{
    private const string AgentsFolder = "docs/agents";
    private const string TeamsFolder = "docs/teams";
    private const string AgentMetadataFile = "agent.json";
    private const string AgentPromptFile = "prompt.md";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<AgentTeamService> _logger;
    private readonly string _basePath;

    public AgentTeamService(
        ILogger<AgentTeamService> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        // Navigate from the API project to the repository root
        _basePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..\\.."));
    }

    /// <summary>
    /// Constructor for testing with custom base path.
    /// </summary>
    internal AgentTeamService(ILogger<AgentTeamService> logger, string basePath)
    {
        _logger = logger;
        _basePath = basePath;
    }

    /// <inheritdoc />
    public Task<List<AgentDefinition>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = new List<AgentDefinition>();
        var agentsPath = Path.Combine(_basePath, AgentsFolder);

        _logger.LogDebug("Looking for agent definitions in: {Path}", agentsPath);

        if (!Directory.Exists(agentsPath))
        {
            _logger.LogWarning("Agents folder not found: {Path}", agentsPath);
            return Task.FromResult(agents);
        }

        foreach (var directory in Directory.GetDirectories(agentsPath))
        {
            var metadataPath = Path.Combine(directory, AgentMetadataFile);
            if (!File.Exists(metadataPath))
                continue;

            try
            {
                var json = File.ReadAllText(metadataPath);
                var agent = JsonSerializer.Deserialize<AgentDefinition>(json, JsonOptions);
                if (agent != null)
                {
                    // Use folder name as ID if not set
                    if (string.IsNullOrEmpty(agent.Id))
                        agent.Id = Path.GetFileName(directory);

                    agents.Add(agent);
                    _logger.LogDebug("Found agent: {Id} ({Name})", agent.Id, agent.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read agent definition from {Path}", metadataPath);
            }
        }

        _logger.LogInformation("Found {Count} agent definitions", agents.Count);
        return Task.FromResult(agents);
    }

    /// <inheritdoc />
    public async Task<AgentDetailResponse?> GetAgentDetailAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            return null;

        // Sanitize to prevent path traversal
        var sanitizedId = Path.GetFileName(agentId);
        var agentDir = Path.Combine(_basePath, AgentsFolder, sanitizedId);

        if (!Directory.Exists(agentDir))
        {
            _logger.LogWarning("Agent not found: {Id}", agentId);
            return null;
        }

        var metadataPath = Path.Combine(agentDir, AgentMetadataFile);
        if (!File.Exists(metadataPath))
        {
            _logger.LogWarning("Agent metadata file not found: {Path}", metadataPath);
            return null;
        }

        try
        {
            var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var agent = JsonSerializer.Deserialize<AgentDefinition>(metadataJson, JsonOptions);
            if (agent == null)
                return null;

            if (string.IsNullOrEmpty(agent.Id))
                agent.Id = sanitizedId;

            var promptContent = string.Empty;
            var promptPath = Path.Combine(agentDir, AgentPromptFile);
            if (File.Exists(promptPath))
            {
                promptContent = await File.ReadAllTextAsync(promptPath, cancellationToken);
            }

            return new AgentDetailResponse
            {
                Agent = agent,
                PromptContent = promptContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read agent detail for {Id}", agentId);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<List<TeamDefinition>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = new List<TeamDefinition>();
        var teamsPath = Path.Combine(_basePath, TeamsFolder);

        _logger.LogDebug("Looking for team definitions in: {Path}", teamsPath);

        if (!Directory.Exists(teamsPath))
        {
            _logger.LogWarning("Teams folder not found: {Path}", teamsPath);
            return Task.FromResult(teams);
        }

        foreach (var file in Directory.GetFiles(teamsPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var team = JsonSerializer.Deserialize<TeamDefinition>(json, JsonOptions);
                if (team != null)
                {
                    // Use filename (without extension) as ID if not set
                    if (string.IsNullOrEmpty(team.Id))
                        team.Id = Path.GetFileNameWithoutExtension(file);

                    teams.Add(team);
                    _logger.LogDebug("Found team: {Id} ({Name})", team.Id, team.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read team definition from {Path}", file);
            }
        }

        _logger.LogInformation("Found {Count} team definitions", teams.Count);
        return Task.FromResult(teams);
    }

    /// <inheritdoc />
    public async Task<TeamDetailResponse?> GetTeamDetailAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return null;

        var teams = await GetTeamsAsync(cancellationToken);
        var team = teams.FirstOrDefault(t => t.Id.Equals(teamId, StringComparison.OrdinalIgnoreCase));
        if (team == null)
        {
            _logger.LogWarning("Team not found: {Id}", teamId);
            return null;
        }

        // Resolve agent definitions for the team
        var agents = await GetAgentsAsync(cancellationToken);
        var resolvedAgents = team.Agents
            .Select(agentId => agents.FirstOrDefault(a => a.Id.Equals(agentId, StringComparison.OrdinalIgnoreCase)))
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();

        return new TeamDetailResponse
        {
            Team = team,
            ResolvedAgents = resolvedAgents
        };
    }

    /// <inheritdoc />
    public async Task<ComposeTeamMessageResponse> ComposeTeamSystemMessageAsync(
        ComposeTeamMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // 1. If a base template is specified, load it first
        if (!string.IsNullOrWhiteSpace(request.TemplateName))
        {
            // Templates are in docs/system_prompts/{name}/copilot-instructions.md
            var templatePath = Path.Combine(_basePath, "docs", "system_prompts", 
                Path.GetFileName(request.TemplateName), "copilot-instructions.md");
            if (File.Exists(templatePath))
            {
                var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
                sb.AppendLine(templateContent);
                sb.AppendLine();
            }
            else
            {
                _logger.LogWarning("Template not found: {Name}", request.TemplateName);
            }
        }

        // 2. Compose the team section header
        if (request.AgentIds.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("# Team Configuration");
            sb.AppendLine();
            sb.AppendLine($"**Workflow Pattern**: {request.WorkflowPattern}");
            sb.AppendLine();

            // 3. Load and append each agent's prompt
            var agentCount = 0;
            foreach (var agentId in request.AgentIds)
            {
                var detail = await GetAgentDetailAsync(agentId, cancellationToken);
                if (detail == null)
                {
                    _logger.LogWarning("Skipping unknown agent: {Id}", agentId);
                    continue;
                }

                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine(detail.PromptContent.TrimEnd());
                sb.AppendLine();
                agentCount++;
            }

            _logger.LogInformation("Composed system message with {Count} agents, pattern: {Pattern}",
                agentCount, request.WorkflowPattern);
        }

        // 4. Append custom content if provided
        if (!string.IsNullOrWhiteSpace(request.CustomContent))
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("# Additional Instructions");
            sb.AppendLine();
            sb.AppendLine(request.CustomContent.TrimEnd());
            sb.AppendLine();
        }

        return new ComposeTeamMessageResponse
        {
            ComposedContent = sb.ToString().TrimEnd(),
            AgentCount = request.AgentIds.Count,
            WorkflowPattern = request.WorkflowPattern
        };
    }
}
