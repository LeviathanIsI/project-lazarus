using System;

namespace Lazarus.Desktop.ViewModels;

public abstract class SettingsSectionBase
{
    protected SettingsSectionBase(SettingsViewModel settings, string title)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Title = title;
    }

    public SettingsViewModel Settings { get; }
    public string Title { get; }
}

public sealed class GeneralSettingsViewModel : SettingsSectionBase
{ public GeneralSettingsViewModel(SettingsViewModel s) : base(s, "General") { } }
public sealed class PathsSettingsViewModel : SettingsSectionBase
{ public PathsSettingsViewModel(SettingsViewModel s) : base(s, "Paths") { } }
public sealed class OrchestratorSettingsViewModel : SettingsSectionBase
{ public OrchestratorSettingsViewModel(SettingsViewModel s) : base(s, "Orchestrator") { } }
public sealed class RunnersSettingsViewModel : SettingsSectionBase
{ public RunnersSettingsViewModel(SettingsViewModel s) : base(s, "Runners") { } }
public sealed class ModelsSettingsViewModel : SettingsSectionBase
{ public ModelsSettingsViewModel(SettingsViewModel s) : base(s, "Models") { } }
public sealed class AudioSettingsViewModel : SettingsSectionBase
{ public AudioSettingsViewModel(SettingsViewModel s) : base(s, "Audio") { } }
public sealed class RagSettingsViewModel : SettingsSectionBase
{ public RagSettingsViewModel(SettingsViewModel s) : base(s, "Embeddings / RAG") { } }
public sealed class TrainingSettingsViewModel : SettingsSectionBase
{ public TrainingSettingsViewModel(SettingsViewModel s) : base(s, "Training") { } }
public sealed class LoggingSettingsViewModel : SettingsSectionBase
{ public LoggingSettingsViewModel(SettingsViewModel s) : base(s, "Logging") { } }
public sealed class AdvancedSettingsViewModel : SettingsSectionBase
{ public AdvancedSettingsViewModel(SettingsViewModel s) : base(s, "Advanced") { } }
public sealed class AvatarsSettingsViewModel : SettingsSectionBase
{ public AvatarsSettingsViewModel(SettingsViewModel s) : base(s, "Avatars (future)") { } }

// Rich section with commands is implemented in a separate class file (GlobalActionsViewModel.cs)
