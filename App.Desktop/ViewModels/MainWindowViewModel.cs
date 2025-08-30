using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.Utilities;
using Lazarus.Desktop.ViewModels.Images;
using Lazarus.Desktop.ViewModels.Video;
using Lazarus.Desktop.ViewModels.Voice;
using Lazarus.Desktop.ViewModels.Entities;
using Lazarus.Desktop.ViewModels.ThreeDModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for the MainWindow containing global application preferences
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly UserPreferencesService _preferencesService;
    private readonly SystemStateViewModel _systemState;
    private readonly INavigationService _navigationService;
    private readonly Lazarus.Desktop.Services.Dashboard.DashboardViewModelFactory? _dashboardFactory;
    
    // Core ViewModels
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly ChatViewModel _chatViewModel;
    
    // Management ViewModels
    private readonly RunnerManagerViewModel _runnerManagerViewModel;
    private readonly JobsViewModel _jobsViewModel;
    private readonly DatasetsViewModel _datasetsViewModel;
    
    // Models ViewModels
    private readonly ModelsViewModel _modelsViewModel;
    private readonly BaseModelViewModel _baseModelViewModel;
    private readonly LorAsViewModel _lorAsViewModel;
    private readonly ControlNetsViewModel _controlNetsViewModel;
    private readonly VAEsViewModel _vaesViewModel;
    private readonly EmbeddingsViewModel _embeddingsViewModel;
    private readonly HypernetworksViewModel _hypernetworksViewModel;
    private readonly AdvancedViewModel _advancedViewModel;
    
    // Images ViewModels
    private readonly Text2ImageViewModel _text2ImageViewModel;
    private readonly Image2ImageViewModel _image2ImageViewModel;
    private readonly InpaintingViewModel _inpaintingViewModel;
    private readonly UpscalingViewModel _upscalingViewModel;
    
    // Video ViewModels
    private readonly Text2VideoViewModel _text2VideoViewModel;
    private readonly Video2VideoViewModel _video2VideoViewModel;
    private readonly MotionControlViewModel _motionControlViewModel;
    private readonly TemporalEffectsViewModel _temporalEffectsViewModel;
    
    // Voice ViewModels
    private readonly VoiceViewModel _voiceViewModel;
    private readonly TTSConfigurationViewModel _ttsConfigurationViewModel;
    private readonly VoiceCloningViewModel _voiceCloningViewModel;
    private readonly RealTimeSynthesisViewModel _realTimeSynthesisViewModel;
    private readonly AudioProcessingViewModel _audioProcessingViewModel;
    
    // 3D Models ViewModels
    private readonly ThreeDModelsViewModel _threeDModelsViewModel;
    
    // Entities ViewModels
    private readonly EntitiesViewModel _entitiesViewModel;
    private readonly EntityCreationViewModel _entityCreationViewModel;
    private readonly BehavioralPatternsViewModel _behavioralPatternsViewModel;
    private readonly InteractionTestingViewModel _interactionTestingViewModel;
    private readonly EntityManagementViewModel _entityManagementViewModel;
    
    // New private fields for ContentControl pattern
    private readonly DashboardViewModel _dashboard;
    private readonly ChatViewModel _chat;
    private readonly RunnerManagerViewModel _runner;
    private readonly JobsViewModel _jobs;
    private readonly DatasetsViewModel _datasets;
    private readonly ModelsViewModel _models;
    private readonly BaseModelViewModel _baseModel;
    private readonly LorAsViewModel _lorAs;
    private readonly ControlNetsViewModel _controlNets;
    private readonly VAEsViewModel _vaes;
    private readonly EmbeddingsViewModel _embeddings;
    private readonly HypernetworksViewModel _hypers;
    private readonly AdvancedViewModel _advanced;
    private readonly Lazarus.Desktop.ViewModels.Images.Text2ImageViewModel _t2i;
    private readonly Lazarus.Desktop.ViewModels.Images.Image2ImageViewModel _i2i;
    private readonly Lazarus.Desktop.ViewModels.Images.InpaintingViewModel _inpaint;
    private readonly Lazarus.Desktop.ViewModels.Images.UpscalingViewModel _upsc;
    private readonly Lazarus.Desktop.ViewModels.Video.Text2VideoViewModel _t2v;
    private readonly Lazarus.Desktop.ViewModels.Video.Video2VideoViewModel _v2v;
    private readonly Lazarus.Desktop.ViewModels.Video.MotionControlViewModel _motion;
    private readonly Lazarus.Desktop.ViewModels.Video.TemporalEffectsViewModel _temporal;
    private readonly Lazarus.Desktop.ViewModels.Voice.VoiceViewModel _voice;
    private readonly Lazarus.Desktop.ViewModels.Voice.TTSConfigurationViewModel _tts;
    private readonly Lazarus.Desktop.ViewModels.Voice.VoiceCloningViewModel _cloning;
    private readonly Lazarus.Desktop.ViewModels.Voice.RealTimeSynthesisViewModel _rts;
    private readonly Lazarus.Desktop.ViewModels.Voice.AudioProcessingViewModel _audio;
    private readonly Lazarus.Desktop.ViewModels.ThreeDModels.ThreeDModelsViewModel _threeD;
    private readonly Lazarus.Desktop.ViewModels.Entities.EntitiesViewModel _entities;
    private readonly Lazarus.Desktop.ViewModels.Entities.EntityCreationViewModel _entCreate;
    private readonly Lazarus.Desktop.ViewModels.Entities.BehavioralPatternsViewModel _entBehave;
    private readonly Lazarus.Desktop.ViewModels.Entities.InteractionTestingViewModel _entTest;
    private readonly Lazarus.Desktop.ViewModels.Entities.EntityManagementViewModel _entManage;
    
    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set { _currentViewModel = value; OnPropertyChanged(); }
    }
    private object _currentViewModel;
    private bool _isInitialized = false;

    public MainWindowViewModel(
        UserPreferencesService preferencesService,
        SystemStateViewModel systemState,
        INavigationService navigation,
        DashboardViewModel dashboard,
        ChatViewModel chat,
        RunnerManagerViewModel runner,
        JobsViewModel jobs,
        DatasetsViewModel datasets,
        ModelsViewModel models,
        BaseModelViewModel baseModel,
        LorAsViewModel lorAs,
        ControlNetsViewModel controlNets,
        VAEsViewModel vaes,
        EmbeddingsViewModel embeddings,
        HypernetworksViewModel hypers,
        AdvancedViewModel advanced,
        Lazarus.Desktop.ViewModels.Images.Text2ImageViewModel t2i,
        Lazarus.Desktop.ViewModels.Images.Image2ImageViewModel i2i,
        Lazarus.Desktop.ViewModels.Images.InpaintingViewModel inpaint,
        Lazarus.Desktop.ViewModels.Images.UpscalingViewModel upsc,
        Lazarus.Desktop.ViewModels.Video.Text2VideoViewModel t2v,
        Lazarus.Desktop.ViewModels.Video.Video2VideoViewModel v2v,
        Lazarus.Desktop.ViewModels.Video.MotionControlViewModel motion,
        Lazarus.Desktop.ViewModels.Video.TemporalEffectsViewModel temporal,
        Lazarus.Desktop.ViewModels.Voice.VoiceViewModel voice,
        Lazarus.Desktop.ViewModels.Voice.TTSConfigurationViewModel tts,
        Lazarus.Desktop.ViewModels.Voice.VoiceCloningViewModel cloning,
        Lazarus.Desktop.ViewModels.Voice.RealTimeSynthesisViewModel rts,
        Lazarus.Desktop.ViewModels.Voice.AudioProcessingViewModel audio,
        Lazarus.Desktop.ViewModels.ThreeDModels.ThreeDModelsViewModel threeD,
        Lazarus.Desktop.ViewModels.Entities.EntitiesViewModel entities,
        Lazarus.Desktop.ViewModels.Entities.EntityCreationViewModel entCreate,
        Lazarus.Desktop.ViewModels.Entities.BehavioralPatternsViewModel entBehave,
        Lazarus.Desktop.ViewModels.Entities.InteractionTestingViewModel entTest,
        Lazarus.Desktop.ViewModels.Entities.EntityManagementViewModel entManage,
        Lazarus.Desktop.Services.Dashboard.DashboardViewModelFactory? dashboardFactory = null)
    {
        _preferencesService = preferencesService;
        _systemState = systemState;
        _navigationService = navigation;
        _dashboardFactory = dashboardFactory;

        _dashboard = dashboard; _chat = chat;
        _runner = runner; _jobs = jobs; _datasets = datasets;
        _models = models; _baseModel = baseModel; _lorAs = lorAs; _controlNets = controlNets; _vaes = vaes;
        _embeddings = embeddings; _hypers = hypers; _advanced = advanced;
        _t2i = t2i; _i2i = i2i; _inpaint = inpaint; _upsc = upsc;
        _t2v = t2v; _v2v = v2v; _motion = motion; _temporal = temporal;
        _voice = voice; _tts = tts; _cloning = cloning; _rts = rts; _audio = audio;
        _threeD = threeD;
        _entities = entities; _entCreate = entCreate; _entBehave = entBehave; _entTest = entTest; _entManage = entManage;

        Navigation.PropertyChanged += (_, e) =>
        {
            if (_isInitialized && (e.PropertyName == nameof(Navigation.CurrentTab) || e.PropertyName == nameof(Navigation.CurrentSubtab)))
                UpdateCurrentView();
        };

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
        _navigationService.PropertyChanged += OnNavigationPropertyChanged;
        // Ensure mode label updates regardless of where the change originated
        UserPreferencesService.ViewModeChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(CurrentViewMode));
            OnPropertyChanged(nameof(CurrentViewModeDisplay));
        };
         
        // Initialize navigation commands
        NavigateToDashboardCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.Dashboard));
        NavigateToConversationsCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.Conversations));
        NavigateToModelsCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.Models));
        NavigateToRunnerManagerCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.RunnerManager));
        NavigateToJobsCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.Jobs));
        NavigateToDatasetsCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.Datasets));
        NavigateToImagesCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Images, "Text2Image"));
        NavigateToVideoCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Video, "Text2Video"));
        NavigateToThreeDModelsCommand = new RelayCommand(_ => Navigation.NavigateTo(NavigationTab.ThreeDModels));
        NavigateToVoiceCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Voice, "TTSConfiguration"));
        NavigateToEntitiesCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Entities, "EntityCreation"));
        
        // Initialize subtab navigation commands
        NavigateToBaseModelCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "BaseModel"));
        NavigateToLorAsCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "LoRAs"));
        NavigateToControlNetsCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "ControlNets"));
        NavigateToVAEsCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "VAEs"));
        NavigateToEmbeddingsCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "Embeddings"));
        NavigateToHypernetworksCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "Hypernetworks"));
        NavigateToAdvancedCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Models, "Advanced"));
        
        // Initialize Images subtab navigation commands
        NavigateToText2ImageCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Images, "Text2Image"));
        NavigateToImage2ImageCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Images, "Image2Image"));
        NavigateToInpaintingCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Images, "Inpainting"));
        NavigateToUpscalingCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Images, "Upscaling"));
        
        // Initialize Video subtab navigation commands
        NavigateToText2VideoCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Video, "Text2Video"));
        NavigateToVideo2VideoCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Video, "Video2Video"));
        NavigateToMotionControlCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Video, "MotionControl"));
        NavigateToTemporalEffectsCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Video, "TemporalEffects"));

		// Initialize Voice subtab navigation commands
		NavigateToVoiceCloningCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Voice, "VoiceCloning"));
		NavigateToRealTimeSynthesisCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Voice, "RealTimeSynthesis"));
		NavigateToAudioProcessingCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Voice, "AudioProcessing"));
		
		// Initialize Entities subtab navigation commands
		NavigateToBehavioralPatternsCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Entities, "BehavioralPatterns"));
		NavigateToInteractionTestingCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Entities, "InteractionTesting"));
		NavigateToEntityManagementCommand = new RelayCommand(_ => Navigation.NavigateToSubtab(NavigationTab.Entities, "EntityManagement"));

        UpdateCurrentView(); // Set initial view
        _isInitialized = true; // Enable navigation updates
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
    /// User-friendly current view mode display
    /// </summary>
    public string CurrentViewModeDisplay => _preferencesService.CurrentViewModeDisplay;

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

    /// <summary>
    /// System state ViewModel for the Context Bar
    /// </summary>
    public SystemStateViewModel SystemState => _systemState;

    /// <summary>
    /// Navigation service for MVVM tab switching
    /// </summary>
    public INavigationService Navigation => _navigationService;
    
    
    /// <summary>
    /// Dashboard ViewModel for the main dashboard view
    /// </summary>
    public DashboardViewModel DashboardViewModel 
    { 
        get 
        {
            Console.WriteLine($"[MainWindowViewModel] DashboardViewModel property accessed - returning: {_dashboardViewModel != null}");
            return _dashboardViewModel;
        }
    }
    
    /// <summary>
    /// Chat ViewModel for the conversations view
    /// </summary>
    public ChatViewModel ChatViewModel => _chatViewModel;
    
    /// <summary>
    /// Models ViewModel for the model configuration view
    /// </summary>
    public ModelsViewModel ModelsViewModel => _modelsViewModel;

    #endregion

    #region Navigation Commands

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToConversationsCommand { get; }
    public ICommand NavigateToModelsCommand { get; }
    public ICommand NavigateToRunnerManagerCommand { get; }
    public ICommand NavigateToJobsCommand { get; }
    public ICommand NavigateToDatasetsCommand { get; }
    public ICommand NavigateToImagesCommand { get; }
    public ICommand NavigateToVideoCommand { get; }
    public ICommand NavigateToThreeDModelsCommand { get; }
    public ICommand NavigateToVoiceCommand { get; }
    public ICommand NavigateToEntitiesCommand { get; }
    
    // Model Configuration Subtab Commands
    public ICommand NavigateToBaseModelCommand { get; }
    public ICommand NavigateToLorAsCommand { get; }
    public ICommand NavigateToControlNetsCommand { get; }
    public ICommand NavigateToVAEsCommand { get; }
    public ICommand NavigateToEmbeddingsCommand { get; }
    public ICommand NavigateToHypernetworksCommand { get; }
    public ICommand NavigateToAdvancedCommand { get; }
    
    // Images Subtab Commands
    public ICommand NavigateToText2ImageCommand { get; }
    public ICommand NavigateToImage2ImageCommand { get; }
    public ICommand NavigateToInpaintingCommand { get; }
    public ICommand NavigateToUpscalingCommand { get; }
    
    // Video Subtab Commands
    public ICommand NavigateToText2VideoCommand { get; }
    public ICommand NavigateToVideo2VideoCommand { get; }
    public ICommand NavigateToMotionControlCommand { get; }
    public ICommand NavigateToTemporalEffectsCommand { get; }

	public ICommand NavigateToVoiceCloningCommand { get; }
	public ICommand NavigateToRealTimeSynthesisCommand { get; }
	public ICommand NavigateToAudioProcessingCommand { get; }
	public ICommand NavigateToBehavioralPatternsCommand { get; }
	public ICommand NavigateToInteractionTestingCommand { get; }
	public ICommand NavigateToEntityManagementCommand { get; }

    #endregion

    #region Event Handlers

    private void OnPreferencesServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward property changes from service to UI
        if (e.PropertyName == nameof(UserPreferencesService.CurrentViewMode))
        {
            OnPropertyChanged(nameof(CurrentViewMode));
            OnPropertyChanged(nameof(CurrentViewModeDisplay));
            
            // Refresh Dashboard ViewModel when ViewMode changes
            if (Navigation.CurrentTab == NavigationTab.Dashboard)
            {
                Console.WriteLine($"[MainWindowViewModel] ViewMode changed to {_preferencesService.CurrentViewMode} - refreshing Dashboard");
                UpdateCurrentView(); // This will call MapDashboard() with the new ViewMode
            }
        }
        else if (e.PropertyName == nameof(UserPreferencesService.CurrentTheme))
        {
            OnPropertyChanged(nameof(CurrentTheme));
        }
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Handle navigation service property changes
        if (e.PropertyName == nameof(INavigationService.CurrentTab) || 
            e.PropertyName == nameof(INavigationService.CurrentSubtab))
        {
            UpdateCurrentView();
        }
    }

    #endregion

    #region CurrentViewModel Mapping

    private void UpdateCurrentView()
    {
        CurrentViewModel = Navigation.CurrentTab switch
        {
            NavigationTab.Dashboard       => MapDashboard(),
            NavigationTab.Conversations   => _chat,
            NavigationTab.Models          => MapModels(),
            NavigationTab.RunnerManager   => _runner,
            NavigationTab.Jobs            => _jobs,
            NavigationTab.Datasets        => _datasets,
            NavigationTab.Images          => MapImages(),
            NavigationTab.Video           => MapVideo(),
            NavigationTab.ThreeDModels    => _threeD,
            NavigationTab.Voice           => MapVoice(),
            NavigationTab.Entities        => MapEntities(),
            _                            => _dashboard
        };
    }

    private object MapDashboard()
    {
        var currentViewMode = _preferencesService.CurrentViewMode;
        Console.WriteLine($"[MainWindowViewModel] MapDashboard called for ViewMode: {currentViewMode}");
        // Always return the central DashboardViewModel. The inner view switches via ViewModeTemplateSelector.
        return _dashboard;
    }
    
    private object MapModels()
    {
        // Ensure the always-visible header is in sync when entering Models area
        _systemState?.SyncFromGlobal();
        return Navigation.CurrentSubtab switch
        {
            "BaseModel"      => EnsureBaseModelInitialized(),
            "LoRAs"          => _lorAs,
            "ControlNets"    => _controlNets,
            "VAEs"           => _vaes,
            "Embeddings"     => _embeddings,
            "Hypernetworks"  => _hypers,
            "Advanced"       => _advanced,
            _                => _models
        };
    }

    private BaseModelViewModel EnsureBaseModelInitialized()
    {
        try { _ = _baseModel.InitializeAsync(); } catch { }
        return _baseModel;
    }
    
    private object MapImages()
    {
        return Navigation.CurrentSubtab switch
        {
            "Text2Image"     => _t2i,
            "Image2Image"    => _i2i,
            "Inpainting"     => _inpaint,
            "Upscaling"      => _upsc,
            _                => _t2i  // Default to Text2Image
        };
    }
    
    private object MapVideo()
    {
        return Navigation.CurrentSubtab switch
        {
            "Text2Video"     => _t2v,
            "Video2Video"    => _v2v,
            "MotionControl"  => _motion,
            "TemporalEffects" => _temporal,
            _                => _t2v  // Default to Text2Video
        };
    }
    
    private object MapVoice()
    {
        return Navigation.CurrentSubtab switch
        {
            "TTSConfiguration"     => _tts,
            "VoiceCloning"         => _cloning,
            "RealTimeSynthesis"    => _rts,
            "AudioProcessing"      => _audio,
            _                      => _tts  // Default to TTSConfiguration
        };
    }
    
    private object MapEntities()
    {
        return Navigation.CurrentSubtab switch
        {
            "EntityCreation"       => _entCreate,
            "BehavioralPatterns"    => _entBehave,
            "InteractionTesting"    => _entTest,
            "EntityManagement"      => _entManage,
            _                      => _entCreate  // Default to EntityCreation
        };
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
            // Unsubscribe from ALL events to prevent memory leaks
            _preferencesService.PropertyChanged -= OnPreferencesServicePropertyChanged;
            
            // Unsubscribe from navigation service events
            if (_navigationService != null)
            {
                _navigationService.PropertyChanged -= OnNavigationPropertyChanged;
            }
            
            _disposed = true;
        }
    }

    #endregion
}