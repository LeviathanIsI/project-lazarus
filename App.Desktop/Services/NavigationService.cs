using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.Services;

/// <summary>
/// MVVM Navigation Service - Eliminates code-behind navigation
/// Centralizes tab state management and provides proper data binding
/// </summary>
public class NavigationService : INavigationService, IDisposable
{
    private NavigationTab _currentTab = NavigationTab.Dashboard;
    private string _currentSubtab = string.Empty;
    
    public NavigationService()
    {
        Console.WriteLine($"[NavigationService] CONSTRUCTOR: Initialized with CurrentTab = {_currentTab}");
        Console.WriteLine($"[NavigationService] CONSTRUCTOR: IsDashboardVisible = {IsDashboardVisible}");
        Console.WriteLine($"[NavigationService] CONSTRUCTOR: IsDashboardActive = {IsDashboardActive}");
        Console.WriteLine($"[NavigationService] CONSTRUCTOR: IsModelsVisible = {IsModelsVisible}");
        Console.WriteLine($"[NavigationService] CONSTRUCTOR: IsModelsActive = {IsModelsActive}");
    }

    public void Dispose()
    {
        // Clean up any resources if needed
        Console.WriteLine("[NavigationService] Disposing NavigationService");
    }
    
    /// <summary>
    /// Current active tab
    /// </summary>
    public NavigationTab CurrentTab
    {
        get => _currentTab;
        private set
        {
            if (_currentTab != value)
            {
                var previousTab = _currentTab;
                Console.WriteLine($"[NavigationService] FORENSICS: CurrentTab setter - changing from {previousTab} to {value}");
                _currentTab = value;
                OnPropertyChanged();
                
                Console.WriteLine($"[NavigationService] FORENSICS: CurrentTab setter - triggering visibility property notifications");
                // Notify all tab visibility properties
                OnPropertyChanged(nameof(IsDashboardVisible));
                OnPropertyChanged(nameof(IsConversationsVisible));
                OnPropertyChanged(nameof(IsModelsVisible));
                Console.WriteLine($"[NavigationService] FORENSICS: IsModelsVisible after property change: {IsModelsVisible}");
                OnPropertyChanged(nameof(IsRunnerManagerVisible));
                OnPropertyChanged(nameof(IsJobsVisible));
                OnPropertyChanged(nameof(IsDatasetsVisible));
                OnPropertyChanged(nameof(IsImagesVisible));
                OnPropertyChanged(nameof(IsVideoVisible));
                OnPropertyChanged(nameof(IsThreeDModelsVisible));
                OnPropertyChanged(nameof(IsVoiceVisible));
                OnPropertyChanged(nameof(IsEntitiesVisible));
                
                // Notify tab button states
                OnPropertyChanged(nameof(IsDashboardActive));
                OnPropertyChanged(nameof(IsConversationsActive));
                OnPropertyChanged(nameof(IsModelsActive));
                OnPropertyChanged(nameof(IsRunnerManagerActive));
                OnPropertyChanged(nameof(IsJobsActive));
                OnPropertyChanged(nameof(IsDatasetsActive));
                OnPropertyChanged(nameof(IsImagesActive));
                OnPropertyChanged(nameof(IsVideoActive));
                OnPropertyChanged(nameof(IsThreeDModelsActive));
                OnPropertyChanged(nameof(IsVoiceActive));
                OnPropertyChanged(nameof(IsEntitiesActive));
                
                Console.WriteLine($"[Navigation] Tab changed: {previousTab} → {value}");
            }
        }
    }
    
    /// <summary>
    /// Current active subtab (empty string if none)
    /// </summary>
    public string CurrentSubtab
    {
        get => _currentSubtab;
        private set
        {
            if (_currentSubtab != value)
            {
                _currentSubtab = value;
                OnPropertyChanged();
                NotifySubtabPropertiesChanged();
            }
        }
    }
    
    #region Tab Visibility Properties for XAML Binding
    
    public bool IsDashboardVisible 
    { 
        get 
        { 
            var result = CurrentTab == NavigationTab.Dashboard;
            Console.WriteLine($"[NavigationService] VISIBILITY DEBUG: IsDashboardVisible = {result} (CurrentTab = {CurrentTab})");
            return result;
        }
    }
    
    public bool IsConversationsVisible => CurrentTab == NavigationTab.Conversations;
    
