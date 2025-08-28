using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using App.Shared.Enums;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Global state management for user preferences including view complexity and theming
/// This is the neural core of the personalization system
/// </summary>
public class UserPreferencesService : INotifyPropertyChanged, IDisposable
{
    // Set to true to enable deep diagnostics and aggressive UI refresh (slower)
    private const bool DiagnosticsEnabled = false;
    private ViewMode _currentViewMode = ViewMode.Novice;
    private ThemeMode _currentTheme = ThemeMode.Dark;
    
    // State management system for ViewMode transitions  
    private readonly ViewModeStateManager _stateManager = new();

    #region Constructor

    /// <summary>
    /// Initialize UserPreferencesService with default preferences
    /// </summary>
    public UserPreferencesService()
    {
        Console.WriteLine($"[UserPreferences] 🏗️ Initializing UserPreferencesService...");
        Console.WriteLine($"[UserPreferences] Default theme: {_currentTheme}");
        Console.WriteLine($"[UserPreferences] Default ViewMode: {_currentViewMode}");
        
        // CRITICAL: Validate resource dictionaries at startup
        Console.WriteLine($"[UserPreferences] 🔍 Running resource validation...");
        ValidateAllThemeResources();
        
        // Load ViewMode template dictionaries for Content Presenters
        Console.WriteLine($"[UserPreferences] 🔧 Loading ViewMode templates for: {_currentViewMode}");
        ApplyViewModeInternal(_currentViewMode);
        ViewModeChanged?.Invoke(this, _currentViewMode);
        Console.WriteLine($"[UserPreferences] ✅ UserPreferencesService initialized successfully");
    }

