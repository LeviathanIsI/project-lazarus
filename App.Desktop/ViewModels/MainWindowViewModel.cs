using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using App.Shared.Enums;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for the MainWindow containing global application preferences
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly UserPreferencesService _preferencesService;

    public MainWindowViewModel(UserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        
        // Initialize available options
        ViewModes = new ObservableCollection<ViewMode>
        {
            ViewMode.Novice,
            ViewMode.Enthusiast, 
            ViewMode.Developer
        };

        Themes = new ObservableCollection<ThemeMode>
        {
            ThemeMode.Dark,
            ThemeMode.Light,
            ThemeMode.Cyberpunk,
            ThemeMode.Minimal
        };

        // Subscribe to service changes
        _preferencesService.PropertyChanged += OnPreferencesServicePropertyChanged;
        
        Console.WriteLine("[MainWindowViewModel] Initialized with dual customization system");
    }

    #region Properties

    /// <summary>
    /// Available view complexity modes
    /// </summary>
    public ObservableCollection<ViewMode> ViewModes { get; }

    /// <summary>
    /// Available visual themes
    /// </summary>
    public ObservableCollection<ThemeMode> Themes { get; }

    /// <summary>
    /// Current view mode (complexity level)
    /// </summary>
    public ViewMode CurrentViewMode
    {
        get => _preferencesService.CurrentViewMode;
        set => _preferencesService.CurrentViewMode = value;
    }

    /// <summary>
    /// Current visual theme
    /// </summary>
    public ThemeMode CurrentTheme
    {
        get => _preferencesService.CurrentTheme;
        set 
        {
            Console.WriteLine($"[THEME DEBUG] MainWindowViewModel.CurrentTheme setter called: {value}");
            _preferencesService.CurrentTheme = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Event Handlers

    private void OnPreferencesServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward property changes from service to UI
        if (e.PropertyName == nameof(UserPreferencesService.CurrentViewMode))
        {
            OnPropertyChanged(nameof(CurrentViewMode));
        }
        else if (e.PropertyName == nameof(UserPreferencesService.CurrentTheme))
        {
            OnPropertyChanged(nameof(CurrentTheme));
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Unsubscribe from service to prevent memory leaks
            _preferencesService.PropertyChanged -= OnPreferencesServicePropertyChanged;
            _disposed = true;
        }
    }

    #endregion
}