    public bool IsModelsVisible 
    { 
        get 
        { 
            var result = CurrentTab == NavigationTab.Models;
            Console.WriteLine($"[NavigationService] VISIBILITY DEBUG: IsModelsVisible = {result} (CurrentTab = {CurrentTab})");
            return result;
        }
    }
    public bool IsRunnerManagerVisible => CurrentTab == NavigationTab.RunnerManager;
    public bool IsJobsVisible => CurrentTab == NavigationTab.Jobs;
    public bool IsDatasetsVisible => CurrentTab == NavigationTab.Datasets;
    public bool IsImagesVisible => CurrentTab == NavigationTab.Images && string.IsNullOrEmpty(_currentSubtab);
    public bool IsVideoVisible => CurrentTab == NavigationTab.Video && string.IsNullOrEmpty(_currentSubtab);
    public bool IsThreeDModelsVisible => CurrentTab == NavigationTab.ThreeDModels;
    public bool IsVoiceVisible => CurrentTab == NavigationTab.Voice && string.IsNullOrEmpty(_currentSubtab);
    public bool IsEntitiesVisible => CurrentTab == NavigationTab.Entities && string.IsNullOrEmpty(_currentSubtab);
    
    // Model Configuration Subtabs
    public bool IsBaseModelVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "BaseModel";
    public bool IsLorAsVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "LoRAs";
    public bool IsControlNetsVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "ControlNets";
    public bool IsVAEsVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "VAEs";
    public bool IsEmbeddingsVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "Embeddings";
    public bool IsHypernetworksVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "Hypernetworks";
    public bool IsAdvancedVisible => CurrentTab == NavigationTab.Models && _currentSubtab == "Advanced";
    
    // Images Subtabs  
    public bool IsText2ImageVisible => CurrentTab == NavigationTab.Images && _currentSubtab == "Text2Image";
    public bool IsImage2ImageVisible => CurrentTab == NavigationTab.Images && _currentSubtab == "Image2Image";
    public bool IsInpaintingVisible => CurrentTab == NavigationTab.Images && _currentSubtab == "Inpainting";
    public bool IsUpscalingVisible => CurrentTab == NavigationTab.Images && _currentSubtab == "Upscaling";
    
    // Video Subtabs
    public bool IsText2VideoVisible => CurrentTab == NavigationTab.Video && _currentSubtab == "Text2Video";
    public bool IsVideo2VideoVisible => CurrentTab == NavigationTab.Video && _currentSubtab == "Video2Video";
    public bool IsMotionControlVisible => CurrentTab == NavigationTab.Video && _currentSubtab == "MotionControl";
    public bool IsTemporalEffectsVisible => CurrentTab == NavigationTab.Video && _currentSubtab == "TemporalEffects";
    
    // Voice Subtabs
    public bool IsTTSConfigurationVisible => CurrentTab == NavigationTab.Voice && _currentSubtab == "TTSConfiguration";
    public bool IsVoiceCloningVisible => CurrentTab == NavigationTab.Voice && _currentSubtab == "VoiceCloning";
    public bool IsRealTimeSynthesisVisible => CurrentTab == NavigationTab.Voice && _currentSubtab == "RealTimeSynthesis";
    public bool IsAudioProcessingVisible => CurrentTab == NavigationTab.Voice && _currentSubtab == "AudioProcessing";
    
    // Entities Subtabs
    public bool IsEntityCreationVisible => CurrentTab == NavigationTab.Entities && _currentSubtab == "EntityCreation";
    public bool IsBehavioralPatternsVisible => CurrentTab == NavigationTab.Entities && _currentSubtab == "BehavioralPatterns";
    public bool IsInteractionTestingVisible => CurrentTab == NavigationTab.Entities && _currentSubtab == "InteractionTesting";
    public bool IsEntityManagementVisible => CurrentTab == NavigationTab.Entities && _currentSubtab == "EntityManagement";
    
    #endregion
    
    #region Tab Button State Properties for XAML Binding
    
    public bool IsDashboardActive => CurrentTab == NavigationTab.Dashboard;
    public bool IsConversationsActive => CurrentTab == NavigationTab.Conversations;
    public bool IsModelsActive => CurrentTab == NavigationTab.Models;
    public bool IsRunnerManagerActive => CurrentTab == NavigationTab.RunnerManager;
    public bool IsJobsActive => CurrentTab == NavigationTab.Jobs;
    public bool IsDatasetsActive => CurrentTab == NavigationTab.Datasets;
    public bool IsImagesActive => CurrentTab == NavigationTab.Images;
    public bool IsVideoActive => CurrentTab == NavigationTab.Video;
    public bool IsThreeDModelsActive => CurrentTab == NavigationTab.ThreeDModels;
    public bool IsVoiceActive => CurrentTab == NavigationTab.Voice;
    public bool IsEntitiesActive => CurrentTab == NavigationTab.Entities;
    
    // Subtab Button State Properties
    public bool IsBaseModelActive => CurrentTab == NavigationTab.Models && _currentSubtab == "BaseModel";
    public bool IsLorAsActive => CurrentTab == NavigationTab.Models && _currentSubtab == "LoRAs";
    public bool IsControlNetsActive => CurrentTab == NavigationTab.Models && _currentSubtab == "ControlNets";
    public bool IsVAEsActive => CurrentTab == NavigationTab.Models && _currentSubtab == "VAEs";
    public bool IsEmbeddingsActive => CurrentTab == NavigationTab.Models && _currentSubtab == "Embeddings";
    public bool IsHypernetworksActive => CurrentTab == NavigationTab.Models && _currentSubtab == "Hypernetworks";
    public bool IsAdvancedActive => CurrentTab == NavigationTab.Models && _currentSubtab == "Advanced";
    
