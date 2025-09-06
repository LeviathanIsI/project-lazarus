using Lazarus.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels
{
    /// <summary>
    /// Main ViewModel that serves as the root of the application's view model hierarchy.
    /// Manages the overall application state and coordinates between different view models.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly IThemeService _themeService;
        private readonly IOrchestratorClient _orchestratorClient;

        public MainViewModel(
            ILogger<MainViewModel> logger,
            NavigationViewModel navigationViewModel,
            INavigationService navigationService,
            IThemeService themeService,
            IOrchestratorClient orchestratorClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Navigation = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _orchestratorClient = orchestratorClient ?? throw new ArgumentNullException(nameof(orchestratorClient));

            // Subscribe to orchestrator health changes
            _orchestratorClient.HealthStatusChanged += OnOrchestratorHealthChanged;

            _logger.LogDebug("MainViewModel initialized");
        }

        /// <summary>
        /// Gets the navigation view model for managing view transitions.
        /// </summary>
        public NavigationViewModel Navigation { get; }

        /// <summary>
        /// Gets a value indicating whether the orchestrator is currently healthy.
        /// </summary>
        public bool IsOrchestratorHealthy => _orchestratorClient.IsHealthy;

        /// <summary>
        /// Gets the current theme name.
        /// </summary>
        public string CurrentTheme => _themeService.CurrentTheme;

        /// <summary>
        /// Gets the available themes.
        /// </summary>
        public IReadOnlyList<string> AvailableThemes => _themeService.AvailableThemes;

        /// <summary>
        /// Gets the title for the main window.
        /// </summary>
        public string WindowTitle => "Lazarus - Local LLM Orchestrator";

        /// <summary>
        /// Command to change the application theme.
        /// </summary>
        public RelayCommand<string> ChangeThemeCommand => new(ChangeTheme, CanChangeTheme);

        /// <summary>
        /// Command to refresh the orchestrator connection.
        /// </summary>
        public RelayCommand RefreshConnectionCommand => new(async () => await RefreshConnectionAsync(), () => !IsDisposed);

        private bool CanChangeTheme(string? themeName) =>
            !IsDisposed && !string.IsNullOrWhiteSpace(themeName) && _themeService.AvailableThemes.Contains(themeName);

        private void ChangeTheme(string? themeName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(themeName))
                return;

            if (_themeService.ApplyTheme(themeName))
            {
                OnPropertyChanged(nameof(CurrentTheme));
                _logger.LogInformation("Theme changed to {ThemeName}", themeName);
            }
        }

        private async Task RefreshConnectionAsync()
        {
            ThrowIfDisposed();

            try
            {
                await _orchestratorClient.CheckHealthAsync().ConfigureAwait(false);
                _logger.LogInformation("Orchestrator connection refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh orchestrator connection");
            }
        }

        private void OnOrchestratorHealthChanged(object? sender, HealthStatusChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsOrchestratorHealthy));

            if (e.IsHealthy)
            {
                _logger.LogInformation("Orchestrator connection restored");
            }
            else
            {
                _logger.LogWarning("Orchestrator connection lost: {ErrorMessage}", e.ErrorMessage);
            }
        }

        protected override void OnDisposing()
        {
            _orchestratorClient.HealthStatusChanged -= OnOrchestratorHealthChanged;
            _logger.LogDebug("MainViewModel disposed");
        }
    }
}