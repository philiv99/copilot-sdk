using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json;
using CopilotSdk.Api.Models.Domain;
using Microsoft.Extensions.AI;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service implementation for managing and executing custom tools in Copilot sessions.
/// </summary>
public class ToolExecutionService : IToolExecutionService
{
    private readonly ConcurrentDictionary<string, RegisteredTool> _tools = new();
    private readonly ILogger<ToolExecutionService> _logger;

    /// <summary>
    /// Represents a registered tool with its definition and handler.
    /// </summary>
    private record RegisteredTool(
        ToolDefinition Definition,
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> Handler);

    public ToolExecutionService(ILogger<ToolExecutionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void RegisterTool(
        ToolDefinition definition,
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(handler);

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new ArgumentException("Tool name cannot be null or empty", nameof(definition));
        }

        var tool = new RegisteredTool(definition, handler);
        
        if (_tools.TryAdd(definition.Name, tool))
        {
            _logger.LogInformation("Registered tool: {ToolName}", definition.Name);
        }
        else
        {
            // Update existing tool
            _tools[definition.Name] = tool;
            _logger.LogInformation("Updated tool registration: {ToolName}", definition.Name);
        }
    }

    /// <inheritdoc/>
    public bool UnregisterTool(string toolName)
    {
        ArgumentNullException.ThrowIfNull(toolName);

        if (_tools.TryRemove(toolName, out _))
        {
            _logger.LogInformation("Unregistered tool: {ToolName}", toolName);
            return true;
        }

        _logger.LogWarning("Tool not found for unregistration: {ToolName}", toolName);
        return false;
    }

    /// <inheritdoc/>
    public async Task<object?> ExecuteToolAsync(
        string toolName,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolName);
        ArgumentNullException.ThrowIfNull(arguments);

        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new KeyNotFoundException($"Tool '{toolName}' is not registered");
        }

        _logger.LogInformation("Executing tool: {ToolName}", toolName);

        try
        {
            var result = await tool.Handler(arguments, cancellationToken);
            _logger.LogInformation("Tool execution completed: {ToolName}", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed: {ToolName}", toolName);
            throw;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<ToolDefinition> GetRegisteredTools()
    {
        return _tools.Values.Select(t => t.Definition).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public bool IsToolRegistered(string toolName)
    {
        return _tools.ContainsKey(toolName);
    }

    /// <inheritdoc/>
    public ICollection<AIFunction> BuildAIFunctions(IEnumerable<ToolDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var functions = new List<AIFunction>();

        foreach (var definition in definitions)
        {
            var aiFunction = CreateAIFunction(definition);
            functions.Add(aiFunction);
        }

        return functions;
    }

    /// <inheritdoc/>
    public void ClearTools()
    {
        _tools.Clear();
        _logger.LogInformation("Cleared all registered tools");
    }

    /// <summary>
    /// Creates an AIFunction from a tool definition.
    /// </summary>
    private AIFunction CreateAIFunction(ToolDefinition definition)
    {
        // Capture the definition name for the closure
        var toolName = definition.Name;
        var toolDescription = definition.Description;

        // Create a delegate that handles tool execution
        // Using the delegate-based approach that works with AIFunctionFactory
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler = async (args, ct) =>
        {
            // Check if we have a registered handler for this tool
            if (_tools.TryGetValue(toolName, out var tool))
            {
                return await tool.Handler(args, ct);
            }

            // Default no-op handler returns a message indicating no handler
            _logger.LogWarning(
                "No handler registered for tool {ToolName}, returning default response",
                toolName);
            return $"Tool '{toolName}' executed (no handler registered)";
        };

        // Use the simple delegate-based Create method with name and description
        return AIFunctionFactory.Create(handler, toolName, toolDescription);
    }
}
