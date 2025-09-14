using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for Avatar settings section
/// </summary>
public class AvatarsSettingsViewModel : SettingsSectionBase
{
    // Placeholder properties for future avatar features
    private bool _enableAvatar;
    private string _avatarStyle = "Default";
    private string _avatarVoice = "Default";

    public AvatarsSettingsViewModel(SettingsViewModel settings) : base(settings, "Avatars")
    {
        SectionDescription = "Configure virtual assistant avatar appearance and behavior";

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    public bool EnableAvatar
    {
        get => _enableAvatar;
        set { if (SetProperty(ref _enableAvatar, value)) MarkAsChanged(); }
    }

    public string AvatarStyle
    {
        get => _avatarStyle;
        set { if (SetProperty(ref _avatarStyle, value)) MarkAsChanged(); }
    }

    public string AvatarVoice
    {
        get => _avatarVoice;
        set { if (SetProperty(ref _avatarVoice, value)) MarkAsChanged(); }
    }

    public override void RefreshFromSettings()
    {
        // Avatar settings will be loaded from settings in future
        EnableAvatar = false;
        AvatarStyle = "Default";
        AvatarVoice = "Default";
    }

    public override async Task ApplySettingsAsync()
    {
        // Avatar settings will be saved in future
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        EnableAvatar = false;
        AvatarStyle = "Default";
        AvatarVoice = "Default";
    }
}