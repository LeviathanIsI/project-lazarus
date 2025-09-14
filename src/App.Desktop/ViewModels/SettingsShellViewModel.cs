using System.Collections.ObjectModel;
using System.Windows.Input;
using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for the settings shell navigation
/// </summary>
public class SettingsShellViewModel : ViewModelBase
{
    private readonly SettingsViewModel _settingsViewModel;
    private SettingsSectionBase? _selectedSection;
    private string _searchText = string.Empty;

    public SettingsShellViewModel(SettingsViewModel settingsViewModel)
    {
        _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

        // Initialize commands
        NavigateToSectionCommand = new RelayCommand<SettingsSectionBase>(NavigateToSection);
        ApplyCommand = new RelayCommand(async () => await ApplySettingsAsync());
        CancelCommand = new RelayCommand(Cancel);
        CloseCommand = new RelayCommand(Close);

        // Set initial selection
        SelectedSection = _settingsViewModel.Sections.FirstOrDefault();
    }

    /// <summary>
    /// Gets the settings ViewModel
    /// </summary>
    public SettingsViewModel SettingsViewModel => _settingsViewModel;

    /// <summary>
    /// Gets the collection of settings sections for navigation
    /// </summary>
    public ObservableCollection<SettingsSectionBase> Sections => _settingsViewModel.Sections;

    /// <summary>
    /// Gets or sets the currently selected section
    /// </summary>
    public SettingsSectionBase? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                _settingsViewModel.SelectedSection = value;
            }
        }
    }

    /// <summary>
    /// Gets whether there are unsaved changes
    /// </summary>
    public bool HasUnsavedChanges => _settingsViewModel.HasUnsavedChanges;

    /// <summary>
    /// Gets or sets the search text
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    /// <summary>
    /// Gets the save command from settings view model
    /// </summary>
    public ICommand SaveCommand => _settingsViewModel.SaveCommand;

    /// <summary>
    /// Gets the reset all command from settings view model
    /// </summary>
    public ICommand ResetAllCommand => _settingsViewModel.ResetAllCommand;

    /// <summary>
    /// Command to navigate to a section
    /// </summary>
    public ICommand NavigateToSectionCommand { get; }

    /// <summary>
    /// Command to apply settings
    /// </summary>
    public ICommand ApplyCommand { get; }

    /// <summary>
    /// Command to cancel changes
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Command to close the settings window
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Navigates to the specified section
    /// </summary>
    private void NavigateToSection(SettingsSectionBase? section)
    {
        if (section != null)
        {
            SelectedSection = section;
        }
    }

    /// <summary>
    /// Applies settings changes
    /// </summary>
    private async Task ApplySettingsAsync()
    {
        if (_settingsViewModel.SaveCommand.CanExecute(null))
        {
            _settingsViewModel.SaveCommand.Execute(null);
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Cancels pending changes
    /// </summary>
    private void Cancel()
    {
        _settingsViewModel.CancelCommand.Execute(null);
    }

    /// <summary>
    /// Closes the settings window
    /// </summary>
    private void Close()
    {
        // Check for unsaved changes
        if (HasUnsavedChanges)
        {
            // TODO: Prompt user to save changes
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Do you want to save them before closing?",
                "Unsaved Changes",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _ = ApplySettingsAsync();
            }
            else if (result == System.Windows.MessageBoxResult.Cancel)
            {
                return; // Don't close
            }
        }

        // Close the window (will be handled by the view)
        OnCloseRequested();
    }

    /// <summary>
    /// Event raised when the window should be closed
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raises the CloseRequested event
    /// </summary>
    protected virtual void OnCloseRequested()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}