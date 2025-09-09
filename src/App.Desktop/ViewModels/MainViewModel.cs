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
        private readonly IOrchestratorRunnerClient _runnerClient;
        private readonly System.Threading.Timer _runnerTimer;
        private readonly IAppState _appState;

        public MainViewModel(
            ILogger<MainViewModel> logger,
            NavigationViewModel navigationViewModel,
            INavigationService navigationService,
            IThemeService themeService,
            IOrchestratorClient orchestratorClient,
            IOrchestratorRunnerClient runnerClient,
            IAppState appState)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Navigation = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _orchestratorClient = orchestratorClient ?? throw new ArgumentNullException(nameof(orchestratorClient));
            _runnerClient = runnerClient ?? throw new ArgumentNullException(nameof(runnerClient));
            _appState = appState ?? throw new ArgumentNullException(nameof(appState));

            // Subscribe to orchestrator health changes
            _orchestratorClient.HealthStatusChanged += OnOrchestratorHealthChanged;

            // Prime runner status and start a light polling timer
            _ = RefreshRunnerStatusAsync();
            _ = RefreshConnectionAsync();
            _runnerTimer = new System.Threading.Timer(async _ => await RefreshRunnerStatusAsync(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
            _runnerClient.RunnerStatusChanged += OnRunnerStatusChanged;

            // Reflect adapter selections in HUD
            _appState.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(LoadedLoraName));
                OnPropertyChanged(nameof(LoadedTokenizerName));
                OnPropertyChanged(nameof(LoadedEmbeddingName));
                OnPropertyChanged(nameof(HasLora));
                OnPropertyChanged(nameof(HasTokenizer));
                OnPropertyChanged(nameof(HasEmbedding));
                OnPropertyChanged(nameof(LoraScale));
            };

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

        public bool IsRunnerRunning
        {
            get => _isRunnerRunning;
            private set => SetProperty(ref _isRunnerRunning, value);
        }
        private bool _isRunnerRunning;

        public string? LoadedModelName
        {
            get => _loadedModelName;
            private set => SetProperty(ref _loadedModelName, value);
        }
        private string? _loadedModelName;

        // HUD: Adapters overview (proxied from AppState)
        public string? LoadedLoraName => string.IsNullOrWhiteSpace(_appState.LoadedLora)
            ? null
            : System.IO.Path.GetFileNameWithoutExtension(_appState.LoadedLora);
        public string? LoadedTokenizerName => string.IsNullOrWhiteSpace(_appState.LoadedTokenizer)
            ? null
            : System.IO.Path.GetFileName(_appState.LoadedTokenizer);
        public string? LoadedEmbeddingName => string.IsNullOrWhiteSpace(_appState.LoadedEmbedding)
            ? null
            : System.IO.Path.GetFileNameWithoutExtension(_appState.LoadedEmbedding);
        public double? LoraScale => _appState.LoraScale;
        public bool HasLora => !string.IsNullOrWhiteSpace(LoadedLoraName);
        public bool HasTokenizer => !string.IsNullOrWhiteSpace(LoadedTokenizerName);
        public bool HasEmbedding => !string.IsNullOrWhiteSpace(LoadedEmbeddingName);

        public string OrchestratorStatusTooltip => IsOrchestratorHealthy ? "Orchestrator: Healthy" : "Orchestrator: Unreachable";
        public string RunnerStatusTooltip => IsOrchestratorHealthy
            ? (IsRunnerRunning ? "Runner: Running" : "Runner: Idle")
            : "Runner: Unknown (orchestrator offline)";

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
            OnPropertyChanged(nameof(OrchestratorStatusTooltip));
            OnPropertyChanged(nameof(RunnerStatusTooltip));

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
            try { _runnerTimer?.Dispose(); } catch { }
            _orchestratorClient.HealthStatusChanged -= OnOrchestratorHealthChanged;
            _runnerClient.RunnerStatusChanged -= OnRunnerStatusChanged;
            _logger.LogDebug("MainViewModel disposed");
        }

        private async Task RefreshRunnerStatusAsync()
        {
            try
            {
                var status = await _runnerClient.GetStatusAsync().ConfigureAwait(false);
                // Use dispatcher to update UI-bound properties
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsRunnerRunning = status.IsRunning;
                    LoadedModelName = string.IsNullOrWhiteSpace(status.ModelPath)
                        ? null
                        : System.IO.Path.GetFileNameWithoutExtension(status.ModelPath);
                    OnPropertyChanged(nameof(RunnerStatusTooltip));
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to refresh runner status");
            }
        }

        private async void OnRunnerStatusChanged(object? sender, RunnerProcessStatus e)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsRunnerRunning = e.IsRunning;
                    LoadedModelName = string.IsNullOrWhiteSpace(e.ModelPath) ? null : System.IO.Path.GetFileNameWithoutExtension(e.ModelPath);
                    OnPropertyChanged(nameof(RunnerStatusTooltip));
                });
            }
            catch { }
        }
    }
}
