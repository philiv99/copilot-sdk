namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// User roles in the application.
/// </summary>
public enum UserRole
{
    /// <summary>Player — can view sessions and send messages, but cannot create sessions or manage config.</summary>
    Player = 0,
    /// <summary>Creator — can create sessions, send messages, view sessions.</summary>
    Creator = 1,
    /// <summary>Admin — full access including user management and client configuration.</summary>
    Admin = 2
}
