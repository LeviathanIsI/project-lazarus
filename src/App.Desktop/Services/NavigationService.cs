using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lazarus.App.Desktop.ViewModels;
using Lazarus.App.Data;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Implementation of navigation service for managing view switching and navigation state
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private object? _currentViewModel;
    private NavigationSection _currentSection = NavigationSection.Dashboard;
    private bool _isNavigating = false; // Guard against re-entrancy
    private bool _isInitialized = false; // Track initialization state
    private DateTime _lastNavigationTime = DateTime.MinValue; // Track navigation timing
    private readonly TimeSpan _navigationTimeout = TimeSpan.FromSeconds(30); // Navigation timeout
    
    // ViewModel state preservation cache to prevent model corruption during navigation
    private readonly Dictionary<NavigationSection, object> _viewModelCache = new();

    /// <summary>
    /// Dictionary mapping navigation sections to their corresponding view model types
    /// </summary>
    private static readonly Dictionary<NavigationSection, Type> SectionViewModelMap = new()
    {
        { NavigationSection.Dashboard, typeof(DashboardViewModel) },
        { NavigationSection.Conversations, typeof(ConversationsViewModel) },
        { NavigationSection.ModelConfiguration, typeof(ModelConfigurationViewModel) },
        { NavigationSection.RunnerManager, typeof(RunnerManagerViewModel) },
        { NavigationSection.Jobs, typeof(JobsViewModel) },
        { NavigationSection.Datasets, typeof(DatasetsViewModel) },
        { NavigationSection.Images, typeof(ImagesViewModel) },
        { NavigationSection.Video, typeof(VideoViewModel) },
        { NavigationSection.Voice, typeof(VoiceViewModel) },
        { NavigationSection.ThreeDModels, typeof(ThreeDModelsViewModel) },
        { NavigationSection.Entities, typeof(EntitiesViewModel) },
        { NavigationSection.Training, typeof(TrainingViewModel) }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    /// <param name="logger">The logger instance</param>
    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("NavigationService constructor completed - initialization deferred");
        // NOTE: Do not navigate in constructor to avoid circular dependency
        // Initialization will be done explicitly after DI container is fully built
    }

    /// <summary>
    /// Event raised when a property value changes
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Event raised when navigation occurs
    /// </summary>
    public event EventHandler<NavigationEventArgs>? NavigationChanged;

    /// <summary>
    /// Gets the current view model being displayed
    /// </summary>
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (_currentViewModel != value)
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
    }

    /// <summary>
    /// Gets the current navigation section
    /// </summary>
    public NavigationSection CurrentSection
    {
        get => _currentSection;
        private set
        {
            if (_currentSection != value)
            {
                var previousSection = _currentSection;
                _currentSection = value;
                OnPropertyChanged(nameof(CurrentSection));
                
                // Raise navigation changed event
                NavigationChanged?.Invoke(this, new NavigationEventArgs(previousSection, value, CurrentViewModel));
            }
        }
    }

    /// <summary>
    /// Navigates to the specified view model type
    /// </summary>
    /// <typeparam name="TViewModel">The view model type to navigate to</typeparam>
    public void NavigateToView<TViewModel>() where TViewModel : class
    {
        try
        {
            _logger.LogInformation("Navigating to view model: {ViewModelType}", typeof(TViewModel).Name);

            // Get the view model from the service provider
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            
            // Update current view model
            CurrentViewModel = viewModel;

            // Find the corresponding navigation section
            var section = SectionViewModelMap.FirstOrDefault(kvp => kvp.Value == typeof(TViewModel));
            if (section.Key != default)
            {
                CurrentSection = section.Key;
            }

            _logger.LogInformation("Successfully navigated to {ViewModelType}", typeof(TViewModel).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to view model: {ViewModelType}", typeof(TViewModel).Name);
            throw;
        }
    }

    /// <summary>
    /// Navigates to the specified navigation section
    /// </summary>
    /// <param name="section">The navigation section to navigate to</param>
    public void NavigateToSection(NavigationSection section)
    {
        // Guard against re-entrancy
        if (_isNavigating)
        {
            _logger.LogWarning("Navigation re-entrancy detected for section: {Section} - ignoring duplicate call", section);
            return;
        }
        
        // Check for navigation timeout (prevent infinite loops)
        var now = DateTime.UtcNow;
        if (now - _lastNavigationTime < TimeSpan.FromMilliseconds(100))
        {
            _logger.LogWarning("Navigation throttled for section: {Section} - too frequent calls", section);
            return;
        }
        _lastNavigationTime = now;
        
        // Skip navigation if already on the same section
        if (_isInitialized && _currentSection == section && _currentViewModel != null)
        {
            _logger.LogDebug("Already on section: {Section} - skipping navigation", section);
            return;
        }
        
        try
        {
            _isNavigating = true;
            _logger.LogInformation("Navigating to section: {Section}", section);

            // Get the view model type for this section
            if (!SectionViewModelMap.TryGetValue(section, out var viewModelType))
            {
                throw new InvalidOperationException($"No view model mapped for section: {section}");
            }

            // CRITICAL FIX: Clear phantom entities BEFORE ViewModel access to prevent duplication
            ClearNavigationPhantoms(section);

            // SURGICAL FIX: Use cached ViewModel instance to preserve model state across navigation
            // This prevents ObservableCollection corruption and phantom model spawning
            object viewModel;
            if (_viewModelCache.TryGetValue(section, out var cachedViewModel))
            {
                viewModel = cachedViewModel;
                _logger.LogDebug("Retrieved cached ViewModel for section: {Section}", section);
            }
            else
            {
                // Only create new instance if not cached (first access)
                viewModel = _serviceProvider.GetRequiredService(viewModelType);
                _viewModelCache[section] = viewModel;
                _logger.LogDebug("Created and cached new ViewModel for section: {Section}", section);
            }
            
            // Update current view model and section atomically
            var previousSection = _currentSection;
            CurrentViewModel = viewModel;
            CurrentSection = section;
            _isInitialized = true;

            _logger.LogInformation("Successfully navigated from {PreviousSection} to {Section} with preserved state", previousSection, section);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to section: {Section}", section);
            throw;
        }
        finally
        {
            _isNavigating = false;
        }
    }

    /// <summary>
    /// Gets whether the specified section is currently selected
    /// </summary>
    /// <param name="section">The section to check</param>
    /// <returns>True if the section is selected; otherwise, false</returns>
    public bool IsCurrentSection(NavigationSection section)
    {
        return CurrentSection == section;
    }
    
    /// <summary>
    /// Initializes the navigation service with the default Dashboard view
    /// This should be called after the DI container is fully constructed
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("NavigationService already initialized - ignoring duplicate call");
            return;
        }
        
        _logger.LogInformation("Initializing NavigationService with Dashboard");
        NavigateToSection(NavigationSection.Dashboard);
    }

    /// <summary>
    /// PHANTOM ELIMINATION: Clears tracked entity phantoms during navigation transitions
    /// </summary>
    /// <param name="targetSection">The section being navigated to</param>
    private void ClearNavigationPhantoms(NavigationSection targetSection)
    {
        try
        {
            // Clear phantoms when navigating to/from ModelConfiguration to prevent model state corruption
            var phantomClearingSections = new[] 
            { 
                NavigationSection.ModelConfiguration, 
                NavigationSection.Conversations,
                NavigationSection.RunnerManager 
            };

            if (phantomClearingSections.Contains(_currentSection) || phantomClearingSections.Contains(targetSection))
            {
                var dbContext = _serviceProvider.GetRequiredService<LazarusDbContext>();
                var trackedCount = dbContext.GetTrackedEntityCount();
                
                if (trackedCount > 0)
                {
                    _logger.LogDebug("PHANTOM CLEARING: Detected {TrackedCount} tracked entities before navigation - clearing phantoms", trackedCount);
                    dbContext.ClearEntityTrackingPhantoms();
                    
                    var clearedCount = dbContext.GetTrackedEntityCount();
                    _logger.LogInformation("PHANTOM CLEARING: Navigation safety applied - {ClearedCount} entities remain tracked", clearedCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PHANTOM CLEARING: Non-critical warning during navigation phantom clearing");
        }
    }

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    /// <param name="propertyName">The name of the property that changed</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}