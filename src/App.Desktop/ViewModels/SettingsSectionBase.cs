using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Base class for all settings section ViewModels
/// </summary>
public abstract class SettingsSectionBase : ViewModelBase
{
    private readonly SettingsViewModel _parentSettings;
    private string _sectionTitle;
    private string _sectionDescription;
    private bool _hasUnsavedChanges;
    private bool _isInitializing = true;

    protected SettingsSectionBase(SettingsViewModel parentSettings, string sectionTitle)
    {
        _parentSettings = parentSettings ?? throw new ArgumentNullException(nameof(parentSettings));
        _sectionTitle = sectionTitle;
        _sectionDescription = string.Empty;

        ResetToDefaultCommand = new RelayCommand(ResetToDefault);
    }

    /// <summary>
    /// Gets the parent settings ViewModel
    /// </summary>
    protected SettingsViewModel ParentSettings => _parentSettings;

    /// <summary>
    /// Gets or sets the section title
    /// </summary>
    public string SectionTitle
    {
        get => _sectionTitle;
        set => SetProperty(ref _sectionTitle, value);
    }

    /// <summary>
    /// Gets the title (alias for SectionTitle for binding compatibility)
    /// </summary>
    public string Title => SectionTitle;

    /// <summary>
    /// Gets or sets the section description
    /// </summary>
    public string SectionDescription
    {
        get => _sectionDescription;
        set => SetProperty(ref _sectionDescription, value);
    }

    /// <summary>
    /// Gets or sets whether this section has unsaved changes
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Command to reset this section to defaults
    /// </summary>
    public ICommand ResetToDefaultCommand { get; }

    /// <summary>
    /// Marks this section as having unsaved changes
    /// </summary>
    protected void MarkAsChanged()
    {
        // Don't mark as changed during initialization
        if (_isInitializing)
            return;

        HasUnsavedChanges = true;
        _parentSettings?.MarkAsChanged();
    }

    /// <summary>
    /// Completes initialization and allows change tracking
    /// </summary>
    protected void CompleteInitialization()
    {
        _isInitializing = false;
    }

    /// <summary>
    /// Clears the unsaved changes flag
    /// </summary>
    public virtual void ClearChanges()
    {
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Resets this section's settings to defaults
    /// </summary>
    protected abstract void ResetToDefault();

    /// <summary>
    /// Validates this section's settings
    /// </summary>
    public virtual List<string> Validate()
    {
        return new List<string>();
    }

    /// <summary>
    /// Called when settings need to be refreshed from the service
    /// </summary>
    public abstract void RefreshFromSettings();

    /// <summary>
    /// Called when settings need to be applied to the service
    /// </summary>
    public abstract Task ApplySettingsAsync();
}