using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;

namespace CopilotSdk.Api.Tools;

/// <summary>
/// Provides sample/demo tools for testing the tool execution service.
/// </summary>
public static class DemoTools
{
    /// <summary>
    /// Gets the definition for the echo tool.
    /// </summary>
    public static ToolDefinition EchoToolDefinition => new()
    {
        Name = "echo_tool",
        Description = "Echoes the input message back to the caller. Useful for testing tool execution.",
        Parameters = new List<ToolParameter>
        {
            new()
            {
                Name = "message",
                Type = "string",
                Description = "The message to echo back",
                Required = true
            },
            new()
            {
                Name = "uppercase",
                Type = "boolean",
                Description = "Whether to convert the message to uppercase",
                Required = false
            }
        }
    };

    /// <summary>
    /// Gets the definition for the get_current_time tool.
    /// </summary>
    public static ToolDefinition GetCurrentTimeDefinition => new()
    {
        Name = "get_current_time",
        Description = "Returns the current date and time. Useful for time-sensitive operations.",
        Parameters = new List<ToolParameter>
        {
            new()
            {
                Name = "format",
                Type = "string",
                Description = "The format string for the date/time (e.g., 'yyyy-MM-dd HH:mm:ss'). If not provided, uses ISO 8601 format.",
                Required = false
            },
            new()
            {
                Name = "timezone",
                Type = "string",
                Description = "The timezone to use (e.g., 'UTC', 'America/New_York'). Defaults to UTC.",
                Required = false
            }
        }
    };

    /// <summary>
    /// Handler for the echo tool.
    /// </summary>
    public static Task<object?> EchoToolHandler(IDictionary<string, object?> args, CancellationToken cancellationToken)
    {
        var message = args.TryGetValue("message", out var msgValue) ? msgValue?.ToString() ?? "" : "";
        var uppercase = args.TryGetValue("uppercase", out var upperValue) && 
                       (upperValue is bool boolVal && boolVal || 
                        upperValue is string strVal && bool.TryParse(strVal, out var parsed) && parsed);

        var result = uppercase ? message.ToUpperInvariant() : message;
        return Task.FromResult<object?>(new EchoToolResult(result, message.Length));
    }

    /// <summary>
    /// Handler for the get_current_time tool.
    /// </summary>
    public static Task<object?> GetCurrentTimeHandler(IDictionary<string, object?> args, CancellationToken cancellationToken)
    {
        var format = args.TryGetValue("format", out var formatValue) ? formatValue?.ToString() : null;
        var timezone = args.TryGetValue("timezone", out var tzValue) ? tzValue?.ToString() : "UTC";

        DateTimeOffset now;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone ?? "UTC");
            now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to UTC if timezone not found
            now = DateTimeOffset.UtcNow;
        }

        string formattedTime;
        try
        {
            formattedTime = string.IsNullOrEmpty(format) 
                ? now.ToString("o") // ISO 8601
                : now.ToString(format);
        }
        catch (FormatException)
        {
            formattedTime = now.ToString("o");
        }

        return Task.FromResult<object?>(new GetCurrentTimeResult(
            formattedTime, 
            timezone ?? "UTC",
            now.Offset.ToString()
        ));
    }

    /// <summary>
    /// Registers all demo tools with the tool execution service.
    /// </summary>
    /// <param name="toolService">The tool execution service to register tools with.</param>
    public static void RegisterAllDemoTools(IToolExecutionService toolService)
    {
        ArgumentNullException.ThrowIfNull(toolService);

        toolService.RegisterTool(EchoToolDefinition, EchoToolHandler);
        toolService.RegisterTool(GetCurrentTimeDefinition, GetCurrentTimeHandler);
    }

    /// <summary>
    /// Gets all demo tool definitions.
    /// </summary>
    /// <returns>A list of all demo tool definitions.</returns>
    public static List<ToolDefinition> GetAllDefinitions()
    {
        return new List<ToolDefinition>
        {
            EchoToolDefinition,
            GetCurrentTimeDefinition
        };
    }
}
