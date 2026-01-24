using System.Collections.Concurrent;

namespace CopilotSdk.Api.Middleware;

/// <summary>
/// Simple rate limiting middleware for the prompt refinement endpoint.
/// Limits requests per client IP to prevent abuse.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _clientRequests = new();
    
    /// <summary>
    /// Maximum requests allowed within the time window.
    /// </summary>
    private const int MaxRequests = 10;
    
    /// <summary>
    /// Time window for rate limiting in seconds.
    /// </summary>
    private const int TimeWindowSeconds = 60;
    
    /// <summary>
    /// Paths to apply rate limiting to.
    /// </summary>
    private static readonly string[] RateLimitedPaths = new[]
    {
        "/api/copilot/refine-prompt"
    };

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        // Only apply rate limiting to specific paths
        if (path != null && RateLimitedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            var clientId = GetClientIdentifier(context);
            
            if (!IsRequestAllowed(clientId))
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on path {Path}", clientId, path);
                
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/problem+json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = $"Rate limit exceeded. Maximum {MaxRequests} requests per {TimeWindowSeconds} seconds allowed.",
                    retryAfter = TimeWindowSeconds
                });
                
                return;
            }
        }
        
        await _next(context);
    }

    /// <summary>
    /// Gets a unique identifier for the client.
    /// Uses X-Forwarded-For header if behind a proxy, otherwise uses remote IP.
    /// </summary>
    private static string GetClientIdentifier(HttpContext context)
    {
        // Check for X-Forwarded-For header (common when behind a proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the list (original client)
            return forwardedFor.Split(',').First().Trim();
        }
        
        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Checks if a request from the given client is allowed based on rate limiting rules.
    /// </summary>
    private bool IsRequestAllowed(string clientId)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-TimeWindowSeconds);
        
        var rateLimitInfo = _clientRequests.AddOrUpdate(
            clientId,
            // Add new entry
            _ => new RateLimitInfo { Requests = new List<DateTime> { now } },
            // Update existing entry
            (_, existing) =>
            {
                // Remove requests outside the time window
                existing.Requests.RemoveAll(r => r < windowStart);
                
                // Check if limit exceeded
                if (existing.Requests.Count >= MaxRequests)
                {
                    return existing; // Don't add the new request
                }
                
                // Add the new request
                existing.Requests.Add(now);
                return existing;
            }
        );
        
        // Clean up old entries periodically
        if (_clientRequests.Count > 1000)
        {
            CleanupOldEntries(windowStart);
        }
        
        return rateLimitInfo.Requests.Count(r => r >= windowStart) <= MaxRequests;
    }

    /// <summary>
    /// Removes entries with no recent requests.
    /// </summary>
    private void CleanupOldEntries(DateTime windowStart)
    {
        var keysToRemove = _clientRequests
            .Where(kvp => kvp.Value.Requests.All(r => r < windowStart))
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _clientRequests.TryRemove(key, out _);
        }
    }

    private class RateLimitInfo
    {
        public List<DateTime> Requests { get; set; } = new();
    }
}

/// <summary>
/// Extension methods for rate limiting middleware.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Adds rate limiting middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
