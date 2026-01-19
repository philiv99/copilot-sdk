using CopilotSdk.Api.Models.Domain;
using Microsoft.AspNetCore.SignalR;

namespace CopilotSdk.Api.Hubs;

/// <summary>
/// SignalR hub for real-time session event streaming.
/// Clients can join/leave session groups to receive events for specific sessions.
/// </summary>
public class SessionHub : Hub
{
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(ILogger<SessionHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Joins a session group to receive events for that session.
    /// </summary>
    /// <param name="sessionId">The session ID to join.</param>
    public async Task JoinSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("Client {ConnectionId} tried to join with empty session ID", Context.ConnectionId);
            throw new HubException("Session ID is required");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(sessionId));
        _logger.LogInformation("Client {ConnectionId} joined session {SessionId}", Context.ConnectionId, sessionId);
        
        // Notify the client they successfully joined
        await Clients.Caller.SendAsync("JoinedSession", sessionId);
    }

    /// <summary>
    /// Leaves a session group to stop receiving events for that session.
    /// </summary>
    /// <param name="sessionId">The session ID to leave.</param>
    public async Task LeaveSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("Client {ConnectionId} tried to leave with empty session ID", Context.ConnectionId);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(sessionId));
        _logger.LogInformation("Client {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
        
        // Notify the client they successfully left
        await Clients.Caller.SendAsync("LeftSession", sessionId);
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to SessionHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">Exception that caused the disconnection, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client {ConnectionId} disconnected with error", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from SessionHub", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets the SignalR group name for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The group name.</returns>
    internal static string GetGroupName(string sessionId) => $"session-{sessionId}";
}

/// <summary>
/// Extension methods for sending events to session groups via SignalR.
/// </summary>
public static class SessionHubExtensions
{
    /// <summary>
    /// Sends a session event to all clients in a session group.
    /// </summary>
    /// <param name="hubContext">The hub context.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="eventDto">The event to send.</param>
    public static async Task SendSessionEventAsync(
        this IHubContext<SessionHub> hubContext,
        string sessionId,
        SessionEventDto eventDto)
    {
        var groupName = SessionHub.GetGroupName(sessionId);
        await hubContext.Clients.Group(groupName).SendAsync("OnSessionEvent", eventDto);
    }

    /// <summary>
    /// Sends a streaming delta event to all clients in a session group.
    /// </summary>
    /// <param name="hubContext">The hub context.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="eventDto">The delta event to send.</param>
    public static async Task SendStreamingDeltaAsync(
        this IHubContext<SessionHub> hubContext,
        string sessionId,
        SessionEventDto eventDto)
    {
        var groupName = SessionHub.GetGroupName(sessionId);
        await hubContext.Clients.Group(groupName).SendAsync("OnStreamingDelta", eventDto);
    }

    /// <summary>
    /// Sends a session error to all clients in a session group.
    /// </summary>
    /// <param name="hubContext">The hub context.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="error">The error data.</param>
    public static async Task SendSessionErrorAsync(
        this IHubContext<SessionHub> hubContext,
        string sessionId,
        SessionErrorDataDto error)
    {
        var groupName = SessionHub.GetGroupName(sessionId);
        var eventDto = new SessionEventDto
        {
            Id = Guid.NewGuid(),
            Type = "session.error",
            Timestamp = DateTimeOffset.UtcNow,
            Data = error
        };
        await hubContext.Clients.Group(groupName).SendAsync("OnSessionError", eventDto);
    }
}
