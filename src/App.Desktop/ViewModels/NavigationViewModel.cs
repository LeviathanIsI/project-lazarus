using Lazarus.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for managing navigation between different views in the application.
    /// Integrates with the navigation service and provides commands for view transitions.
    /// </summary>
    public class NavigationViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly ILogger<NavigationViewModel> _logger;
        private string _selectedView = "Dashboard";
        private bool _isSidebarExpanded = true;
        private object? _currentView;

        public NavigationViewModel(
            INavigationService navigationService,
            ILogger<NavigationViewModel> logger)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to navigation service events
            _navigationService.Navigated += OnNavigated;
            _navigationService.PropertyChanged += OnNavigationServicePropertyChanged;

            // Initialize commands
            NavigateCommand = new RelayCommand<string>(Navigate, CanNavigate);
            ToggleSidebarCommand = new RelayCommand(_ => IsSidebarExpanded = !IsSidebarExpanded, _ => !IsDisposed);
            GoBackCommand = new RelayCommand(_ => _navigationService.GoBack(), _ => _navigationService.CanGoBack && !IsDisposed);
            GoForwardCommand = new RelayCommand(_ => _navigationService.GoForward(), _ => _navigationService.CanGoForward && !IsDisposed);

            _logger.LogDebug("NavigationViewModel initialized");
        }

        /// <summary>
        /// Gets or sets the currently selected view.
        /// </summary>
        public string SelectedView
        {
            get => _selectedView;
            set => SetProperty(ref _selectedView, value);
        }

        /// <summary>
        /// Gets or sets whether the sidebar is expanded.
        /// </summary>
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set => SetProperty(ref _isSidebarExpanded, value);
        }

        /// <summary>
        /// Gets or sets the current view instance.
        /// </summary>
        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        /// <summary>
        /// Gets a value indicating whether back navigation is possible.
        /// </summary>
        public bool CanGoBack => _navigationService.CanGoBack;

        /// <summary>
        /// Gets a value indicating whether forward navigation is possible.
        /// </summary>
        public bool CanGoForward => _navigationService.CanGoForward;

        /// <summary>
        /// Gets the current view name from the navigation service.
        /// </summary>
        public string? CurrentViewName => _navigationService.CurrentView;

        /// <summary>
        /// Command to navigate to a specific view.
        /// </summary>
        public ICommand NavigateCommand { get; }

        /// <summary>
        /// Command to toggle the sidebar expansion state.
        /// </summary>
        public ICommand ToggleSidebarCommand { get; }

        /// <summary>
        /// Command to navigate back to the previous view.
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// Command to navigate forward to the next view.
        /// </summary>
        public ICommand GoForwardCommand { get; }

        private bool CanNavigate(string? viewName) =>
            !IsDisposed && !string.IsNullOrWhiteSpace(viewName);

        private void Navigate(string? viewName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(viewName))
                return;

            try
            {
                _navigationService.NavigateTo(viewName);
                _logger.LogDebug("Navigated to view: {ViewName}", viewName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to navigate to view: {ViewName}", viewName);
            }
        }

        private void OnNavigated(object? sender, NavigationEventArgs e)
        {
            if (IsDisposed)
                return;

            SelectedView = e.ViewName;

            // Create view instance based on navigation, with resilient Settings fallback
            CurrentView = e.ViewName switch
            {
                "Dashboard" => new Views.DashboardView(),
                "ChatSessions" => CreateChatSessionsView(),
                "Images" => new Views.ImagesView(),
                "Videos" => new Views.VideosView(),
                "Entities" => new Views.EntitiesView(),
                "Models" => new Views.ModelsView(),
                "ThreeDModels" => new Views.ThreeDModelsView(),
                "Audio" => CreateAudioViewSafe(),
                "Settings" => CreateSettingsViewSafe(),
                _ => new Views.DashboardView()
            };

            // Notify property changes
            OnPropertyChanged(nameof(CurrentViewName));
        }

        private object CreateAudioViewSafe()
        {
            try
            {
                var view = new Views.AudioView();
                // Wire a scoped ViewModel via DI
                if (App.ServiceProvider != null)
                {
                    try
                    {
                        var vm = App.ServiceProvider.GetRequiredService<AudioViewModel>();
                        view.DataContext = vm;
                    }
                    catch { /* leave DataContext null if DI not ready */ }
                }
                return view;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create AudioView");
                // Fall back to last-known-safe view to avoid stale content perception
                return new Views.DashboardView();
            }
        }

        private object CreateChatSessionsView()
        {
            try
            {
                var view = new Views.ChatSessionsView();
                if (App.ServiceProvider != null)
                {
                    var locator = App.ServiceProvider.GetService<ViewModelLocator>();
                    if (locator != null)
                    {
                        var vm = locator.ChatSessionsViewModel;
                        view.DataContext = vm;
                        // Trigger a refresh when navigating here in case DB was still initializing earlier
                        _ = vm.RefreshConversationsAsync();
                    }
                }
                return view;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create ChatSessionsView");
                return new Views.DashboardView();
            }
        }

        private static object CreateSettingsViewSafe()
        {
            try
            {
                // Create the ViewModels using DI
                var settingsViewModel = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
                
                // Try to create SettingsShell first
                try
                {
                    var settingsShell = new Views.SettingsShell();
                    var settingsShellViewModel = new SettingsShellViewModel(settingsViewModel);
                    settingsShell.DataContext = settingsShellViewModel;
                    return settingsShell;
                }
                catch
                {
                    // Fallback to SettingsView if SettingsShell fails
                    var settingsView = new Views.SettingsView();
                    settingsView.DataContext = settingsViewModel;
                    return settingsView;
                }
            }
            catch (Exception ex)
            {
                // If all else fails, return a view with an error message
                System.Diagnostics.Debug.WriteLine("[Settings] Failed to create settings view: " + ex);
                var errorView = new Views.SettingsView();
                // Create a minimal SettingsViewModel manually if DI fails
                try
                {
                    var settingsService = App.ServiceProvider.GetService<Lazarus.Shared.Settings.ISettingsService>();
                    var hardwareInfoService = App.ServiceProvider.GetService<Lazarus.Desktop.Services.IHardwareInfoService>();
                    if (settingsService != null && hardwareInfoService != null)
                    {
                        errorView.DataContext = new SettingsViewModel(settingsService, hardwareInfoService);
                    }
                }
                catch
                {
                    // Unable to create any valid DataContext
                }
                return errorView;
            }
        }

        private void OnNavigationServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (IsDisposed)
                return;

            // Forward relevant property change notifications
            switch (e.PropertyName)
            {
                case nameof(INavigationService.CanGoBack):
                    OnPropertyChanged(nameof(CanGoBack));
                    break;
                case nameof(INavigationService.CanGoForward):
                    OnPropertyChanged(nameof(CanGoForward));
                    break;
                case nameof(INavigationService.CurrentView):
                    OnPropertyChanged(nameof(CurrentViewName));
                    break;
            }
        }

        protected override void OnDisposing()
        {
            _navigationService.Navigated -= OnNavigated;
            _navigationService.PropertyChanged -= OnNavigationServicePropertyChanged;
            _logger.LogDebug("NavigationViewModel disposed");
        }
    }
}