    public void Dispose()
    {
        // Clean up any resources if needed
        Console.WriteLine("[UserPreferences] Disposing UserPreferencesService");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Current UI complexity level - affects which controls/features are visible
    /// </summary>
    public ViewMode CurrentViewMode 
    { 
        get => _currentViewMode;
        set
        {
            if (_currentViewMode != value)
            {
                var oldViewMode = _currentViewMode;
                Console.WriteLine($"[UserPreferences] 🎯 ViewMode transition: {oldViewMode} → {value}");
                
                // Execute intelligent state-preserving ViewMode transition
                ExecuteStatePreservingTransition(oldViewMode, value);
            }
        }
    }

    /// <summary>
    /// Current visual theme - affects colors, styling, and aesthetic
    /// </summary>
    public ThemeMode CurrentTheme 
    { 
        get => _currentTheme;
        set
        {
            if (SetProperty(ref _currentTheme, value))
            {
                Console.WriteLine($"[UserPreferences] 🎨 Theme changed to: {value}");
                
                // Apply the theme immediately when property is set
                ApplyThemeInternal(value);
                
                ThemeChanged?.Invoke(this, value);
            }
        }
    }

    #endregion

    #region Static Events for Cross-Tab Communication

    /// <summary>
    /// Global event fired when view mode changes - all UI components can subscribe
    /// </summary>
    public static event EventHandler<ViewMode>? ViewModeChanged;

    /// <summary>
    /// Global event fired when theme changes - triggers resource dictionary swapping
    /// </summary>
    public static event EventHandler<ThemeMode>? ThemeChanged;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if current view mode meets minimum complexity requirement
    /// </summary>
    /// <param name="minimumLevel">Minimum complexity needed to show feature</param>
    /// <returns>True if current mode >= minimum level</returns>
    public bool IsViewModeAtLeast(ViewMode minimumLevel)
    {
        return CurrentViewMode >= minimumLevel;
    }

    /// <summary>
    /// Get user-friendly display name for current view mode
    /// </summary>
    public string CurrentViewModeDisplay => CurrentViewMode switch
    {
        ViewMode.Novice => "Novice (Simple)",
        ViewMode.Enthusiast => "Enthusiast (Balanced)", 
        ViewMode.Developer => "Developer (Full)",
        _ => CurrentViewMode.ToString()
    };

    /// <summary>
    /// Get user-friendly display name for current theme
    /// </summary>
    public string CurrentThemeDisplay => CurrentTheme switch
    {
        ThemeMode.Dark => "Dark Gothic",
        ThemeMode.Light => "Clean Light",
        ThemeMode.Cyberpunk => "Cyberpunk Neon",
        ThemeMode.Minimal => "Minimal Clean",
        _ => CurrentTheme.ToString()
    };

    /// <summary>
    /// Apply the specified theme by swapping resource dictionaries
    /// This is the nuclear-powered theme metamorphosis engine
    /// </summary>
    /// <param name="newTheme">Theme to apply</param>
    public void ApplyTheme(ThemeMode newTheme)
    {
        Console.WriteLine($"[THEME DEBUG] ApplyTheme called with: {newTheme}");
        System.Diagnostics.Debug.WriteLine($"[THEME DEBUG] Current resources count: {Application.Current.Resources.MergedDictionaries.Count}");
        
        // Update the current theme property, which will trigger ApplyThemeInternal
        CurrentTheme = newTheme;
        
        Console.WriteLine($"[THEME DEBUG] Theme application completed");
    }
    
    /// <summary>
    /// Apply the specified ViewMode by swapping template dictionaries
    /// This is the cognitive complexity transformation engine
    /// </summary>
    /// <param name="newViewMode">ViewMode to apply</param>
    public void ApplyViewMode(ViewMode newViewMode)
    {
        Console.WriteLine($"[VIEWMODE DEBUG] ApplyViewMode called with: {newViewMode}");
        System.Diagnostics.Debug.WriteLine($"[VIEWMODE DEBUG] Current view mode: {CurrentViewMode}");
        
        // Update the current ViewMode property, which will trigger state-preserving transition
        CurrentViewMode = newViewMode;
        
        Console.WriteLine($"[VIEWMODE DEBUG] ViewMode application completed");
    }
    
    /// <summary>
    /// Get the ViewModeStateManager for external parameter preservation
    /// </summary>
    public ViewModeStateManager StateManager => _stateManager;
    
    /// <summary>
    /// Execute intelligent state-preserving ViewMode transition with user protection
    /// </summary>
    private void ExecuteStatePreservingTransition(ViewMode fromMode, ViewMode toMode)
    {
        try
        {
            Console.WriteLine($"[StateTransition] 🔄 Beginning intelligent transition: {fromMode} → {toMode}");
            
            // Step 1: Check for unsaved work that could be lost
            var unsavedWarnings = _stateManager.GetUnsavedWorkWarnings();
            if (unsavedWarnings.Count > 0)
            {
                Console.WriteLine($"[StateTransition] ⚠️ Found {unsavedWarnings.Count} unsaved work items");
                
                // Show user warning dialog about unsaved work
                var shouldProceed = ShowUnsavedWorkDialog(unsavedWarnings, toMode);
                if (!shouldProceed)
                {
                    Console.WriteLine($"[StateTransition] ❌ User cancelled transition due to unsaved work");
                    return; // User chose to cancel transition
                }
            }
            
            // Step 2: Get current active parameters for visibility analysis
            var activeParameters = GetCurrentActiveParameters();
            
            // Step 3: Check which parameters would be hidden in target ViewMode
            var hiddenParameters = _stateManager.GetParameterVisibilityWarnings(toMode, activeParameters);
            if (hiddenParameters.Count > 0)
            {
                Console.WriteLine($"[StateTransition] ⚠️ {hiddenParameters.Count} parameters will be hidden in {toMode} mode");
                
                // Show parameter visibility warning
                var shouldProceed = ShowParameterVisibilityDialog(hiddenParameters, fromMode, toMode);
                if (!shouldProceed)
                {
                    Console.WriteLine($"[StateTransition] ❌ User cancelled transition due to parameter visibility concerns");
                    return;
                }
            }
            
            // Step 4: Preserve current state before transition
            PreserveCurrentState(activeParameters);
            
            // Step 5: Execute the actual ViewMode transition
            Console.WriteLine($"[StateTransition] 🔄 Executing template transition...");
            if (SetProperty(ref _currentViewMode, toMode))
            {
                ApplyViewModeInternal(toMode);
                ViewModeChanged?.Invoke(this, toMode);
            }
            
            // Step 6: Restore compatible state in new ViewMode
            RestoreCompatibleState(toMode);
            
            // Step 7: Provide transition guidance to user
            ShowTransitionGuidance(fromMode, toMode);
            
            Console.WriteLine($"[StateTransition] ✅ Intelligent transition completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StateTransition] ❌ Transition failed: {ex.Message}");
            
            // Emergency fallback - apply ViewMode without state preservation
            try
            {
                Console.WriteLine($"[StateTransition] 🚑 Attempting emergency fallback transition...");
                if (SetProperty(ref _currentViewMode, toMode))
                {
                    ApplyViewModeInternal(toMode);
                    ViewModeChanged?.Invoke(this, toMode);
                }
                Console.WriteLine($"[StateTransition] ✅ Emergency transition completed");
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"[StateTransition] 🔥 FATAL: Emergency fallback failed: {fallbackEx.Message}");
            }
        }
    }
    
    /// <summary>
    /// Get currently active parameters from the dynamic parameter system
    /// </summary>
    private List<string> GetCurrentActiveParameters()
    {
        // TODO: Integrate with DynamicParameterViewModel to get actual active parameters
        // For now, return a sample set for testing
        return new List<string> { "Temperature", "TopP", "TopK", "RepetitionPenalty" };
    }
    
