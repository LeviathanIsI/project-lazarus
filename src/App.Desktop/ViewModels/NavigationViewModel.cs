using Lazarus.Desktop.Services;
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

            // Create view instance based on navigation
            CurrentView = e.ViewName switch
            {
                "Dashboard" => new Views.DashboardView(),
                "ChatSessions" => new Views.ChatSessionsView(),
                "Images" => new Views.ImagesView(),
                "Videos" => new Views.VideosView(),
                "Entities" => new Views.EntitiesView(),
                "ThreeDModels" => new Views.ThreeDModelsView(),
                "Audio" => new Views.AudioView(),
                _ => new Views.DashboardView()
            };

            // Notify property changes
            OnPropertyChanged(nameof(CurrentViewName));
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