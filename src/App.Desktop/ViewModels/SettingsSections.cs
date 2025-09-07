using System;

namespace Lazarus.Desktop.ViewModels;

internal abstract class SettingsSectionBase
{
    protected SettingsSectionBase(SettingsViewModel settings, string title)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Title = title;
    }

    public SettingsViewModel Settings { get; }
    public string Title { get; }
}

internal sealed class GeneralSettingsViewModel : SettingsSectionBase
{ public GeneralSettingsViewModel(SettingsViewModel s) : base(s, "General") { } }
internal sealed class PathsSettingsViewModel : SettingsSectionBase
{ public PathsSettingsViewModel(SettingsViewModel s) : base(s, "Paths") { } }
internal sealed class OrchestratorSettingsViewModel : SettingsSectionBase
{ public OrchestratorSettingsViewModel(SettingsViewModel s) : base(s, "Orchestrator") { } }
internal sealed class RunnersSettingsViewModel : SettingsSectionBase
{ public RunnersSettingsViewModel(SettingsViewModel s) : base(s, "Runners") { } }
internal sealed class ModelsSettingsViewModel : SettingsSectionBase
{ public ModelsSettingsViewModel(SettingsViewModel s) : base(s, "Models") { } }
internal sealed class AudioSettingsViewModel : SettingsSectionBase
{ public AudioSettingsViewModel(SettingsViewModel s) : base(s, "Audio") { } }
internal sealed class RagSettingsViewModel : SettingsSectionBase
{ public RagSettingsViewModel(SettingsViewModel s) : base(s, "Embeddings / RAG") { } }
internal sealed class TrainingSettingsViewModel : SettingsSectionBase
{ public TrainingSettingsViewModel(SettingsViewModel s) : base(s, "Training") { } }
internal sealed class LoggingSettingsViewModel : SettingsSectionBase
{ public LoggingSettingsViewModel(SettingsViewModel s) : base(s, "Logging") { } }
internal sealed class AdvancedSettingsViewModel : SettingsSectionBase
{ public AdvancedSettingsViewModel(SettingsViewModel s) : base(s, "Advanced") { } }
internal sealed class AvatarsSettingsViewModel : SettingsSectionBase
{ public AvatarsSettingsViewModel(SettingsViewModel s) : base(s, "Avatars (future)") { } }