    /// <summary>
    /// Preserve current parameter and UI state before ViewMode transition
    /// </summary>
    private void PreserveCurrentState(List<string> activeParameters)
    {
        try
        {
            Console.WriteLine($"[StateTransition] 💾 Preserving current state...");
            
            // Preserve parameter values (mock data for now)
            var parameterValues = new Dictionary<string, object>
            {
                { "Temperature", 0.7 },
                { "TopP", 0.9 },
                { "TopK", 40 },
                { "RepetitionPenalty", 1.1 }
            };
            
            _stateManager.PreserveParameterState(parameterValues);
            
            // Preserve UI state (scroll positions, expanded states, etc.)
            var uiState = new Dictionary<string, object>
            {
                { "AdvancedSectionExpanded", true },
                { "ScrollPosition", 150.0 },
                { "SelectedTab", "Parameters" }
            };
            
            _stateManager.PreserveUIState(uiState);
            
            Console.WriteLine($"[StateTransition] ✅ State preservation completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StateTransition] ⚠️ State preservation failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Restore compatible state in the new ViewMode
    /// </summary>
    private void RestoreCompatibleState(ViewMode newViewMode)
    {
        try
        {
            Console.WriteLine($"[StateTransition] 🔄 Restoring compatible state for {newViewMode}...");
            
            // Get parameters available in the new ViewMode
            var availableParameters = GetParametersForViewMode(newViewMode);
            
            // Restore compatible parameter values
            var restoredParameters = _stateManager.RestoreParameterState(availableParameters);
            Console.WriteLine($"[StateTransition] Restored {restoredParameters.Count} parameter values");
            
            // Restore UI state
            var restoredUIState = _stateManager.RestoreUIState();
            Console.WriteLine($"[StateTransition] Restored {restoredUIState.Count} UI state items");
            
            // TODO: Apply restored state to actual UI components
            
            Console.WriteLine($"[StateTransition] ✅ State restoration completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StateTransition] ⚠️ State restoration failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get parameters available in the specified ViewMode
    /// </summary>
    private List<string> GetParametersForViewMode(ViewMode viewMode)
    {
        // TODO: Integrate with ParameterRiskRegistry to get actual available parameters
        return viewMode switch
        {
            ViewMode.Novice => new List<string> { "Temperature", "TopP" },
            ViewMode.Enthusiast => new List<string> { "Temperature", "TopP", "TopK", "RepetitionPenalty" },
            ViewMode.Developer => new List<string> { "Temperature", "TopP", "TopK", "RepetitionPenalty", "Mirostat", "TailFreeSampling", "TypicalP" },
            _ => new List<string>()
        };
    }
    
    /// <summary>
    /// Show dialog warning about unsaved work that could be lost
    /// </summary>
    private bool ShowUnsavedWorkDialog(List<string> unsavedItems, ViewMode targetMode)
    {
        try
        {
            var message = $"You have unsaved work that could be affected by switching to {targetMode} mode:\n\n";
            message += string.Join("\n", unsavedItems.Select(item => $"• {item}"));
            message += "\n\nDo you want to continue with the ViewMode change?";
            
            var result = MessageBox.Show(
                message,
                "Unsaved Work Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No
            );
            
            return result == MessageBoxResult.Yes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StateTransition] Error showing unsaved work dialog: {ex.Message}");
            return true; // Allow transition if dialog fails
        }
    }
    
    /// <summary>
    /// Show dialog warning about parameters that will be hidden
    /// </summary>
    private bool ShowParameterVisibilityDialog(List<string> hiddenParameters, ViewMode fromMode, ViewMode toMode)
    {
        try
        {
            var message = $"Switching from {fromMode} to {toMode} mode will hide {hiddenParameters.Count} parameters:\n\n";
            message += string.Join("\n", hiddenParameters.Select(param => $"• {param}"));
            message += $"\n\nThese parameters will remain active but won't be visible in {toMode} mode.";
            message += "\nYou can switch back to see them again. Continue?";
            
            var result = MessageBox.Show(
                message,
                "Parameter Visibility Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information,
                MessageBoxResult.Yes
            );
            
            return result == MessageBoxResult.Yes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StateTransition] Error showing parameter visibility dialog: {ex.Message}");
            return true; // Allow transition if dialog fails
        }
    }
    
    /// <summary>
    /// Show guidance information about the transition
    /// </summary>
    private void ShowTransitionGuidance(ViewMode fromMode, ViewMode toMode)
    {
        try
        {
            // Users have eyes. They can see they switched views. No celebration popups needed.
            Console.WriteLine($"[UserPreferences] ViewMode switched: {fromMode} -> {toMode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StateTransition] Error showing transition guidance: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get smart ViewMode suggestion based on current parameter usage
    /// </summary>
    public ViewModeSuggestion GetViewModeSuggestion()
    {
        var activeParameters = GetCurrentActiveParameters();
        return _stateManager.SuggestOptimalViewMode(activeParameters);
    }
    
    /// <summary>
    /// SAFE DUAL PERSONALIZATION - Apply both theme and ViewMode safely
    /// Prevents resource dictionary conflicts without blocking UI thread
    /// </summary>
    /// <param name="theme">Theme to apply</param>
    /// <param name="viewMode">ViewMode to apply</param>
    public async Task ApplyBothAsync(ThemeMode theme, ViewMode viewMode)
    {
        var app = Application.Current;
        if (app == null) return;
        
        Console.WriteLine($"[SAFE] Starting safe dual personalization: {theme} + {viewMode}");
        
        try
        {
            // Apply theme first (UI thread to avoid cross-thread resource contention)
            await app.Dispatcher.InvokeAsync(() => ApplyThemeInternal(theme));
            
            // Apply ViewMode second (UI thread for consistent resource merge timing)
            await app.Dispatcher.InvokeAsync(() => ApplyViewModeInternal(viewMode));
            
            // Update internal state
            _currentTheme = theme;
            _currentViewMode = viewMode;
            
            // Fire events (already on UI thread; keep for clarity)
            await app.Dispatcher.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(CurrentTheme));
                OnPropertyChanged(nameof(CurrentViewMode));
                ThemeChanged?.Invoke(this, theme);
                ViewModeChanged?.Invoke(this, viewMode);
            });
            
            Console.WriteLine($"[SAFE] Dual personalization complete: {theme} + {viewMode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SAFE] Personalization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Internal method that performs the actual resource dictionary swapping
    /// Called automatically when CurrentTheme property changes
    /// </summary>
    private void ApplyThemeInternal(ThemeMode newTheme)
    {
        try
        {
            Console.WriteLine($"[UserPreferences] 🎨 Applying theme internally: {newTheme}");
            
            var app = Application.Current;
            if (app == null)
            {
                Console.WriteLine("[UserPreferences] ❌ Application.Current is null - cannot apply theme");
                return;
            }

            // Remove any existing theme dictionaries
            RemoveExistingThemeDictionaries(app);

            // Get the theme file path  
            string themeFile = GetThemeFilePath(newTheme);
            LoadNewTheme(newTheme, themeFile, app);
            
            Console.WriteLine($"[UserPreferences] ✅ Theme {newTheme} applied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserPreferences] ❌ Theme application failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove any existing theme dictionaries to prevent conflicts
    /// BRUTAL GENOCIDE MODE - NO SURVIVORS
    /// </summary>
    private void RemoveExistingThemeDictionaries(Application app)
    {
        var themesToRemove = new List<ResourceDictionary>();
        
        foreach (var dict in app.Resources.MergedDictionaries)
        {
            var source = dict.Source?.OriginalString;
            Console.WriteLine($"[THEME DEBUG] Found resource dictionary: {source}");
            
            if (source?.Contains("Theme") == true || source?.Contains("theme") == true)
            {
                Console.WriteLine($"[THEME DEBUG] Marking for removal: {source}");
                themesToRemove.Add(dict);
            }
        }
        
        foreach (var theme in themesToRemove)
        {
            app.Resources.MergedDictionaries.Remove(theme);
            Console.WriteLine($"[THEME DEBUG] Removed theme dictionary");
        }
        
        Console.WriteLine($"[THEME DEBUG] Genocide complete - {themesToRemove.Count} theme dictionaries eliminated");
    }

    /// <summary>
    /// Load new theme with brutal diagnostic verification
    /// </summary>
    private void LoadNewTheme(ThemeMode theme, string themeFile, Application app)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/Themes/{themeFile}");
            Console.WriteLine($"[THEME DEBUG] Attempting to load: {uri}");
            
            var newTheme = new ResourceDictionary { Source = uri };
            
            // Verify the dictionary loaded with expected brushes
            Console.WriteLine($"[THEME DEBUG] New theme loaded with {newTheme.Count} resources");
            
            // CRITICAL: Verify all required brush keys exist to prevent resource resolution cascade failure
            var requiredBrushes = new[] {
                "PrimaryDarkBrush", "SecondaryDarkBrush", "TertiaryDarkBrush",
                "AccentRedBrush", "AccentRedHoverBrush", "AccentRedPressedBrush", 
                "BorderBrush", "BorderHoverBrush",
                "TextPrimaryBrush", "TextSecondaryBrush", "TextMutedBrush", "TextDisabledBrush",
                "SuccessBrush", "ErrorBrush", "WarningBrush"
            };
            
            var missingBrushes = new List<string>();
            foreach (var brushKey in requiredBrushes)
            {
                if (!newTheme.Contains(brushKey))
                {
                    missingBrushes.Add(brushKey);
                    Console.WriteLine($"[THEME DEATH] ☠️ Missing brush: {brushKey}");
                }
                else
                {
                    Console.WriteLine($"[THEME HEALTH] ✅ Found brush: {brushKey}");
                }
            }
            
            if (missingBrushes.Count > 0)
            {
                Console.WriteLine($"[THEME DEATH] ☠️ FATAL: {missingBrushes.Count} missing brushes will cause resource resolution cascade failure!");
                throw new InvalidOperationException($"Theme {theme} missing required brushes: {string.Join(", ", missingBrushes)}");
            }
            
            Console.WriteLine($"[THEME HEALTH] ✅ All {requiredBrushes.Length} required brushes verified - theme is resurrection-ready");
            
            app.Resources.MergedDictionaries.Add(newTheme);
            Console.WriteLine($"[THEME DEBUG] Theme dictionary added to application");
            
            // NUCLEAR UI REFRESH - FORCE EVERY ELEMENT TO RE-EVALUATE RESOURCES
            ForceUIRefresh(theme, app);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[THEME DEBUG] FATAL: Theme loading failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// NUCLEAR UI REFRESH - Force every visual element to re-evaluate StaticResources
    /// This is digital warfare against WPF's stubborn resource caching
    /// </summary>
    private void ForceUIRefresh(ThemeMode theme, Application app)
    {
        Console.WriteLine($"[THEME DEBUG] 💥 INITIATING NUCLEAR UI REFRESH");
        
        try
        {
            // METHOD 1: Force main window invalidation
            foreach (Window window in app.Windows)
            {
                Console.WriteLine($"[THEME DEBUG] Refreshing window: {window.GetType().Name}");
                
                // Force complete visual refresh
                window.InvalidateVisual();
                window.UpdateLayout();
                
                // Recursively refresh all children
                RefreshVisualTree(window);
            }
            
            // METHOD 2: Trigger resource change notification
            app.Resources.MergedDictionaries.Add(new ResourceDictionary());
            app.Resources.MergedDictionaries.RemoveAt(app.Resources.MergedDictionaries.Count - 1);
            
            Console.WriteLine($"[THEME DEBUG] ✅ Nuclear UI refresh completed");
            
            // CRITICAL: Verify theme transition completed successfully
            VerifyThemeTransition(theme, app);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[THEME DEBUG] ⚠️ UI refresh failed: {ex.Message}");
            
            // Emergency fallback - attempt basic resource refresh
            try
            {
                Console.WriteLine($"[THEME DEBUG] 🚑 Attempting emergency theme recovery...");
                app.Resources.MergedDictionaries.Add(new ResourceDictionary());
                app.Resources.MergedDictionaries.RemoveAt(app.Resources.MergedDictionaries.Count - 1);
                Console.WriteLine($"[THEME DEBUG] ✅ Emergency recovery completed");
            }
            catch (Exception recoveryEx)
            {
                Console.WriteLine($"[THEME DEBUG] 🔥 FATAL: Emergency recovery failed: {recoveryEx.Message}");
            }
        }
    }

    /// <summary>
    /// Recursively refresh all visual elements in the tree
    /// ENHANCED: Special handling for buttons to fix color persistence issues
    /// </summary>
    private void RefreshVisualTree(System.Windows.DependencyObject parent)
    {
        if (parent is System.Windows.FrameworkElement element)
        {
            element.InvalidateVisual();
            element.UpdateLayout();
            
            // BUTTON-SPECIFIC REFRESH: Force style re-evaluation for nav buttons
            if (parent is System.Windows.Controls.Button button)
            {
                Console.WriteLine($"[THEME DEBUG] 🔘 Refreshing button: {button.Content}");
                
                // Force style re-evaluation by temporarily clearing and restoring
                var currentStyle = button.Style;
                button.Style = null;
                button.UpdateLayout();
                button.Style = currentStyle;
                button.InvalidateVisual();
                
                // Force background/foreground resource re-evaluation
                button.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                button.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                button.InvalidateVisual();
                
                Console.WriteLine($"[THEME DEBUG] ✅ Button refresh complete");
            }
        }
        
        // Recurse through children
        int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            RefreshVisualTree(child);
        }
    }

    /// <summary>
    /// Get the filename for the specified theme
    /// </summary>
    private string GetThemeFilePath(ThemeMode theme)
    {
        return theme switch
        {
            ThemeMode.Dark => "DarkTheme.xaml",
            ThemeMode.Light => "LightTheme.xaml", 
            ThemeMode.Cyberpunk => "CyberpunkTheme.xaml",
            ThemeMode.Minimal => "MinimalTheme.xaml",
            _ => "DarkTheme.xaml" // Fallback to dark theme
        };
    }
    
    /// <summary>
    /// Internal method that performs the actual template dictionary swapping
    /// Called automatically when CurrentViewMode property changes
    /// </summary>
    private void ApplyViewModeInternal(ViewMode newViewMode)
    {
        try
        {
            Console.WriteLine($"[UserPreferences] 🎯 Applying ViewMode internally: {newViewMode}");
            
            var app = Application.Current;
            if (app == null)
            {
                Console.WriteLine("[UserPreferences] ❌ Application.Current is null - cannot apply ViewMode");
                return;
            }

            // Remove any existing ViewMode template dictionaries
            RemoveExistingViewModeDictionaries(app);

            // Get the ViewMode template file path
            string viewModeFile = GetViewModeFilePath(newViewMode);
            LoadNewViewMode(newViewMode, viewModeFile, app);
            
            Console.WriteLine($"[UserPreferences] ✅ ViewMode {newViewMode} applied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserPreferences] ❌ ViewMode application failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove any existing ViewMode template dictionaries to prevent conflicts
    /// SURGICAL TEMPLATE GENOCIDE - PRECISION COGNITIVE CLEARING
    /// </summary>
    private void RemoveExistingViewModeDictionaries(Application app)
    {
        var viewModesToRemove = new List<ResourceDictionary>();
        
        foreach (var dict in app.Resources.MergedDictionaries)
        {
            var source = dict.Source?.OriginalString;
            Console.WriteLine($"[VIEWMODE DEBUG] Found resource dictionary: {source}");
            
            if (source?.Contains("ViewModes") == true && !source.Contains("SharedTemplates"))
            {
                Console.WriteLine($"[VIEWMODE DEBUG] Marking for removal: {source}");
                viewModesToRemove.Add(dict);
            }
        }
        
        foreach (var viewMode in viewModesToRemove)
        {
            app.Resources.MergedDictionaries.Remove(viewMode);
            Console.WriteLine($"[VIEWMODE DEBUG] Removed ViewMode template dictionary");
        }
        
        Console.WriteLine($"[VIEWMODE DEBUG] Template genocide complete - {viewModesToRemove.Count} ViewMode dictionaries eliminated");
    }

    /// <summary>
    /// Load new ViewMode templates with diagnostic verification
    /// </summary>
    private void LoadNewViewMode(ViewMode viewMode, string viewModeFile, Application app)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/ViewModes/{viewModeFile}");
            Console.WriteLine($"[VIEWMODE DEBUG] Attempting to load: {uri}");
            
            var newViewMode = new ResourceDictionary { Source = uri };
            
            // Verify the dictionary loaded with expected templates
            Console.WriteLine($"[VIEWMODE DEBUG] New ViewMode loaded with {newViewMode.Count} templates");
            
            // CRITICAL (diagnostics only): Verify required template keys exist
            // Wrapped behind DiagnosticsEnabled to avoid overhead during normal use
            var requiredTemplates = new[] {
                // Global shared templates
                "LoRACardTemplate",
                "ParameterControlTemplate", 
                "ChatMessageTemplate",
                "ChatStatusTemplate",
                "FloatParameterTemplate",
                "IntParameterTemplate",
                "BoolParameterTemplate",
                "GenerationProgressTemplate",
                "ModelSelectionTemplate",
                "StatusDisplayTemplate",
                "ErrorDisplayTemplate",
                
                // Parent navigation templates (with subtab menus)
                "ImagesTemplate",
                "VideoTemplate", 
                "VoiceTemplate",
                "EntitiesTemplate",
                
                // Images section templates
                "Text2ImageTemplate",
                "Image2ImageTemplate", 
                "InpaintingTemplate",
                "UpscalingTemplate",
                
                // Video section templates
                "Text2VideoTemplate",
                "Video2VideoTemplate",
                "MotionControlTemplate", 
                "TemporalEffectsTemplate",
                
                // 3D Models section templates
                "ThreeDModelsTemplate",
                "ModelPropertiesPanelTemplate",
                "ModelTreeViewTemplate", 
                "ViewportControlTemplate",
                
                // Voice section templates  
                "TTSConfigurationTemplate",
                "VoiceCloningTemplate",
                "RealTimeSynthesisTemplate",
                "AudioProcessingTemplate",
                
                // Entities section templates
                "EntityCreationTemplate",
                "BehavioralPatternsTemplate", 
                "InteractionTestingTemplate",
                "EntityManagementTemplate"
            };
            
            if (DiagnosticsEnabled)
            {
                var missingTemplates = new List<string>();
                foreach (var templateKey in requiredTemplates)
                {
                    if (!newViewMode.Contains(templateKey))
                    {
                        missingTemplates.Add(templateKey);
                        Console.WriteLine($"[VIEWMODE DEATH] ☠️ Missing template: {templateKey}");
                    }
                    else
                    {
                        Console.WriteLine($"[VIEWMODE HEALTH] ✅ Found template: {templateKey}");
                    }
                }
                if (missingTemplates.Count > 0)
                {
                    Console.WriteLine($"[VIEWMODE DEATH] ☠️ FATAL: {missingTemplates.Count} missing templates will cause UI rendering cascade failure!");
                    throw new InvalidOperationException($"ViewMode {viewMode} missing required templates: {string.Join(", ", missingTemplates)}");
                }
                Console.WriteLine($"[VIEWMODE HEALTH] ✅ All {requiredTemplates.Length} required templates verified - ViewMode is transformation-ready");
            }
            
            // Load shared templates first, then ViewMode-specific templates
            LoadSharedTemplates(app);
            app.Resources.MergedDictionaries.Add(newViewMode);
            Console.WriteLine($"[VIEWMODE DEBUG] ViewMode template dictionary added to application");
            
            if (DiagnosticsEnabled)
            {
                // Verify specific keys exist after merging
                bool CheckKey(string key)
                {
                    var ok = Application.Current.TryFindResource(key) != null;
                    Console.WriteLine(ok
                        ? $"[VIEWMODE CHECK] ✅ {key}"
                        : $"[VIEWMODE CHECK] ❌ {key} NOT FOUND");
                    return ok;
                }
                // Check a few critical templates (diagnostic only)
                CheckKey("ImagesTemplate");
                CheckKey("VideoTemplate");
                CheckKey("VoiceTemplate");
                CheckKey("EntitiesTemplate");
            }
            
            // COGNITIVE UI REFRESH - FORCE TEMPLATE RE-EVALUATION
            ForceCognitiveUIRefresh(app);
        }
        catch (System.Xaml.XamlParseException xpe)
        {
            Console.WriteLine(
                $"[VIEWMODE DEBUG] FATAL XAML: {xpe.Message}\n" +
                $"  Line={xpe.LineNumber}, Pos={xpe.LinePosition}\n" +
                $"{xpe}\n" +
                $"  Inner: {xpe.InnerException}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VIEWMODE DEBUG] FATAL: {ex}\n  Inner: {ex.InnerException}");
            throw;
        }
    }
    
    /// <summary>
    /// Load shared templates that are used across all ViewModes
    /// </summary>
    private void LoadSharedTemplates(Application app)
    {
        try
        {
            var sharedUri = new Uri("pack://application:,,,/Resources/ViewModes/SharedTemplates.xaml");
            var sharedTemplates = new ResourceDictionary { Source = sharedUri };
            
            // Remove any existing shared templates first
            var existingShared = app.Resources.MergedDictionaries
                .Where(d => d.Source?.OriginalString?.Contains("SharedTemplates") == true)
                .ToList();
            
            foreach (var shared in existingShared)
            {
                app.Resources.MergedDictionaries.Remove(shared);
            }
            
            app.Resources.MergedDictionaries.Add(sharedTemplates);
            Console.WriteLine($"[VIEWMODE DEBUG] Shared templates loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VIEWMODE DEBUG] Warning: Failed to load shared templates: {ex.Message}");
        }
    }

    /// <summary>
    /// COGNITIVE UI REFRESH - Force every template to re-evaluate
    /// This is cognitive warfare against WPF's template caching
    /// </summary>
    private void ForceCognitiveUIRefresh(Application app)
    {
        if (!DiagnosticsEnabled)
        {
            // Skip heavy visual tree invalidation in production to avoid lag
            return;
        }
        Console.WriteLine($"[VIEWMODE DEBUG] 🧠 INITIATING COGNITIVE UI REFRESH");
        
        try
        {
            // Force template re-evaluation by triggering resource change notification
            foreach (Window window in app.Windows)
            {
                Console.WriteLine($"[VIEWMODE DEBUG] Refreshing cognitive templates in window: {window.GetType().Name}");
                
                // Force complete template refresh
                window.InvalidateVisual();
                window.UpdateLayout();
                
                // Recursively refresh all templated children
                RefreshTemplatedElements(window);
            }
            
            // METHOD: Trigger template resource change notification
            app.Resources.MergedDictionaries.Add(new ResourceDictionary());
            app.Resources.MergedDictionaries.RemoveAt(app.Resources.MergedDictionaries.Count - 1);
            
            Console.WriteLine($"[VIEWMODE DEBUG] ✅ Cognitive UI refresh completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VIEWMODE DEBUG] ⚠️ Cognitive UI refresh failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively refresh all templated elements in the visual tree
    /// </summary>
    private void RefreshTemplatedElements(System.Windows.DependencyObject parent)
    {
        if (parent is System.Windows.Controls.ContentControl contentControl)
        {
            // Force ContentTemplate re-evaluation
            var template = contentControl.ContentTemplate;
            contentControl.ContentTemplate = null;
            contentControl.ContentTemplate = template;
            contentControl.InvalidateVisual();
        }
        
        if (parent is System.Windows.FrameworkElement element)
        {
            element.InvalidateVisual();
            element.UpdateLayout();
        }
        
        // Recurse through children
        int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            RefreshTemplatedElements(child);
        }
    }

    /// <summary>
    /// Get the filename for the specified ViewMode
    /// </summary>
    private string GetViewModeFilePath(ViewMode viewMode)
    {
        return viewMode switch
        {
            ViewMode.Novice => "NoviceTemplates.xaml",
            ViewMode.Enthusiast => "EnthusiastTemplates.xaml", 
            ViewMode.Developer => "DeveloperTemplates.xaml",
            _ => "NoviceTemplates.xaml" // Fallback to novice templates
        };
    }
    
    #region SAFE RESOURCE MANAGEMENT
    
    /// <summary>
    /// Safely manage resource dictionaries without blocking UI thread
    /// </summary>
    private void SafeResourceManagement()
    {
        // Resource management is now handled safely in ApplyThemeInternal and ApplyViewModeInternal
        Console.WriteLine("[SAFE] Resource management initialized");
    }
    
    #endregion

    #endregion

    #region Resource Validation Protocol

    /// <summary>
    /// Validate all theme resource dictionaries at startup to prevent runtime crashes
    /// ARCHITECTURAL SAFETY NET - Critical for preventing resource binding failures
    /// </summary>
    private void ValidateAllThemeResources()
    {
        var allThemes = new[] { ThemeMode.Dark, ThemeMode.Light, ThemeMode.Cyberpunk, ThemeMode.Minimal };
        var criticalBrushes = new[]
        {
            // Standard 15-brush constitution
            "PrimaryDarkBrush", "SecondaryDarkBrush", "TertiaryDarkBrush",
            "AccentRedBrush", "AccentRedHoverBrush", "AccentRedPressedBrush",
            "BorderBrush", "BorderHoverBrush",
            "TextPrimaryBrush", "TextSecondaryBrush", "TextMutedBrush", "TextDisabledBrush",
            "SuccessBrush", "ErrorBrush", "WarningBrush",
            // Essential extended brushes
            "ButtonBackgroundBrush", "ButtonHoverBrush", "ButtonPressedBrush",
            "ButtonTextNormalBrush", "ButtonTextHoverBrush", "ButtonTextBrush",
            "DropdownTextBrush", "DropdownTextHoverBrush"
        };

        var validationErrors = new List<string>();

        foreach (var theme in allThemes)
        {
            try
            {
                Console.WriteLine($"[VALIDATION] 🔍 Validating {theme} theme...");
                var themeDict = LoadThemeForValidation(theme);
                
                if (themeDict == null)
                {
                    validationErrors.Add($"FATAL: {theme} theme dictionary failed to load");
                    continue;
                }

                var missingBrushes = new List<string>();
                foreach (var brushKey in criticalBrushes)
                {
                    if (!themeDict.Contains(brushKey))
                    {
                        missingBrushes.Add(brushKey);
                    }
                }

                if (missingBrushes.Count > 0)
                {
                    validationErrors.Add($"CRITICAL: {theme} theme missing brushes: {string.Join(", ", missingBrushes)}");
                }
                else
                {
                    Console.WriteLine($"[VALIDATION] ✅ {theme} theme validated successfully ({themeDict.Count} resources)");
                }
            }
            catch (Exception ex)
            {
                validationErrors.Add($"FATAL: {theme} theme validation crashed: {ex.Message}");
            }
        }

        if (validationErrors.Count > 0)
        {
            Console.WriteLine($"[VALIDATION] ⚠️ RESOURCE VALIDATION FAILURES:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($"[VALIDATION]   - {error}");
            }
            
            // Show critical error dialog but allow app to continue
            Application.Current?.Dispatcher?.BeginInvoke(() =>
            {
                MessageBox.Show(
                    $"Theme Resource Validation Errors:\n\n{string.Join("\n", validationErrors)}\n\nSome themes may not work correctly.",
                    "Resource Validation Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            });
        }
        else
        {
            Console.WriteLine($"[VALIDATION] 🎆 All theme resources validated successfully!");
        }
    }

    /// <summary>
    /// Load theme dictionary for validation without applying it
    /// </summary>
    private ResourceDictionary? LoadThemeForValidation(ThemeMode theme)
    {
        try
        {
            var themeFile = GetThemeFilePath(theme);
            var uri = new Uri($"pack://application:,,,/Resources/Themes/{themeFile}");
            return new ResourceDictionary { Source = uri };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VALIDATION] Failed to load {theme} for validation: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Verify theme transition completed successfully
    /// POST-TRANSITION VERIFICATION PROTOCOL
    /// </summary>
    private void VerifyThemeTransition(ThemeMode expectedTheme, Application app)
    {
        try
        {
            Console.WriteLine($"[VERIFICATION] 🔍 Verifying {expectedTheme} theme transition...");
            
            // Verify theme dictionary is loaded
            var themeFile = GetThemeFilePath(expectedTheme);
            var expectedUri = $"/Resources/Themes/{themeFile}";
            
            bool themeFound = false;
            ResourceDictionary? activeThemeDict = null;
            
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source?.OriginalString?.Contains(themeFile) == true)
                {
                    themeFound = true;
                    activeThemeDict = dict;
                    break;
                }
            }

            if (!themeFound)
            {
                Console.WriteLine($"[VERIFICATION] ⚠️ WARNING: {expectedTheme} theme dictionary not found in merged dictionaries");
                return;
            }

            // Verify critical brushes are accessible
            var criticalBrushes = new[] { "PrimaryDarkBrush", "AccentRedBrush", "TextPrimaryBrush", "BorderBrush" };
            var missingBrushes = new List<string>();
            
            foreach (var brushKey in criticalBrushes)
            {
                try
                {
                    var brush = app.FindResource(brushKey);
                    if (brush == null)
                    {
                        missingBrushes.Add(brushKey);
                    }
                }
                catch
                {
                    missingBrushes.Add(brushKey);
                }
            }

            if (missingBrushes.Count > 0)
            {
                Console.WriteLine($"[VERIFICATION] ⚠️ WARNING: Critical brushes not accessible: {string.Join(", ", missingBrushes)}");
            }
            else
            {
                Console.WriteLine($"[VERIFICATION] ✅ {expectedTheme} theme transition verified successfully!");
                Console.WriteLine($"[VERIFICATION] Active theme dictionary: {activeThemeDict?.Count} resources loaded");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VERIFICATION] ⚠️ Theme verification failed: {ex.Message}");
        }
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}