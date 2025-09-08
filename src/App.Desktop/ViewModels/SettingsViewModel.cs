using System.Collections.ObjectModel;
using System.Windows.Input;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Main ViewModel for the settings interface
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly Lazarus.Desktop.Services.IHardwareInfoService _hardwareInfoService;
    private AppSettings _settings;
    private bool _hasUnsavedChanges;
    private SettingsSectionBase? _selectedSection;
    private string _searchText = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, Lazarus.Desktop.Services.IHardwareInfoService hardwareInfoService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _hardwareInfoService = hardwareInfoService ?? throw new ArgumentNullException(nameof(hardwareInfoService));
        _settings = _settingsService.Current;

        // Initialize sections
        Sections = new ObservableCollection<SettingsSectionBase>
        {
            new GeneralSettingsViewModel(this),
            new PathsSettingsViewModel(this),
            new OrchestratorSettingsViewModel(this),
            new RunnersSettingsViewModel(this, _hardwareInfoService),
            new ModelsSettingsViewModel(this),
            new AudioSettingsViewModel(this),
            new RagSettingsViewModel(this),
            new TrainingSettingsViewModel(this),
            new LoggingSettingsViewModel(this),
            new AdvancedSettingsViewModel(this),
            new AvatarsSettingsViewModel(this),
            new GlobalActionsViewModel(this)
        };

        // Set default selection
        SelectedSection = Sections.FirstOrDefault();

        // Initialize commands
        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync(), () => HasUnsavedChanges);
        CancelCommand = new RelayCommand(CancelChanges);
        ResetAllCommand = new RelayCommand(async () => await ResetAllToDefaultsAsync());
        SearchCommand = new RelayCommand<string>(Search);

        // Subscribe to settings changes
        _settingsService.SettingsChanged += OnSettingsChanged;

        // Load initial settings
        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// Gets the collection of settings sections
    /// </summary>
    public ObservableCollection<SettingsSectionBase> Sections { get; }

    /// <summary>
    /// Gets or sets the current settings
    /// </summary>
    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    /// <summary>
    /// Gets or sets whether there are unsaved changes
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                // Only raise if command is initialized
                if (SaveCommand is RelayCommand saveCmd)
                {
                    saveCmd.RaiseCanExecuteChanged();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected section
    /// </summary>
    public SettingsSectionBase? SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    /// <summary>
    /// Gets or sets the search text
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterSections();
            }
        }
    }

    /// <summary>
    /// Command to save settings
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Command to cancel changes
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Command to reset all settings to defaults
    /// </summary>
    public ICommand ResetAllCommand { get; }

    /// <summary>
    /// Command to search settings
    /// </summary>
    public ICommand SearchCommand { get; }

    /// <summary>
    /// Marks the ViewModel as having unsaved changes
    /// </summary>
    public void MarkAsChanged()
    {
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Loads settings from the service
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            Settings = await _settingsService.LoadAsync();
            
            // Refresh all sections
            foreach (var section in Sections)
            {
                section.RefreshFromSettings();
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves settings to the service
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Apply settings from all sections
            foreach (var section in Sections)
            {
                await section.ApplySettingsAsync();
            }

            // Save to service
            await _settingsService.SaveAsync(Settings);

            // Clear changed flags
            HasUnsavedChanges = false;
            foreach (var section in Sections)
            {
                section.ClearChanges();
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels pending changes
    /// </summary>
    private void CancelChanges()
    {
        // Reload settings from service
        Settings = _settingsService.Current;

        // Refresh all sections
        foreach (var section in Sections)
        {
            section.RefreshFromSettings();
            section.ClearChanges();
        }

        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Resets all settings to defaults
    /// </summary>
    private async Task ResetAllToDefaultsAsync()
    {
        try
        {
            await _settingsService.ResetToDefaultsAsync();
            await LoadSettingsAsync();
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Failed to reset settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for settings
    /// </summary>
    private void Search(string? searchText)
    {
        SearchText = searchText ?? string.Empty;
    }

    /// <summary>
    /// Filters sections based on search text
    /// </summary>
    private void FilterSections()
    {
        // TODO: Implement section filtering based on search text
        // This could highlight matching settings or filter visible sections
    }

    /// <summary>
    /// Handles settings changed events from the service
    /// </summary>
    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        // Update local settings
        Settings = e.NewSettings;

        // Refresh all sections
        foreach (var section in Sections)
        {
            section.RefreshFromSettings();
        }
    }

    protected override void OnDisposing()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        base.OnDisposing();
    }
}
