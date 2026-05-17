using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CopilotSdk.Api.Hubs;
using CopilotSdk.Api.Models.Domain;
using GitHub.Copilot.SDK;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Policy-based permission handler for SDK built-in tools.
/// </summary>
public partial class PermissionPolicyService : IPermissionPolicyService
{
    private const string Approved = "approved";
    private const string DeniedByRules = "denied-by-rules";
    private const string LocalExecutorMode = "AutoApproveLocalExecutor";

    private readonly PermissionPolicyOptions _options;
    private readonly ILogger<PermissionPolicyService> _logger;
    private readonly IHubContext<SessionHub> _hubContext;

    public PermissionPolicyService(
        IOptions<PermissionPolicyOptions> options,
        ILogger<PermissionPolicyService> logger,
        IHubContext<SessionHub> hubContext)
    {
        _options = options.Value;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<PermissionRequestResult> HandlePermissionRequestAsync(PermissionRequest request, PermissionInvocation invocation)
    {
        var requestText = BuildRequestText(request);
        var decision = Evaluate(request, requestText);

        _logger.LogInformation(
            "Permission request {Decision}: kind={Kind}, session={SessionId}, toolCallId={ToolCallId}, reason={Reason}",
            decision.Approved ? "approved" : "denied",
            request.Kind,
            invocation.SessionId,
            request.ToolCallId,
            decision.Reason);

        await SendProgressAsync(invocation.SessionId, decision, request.Kind, requestText);

        return new PermissionRequestResult
        {
            Kind = decision.Approved ? Approved : DeniedByRules
        };
    }

    private PermissionDecision Evaluate(PermissionRequest request, string requestText)
    {
        if (!string.Equals(_options.Mode, LocalExecutorMode, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionDecision.Deny($"mode '{_options.Mode}' is not enabled");
        }

        var allowedKinds = new HashSet<string>(_options.AllowedKinds, StringComparer.OrdinalIgnoreCase);
        if (!allowedKinds.Contains(request.Kind))
        {
            return PermissionDecision.Deny($"kind '{request.Kind}' is not allowed");
        }

        foreach (var pattern in _options.DeniedCommandPatterns.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (Regex.IsMatch(requestText, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return PermissionDecision.Deny($"matched denied pattern '{pattern}'");
            }
        }

        var paths = ExtractWindowsPaths(requestText).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var deniedPath = paths.FirstOrDefault(path => !IsUnderAllowedRoot(path));
        if (deniedPath != null)
        {
            return PermissionDecision.Deny($"path '{deniedPath}' is outside allowed root '{_options.AllowedRoot}'");
        }

        return paths.Count > 0
            ? PermissionDecision.Approve($"all paths are under '{_options.AllowedRoot}'")
            : PermissionDecision.Approve("no absolute paths found in request");
    }

    private bool IsUnderAllowedRoot(string path)
    {
        try
        {
            var root = Path.GetFullPath(_options.AllowedRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(path.Trim().TrimEnd('.', ',', ';'));

            return fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            _logger.LogWarning(ex, "Could not normalize permission request path {Path}", path);
            return false;
        }
    }

    private static string BuildRequestText(PermissionRequest request)
    {
        var builder = new StringBuilder()
            .Append(request.Kind)
            .Append(' ')
            .Append(request.ToolCallId);

        if (request.ExtensionData != null)
        {
            foreach (var item in request.ExtensionData)
            {
                builder
                    .Append(' ')
                    .Append(item.Key)
                    .Append('=')
                    .Append(JsonSerializer.Serialize(item.Value));
            }
        }

        return builder.ToString();
    }

    private async Task SendProgressAsync(string sessionId, PermissionDecision decision, string kind, string requestText)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var summary = SummarizePermissionRequest(requestText);
        var eventDto = new SessionEventDto
        {
            Id = Guid.NewGuid(),
            Type = "session.progress",
            Timestamp = DateTimeOffset.UtcNow,
            Ephemeral = true,
            Data = new SessionProgressDataDto
            {
                Message = $"{(decision.Approved ? "Approved" : "Denied")} {kind} permission: {summary}",
                Phase = decision.Approved ? "permission-approved" : "permission-denied",
                IsActive = false
            }
        };

        await _hubContext.SendSessionEventAsync(sessionId, eventDto);
    }

    private static string SummarizePermissionRequest(string requestText)
    {
        var commandMatch = Regex.Match(
            requestText,
            @"command=(""(?<value>(?:\\""|[^""])*)""|(?<value>\S+))",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (commandMatch.Success)
        {
            var command = commandMatch.Groups["value"].Value
                .Replace("\\r", " ")
                .Replace("\\n", " ")
                .Replace("\\\"", "\"");
            return Truncate(command, 240);
        }

        return Truncate(requestText, 240);
    }

    private static string Truncate(string value, int maxLength)
    {
        var normalized = Regex.Replace(value, @"\s+", " ").Trim();
        return normalized.Length <= maxLength ? normalized : $"{normalized[..(maxLength - 3)]}...";
    }

    private static IEnumerable<string> ExtractWindowsPaths(string value)
    {
        foreach (Match match in WindowsPathRegex().Matches(value))
        {
            yield return match.Value.Trim().TrimEnd('.', ',', ';');
        }
    }

    [GeneratedRegex(@"[A-Za-z]:\\[^\r\n""'<>|]+", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsPathRegex();

    private readonly record struct PermissionDecision(bool Approved, string Reason)
    {
        public static PermissionDecision Approve(string reason) => new(true, reason);
        public static PermissionDecision Deny(string reason) => new(false, reason);
    }
}
