using CopilotSdk.Api.Models.Domain;
using Microsoft.Extensions.AI;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service for managing and executing custom tools in Copilot sessions.
/// </summary>
public interface IToolExecutionService
{
    /// <summary>
    /// Registers a tool definition with its execution handler.
    /// </summary>
    /// <param name="definition">The tool definition describing the tool.</param>
    /// <param name="handler">The function that executes the tool, receiving parameters and returning a result.</param>
    void RegisterTool(ToolDefinition definition, Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler);

    /// <summary>
    /// Unregisters a tool by its name.
    /// </summary>
    /// <param name="toolName">The name of the tool to unregister.</param>
    /// <returns>True if the tool was found and removed; otherwise, false.</returns>
    bool UnregisterTool(string toolName);

    /// <summary>
    /// Executes a registered tool by name with the given arguments.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute.</param>
    /// <param name="arguments">The arguments to pass to the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the tool is not registered.</exception>
    Task<object?> ExecuteToolAsync(string toolName, IDictionary<string, object?> arguments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered tool definitions.
    /// </summary>
    /// <returns>A read-only collection of registered tool definitions.</returns>
    IReadOnlyCollection<ToolDefinition> GetRegisteredTools();

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    /// <param name="toolName">The name of the tool to check.</param>
    /// <returns>True if the tool is registered; otherwise, false.</returns>
    bool IsToolRegistered(string toolName);

    /// <summary>
    /// Builds a collection of AIFunction objects from tool definitions.
    /// If handlers are registered for the tools, they will be used for execution.
    /// For tools without registered handlers, a default no-op handler is used.
    /// </summary>
    /// <param name="definitions">The tool definitions to convert.</param>
    /// <returns>A collection of AIFunction objects ready for use with the SDK.</returns>
    ICollection<AIFunction> BuildAIFunctions(IEnumerable<ToolDefinition> definitions);

    /// <summary>
    /// Clears all registered tools.
    /// </summary>
    void ClearTools();
}