    #endregion
    
    /// <summary>
    /// Navigate to a specific tab
    /// </summary>
    public void NavigateTo(NavigationTab tab)
    {
        Console.WriteLine($"[NavigationService] FORENSICS: NavigateTo({tab}) called");
        Console.WriteLine($"[NavigationService] FORENSICS: Previous CurrentTab: {CurrentTab}");
        Console.WriteLine($"[NavigationService] FORENSICS: Previous _currentSubtab: '{_currentSubtab}'");
        
        CurrentTab = tab;
        CurrentSubtab = string.Empty; // Clear subtab when changing main tab
        
        Console.WriteLine($"[NavigationService] FORENSICS: New CurrentTab: {CurrentTab}");
        Console.WriteLine($"[NavigationService] FORENSICS: New _currentSubtab: '{_currentSubtab}'");
        Console.WriteLine($"[NavigationService] FORENSICS: IsModelsVisible: {IsModelsVisible}");
        Console.WriteLine("[NavigationService] FORENSICS: NavigateTo completed");
    }
    
    /// <summary>
    /// Navigate to a specific subtab within the current main tab
    /// </summary>
    public void NavigateToSubtab(NavigationTab tab, string subtab)
    {
        Console.WriteLine($"[NavigationService] FORENSICS: NavigateToSubtab({tab}, '{subtab}') called");
        if (CurrentTab != tab)
        {
            Console.WriteLine($"[NavigationService] FORENSICS: Changing tab from {CurrentTab} to {tab}");
            CurrentTab = tab;
        }
        
        if (_currentSubtab != subtab)
        {
            Console.WriteLine($"[NavigationService] FORENSICS: Changing subtab from '{_currentSubtab}' to '{subtab}'");
            CurrentSubtab = subtab;
        }
        Console.WriteLine("[NavigationService] FORENSICS: NavigateToSubtab completed");
    }
    
    private void NotifySubtabPropertiesChanged()
    {
        // ===== Model Configuration =====
        OnPropertyChanged(nameof(IsBaseModelVisible));
        OnPropertyChanged(nameof(IsLorAsVisible));
        OnPropertyChanged(nameof(IsControlNetsVisible));
        OnPropertyChanged(nameof(IsVAEsVisible));
        OnPropertyChanged(nameof(IsEmbeddingsVisible));
        OnPropertyChanged(nameof(IsHypernetworksVisible));
        OnPropertyChanged(nameof(IsAdvancedVisible));

        OnPropertyChanged(nameof(IsBaseModelActive));
        OnPropertyChanged(nameof(IsLorAsActive));
        OnPropertyChanged(nameof(IsControlNetsActive));
        OnPropertyChanged(nameof(IsVAEsActive));
        OnPropertyChanged(nameof(IsEmbeddingsActive));
        OnPropertyChanged(nameof(IsHypernetworksActive));
        OnPropertyChanged(nameof(IsAdvancedActive));

        // ===== Images =====
        OnPropertyChanged(nameof(IsText2ImageVisible));
        OnPropertyChanged(nameof(IsImage2ImageVisible));
        OnPropertyChanged(nameof(IsInpaintingVisible));
        OnPropertyChanged(nameof(IsUpscalingVisible));
        // (Active props optional — add only if you defined them)

        // ===== Video =====
        OnPropertyChanged(nameof(IsText2VideoVisible));
        OnPropertyChanged(nameof(IsVideo2VideoVisible));
        OnPropertyChanged(nameof(IsMotionControlVisible));
        OnPropertyChanged(nameof(IsTemporalEffectsVisible));

        // ===== Voice =====
        OnPropertyChanged(nameof(IsTTSConfigurationVisible));
        OnPropertyChanged(nameof(IsVoiceCloningVisible));
        OnPropertyChanged(nameof(IsRealTimeSynthesisVisible));
        OnPropertyChanged(nameof(IsAudioProcessingVisible));

        // ===== Entities =====
        OnPropertyChanged(nameof(IsEntityCreationVisible));
        OnPropertyChanged(nameof(IsBehavioralPatternsVisible));
        OnPropertyChanged(nameof(IsInteractionTestingVisible));
        OnPropertyChanged(nameof(IsEntityManagementVisible));

        Console.WriteLine($"[Navigation] Subtab changed: {CurrentTab} → {_currentSubtab}");
    }
    
    /// <summary>
    /// Check if a tab is currently active
    /// </summary>
    public bool IsTabActive(NavigationTab tab)
    {
        return CurrentTab == tab;
    }
    
    #region INotifyPropertyChanged
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    #endregion
}