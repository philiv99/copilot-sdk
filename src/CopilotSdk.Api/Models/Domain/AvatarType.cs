namespace CopilotSdk.Api.Models.Domain;

/// <summary>
/// Type of avatar a user has selected.
/// </summary>
public enum AvatarType
{
    /// <summary>Default generic avatar.</summary>
    Default = 0,
    /// <summary>A preset avatar from the built-in collection.</summary>
    Preset = 1,
    /// <summary>A custom uploaded avatar image (base64).</summary>
    Custom = 2
}
