namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response containing a list of users (admin endpoint).
/// </summary>
public class UserListResponse
{
    /// <summary>List of users.</summary>
    public List<UserResponse> Users { get; set; } = new();

    /// <summary>Total count of users matching the filter.</summary>
    public int TotalCount { get; set; }
}
