namespace CopilotSdk.Api.Models.Responses;

/// <summary>
/// Response for avatar preset listing.
/// </summary>
public class AvatarPresetsResponse
{
    /// <summary>Available preset avatars.</summary>
    public List<AvatarPresetItem> Presets { get; set; } = new();
}

/// <summary>
/// A single preset avatar option.
/// </summary>
public class AvatarPresetItem
{
    /// <summary>Preset identifier name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Emoji or unicode character representing the avatar.</summary>
    public string Emoji { get; set; } = string.Empty;
}
