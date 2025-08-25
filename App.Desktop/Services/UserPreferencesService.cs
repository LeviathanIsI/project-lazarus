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
public class UserPreferencesService : INotifyPropertyChanged
{
    private ViewMode _currentViewMode = ViewMode.Novice;
    private ThemeMode _currentTheme = ThemeMode.Dark;

    #region Constructor

    /// <summary>
    /// Initialize UserPreferencesService with default preferences
    /// </summary>
    public UserPreferencesService()
    {
        Console.WriteLine($"[UserPreferences] 🏗️ Initializing UserPreferencesService...");
        Console.WriteLine($"[UserPreferences] Default theme: {_currentTheme}");
        Console.WriteLine($"[UserPreferences] Default ViewMode: {_currentViewMode}");
        
        // Initialize ViewMode templates on startup
        Console.WriteLine($"[UserPreferences] 🎭 Loading default ViewMode templates...");
        ApplyViewModeInternal(_currentViewMode);
        Console.WriteLine($"[UserPreferences] ✅ UserPreferencesService initialized successfully");
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
            if (SetProperty(ref _currentViewMode, value))
            {
                Console.WriteLine($"[UserPreferences] 🎯 ViewMode changed to: {value}");
                
                // Apply the ViewMode immediately when property is set
                ApplyViewModeInternal(value);
                
                ViewModeChanged?.Invoke(this, value);
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
        
        // Update the current ViewMode property, which will trigger ApplyViewModeInternal
        CurrentViewMode = newViewMode;
        
        Console.WriteLine($"[VIEWMODE DEBUG] ViewMode application completed");
    }
    
    /// <summary>
    /// ATOMIC DUAL PERSONALIZATION - Apply both theme and ViewMode in single batch
    /// Prevents resource dictionary conflicts and ensures smooth transitions
    /// </summary>
    /// <param name="theme">Theme to apply</param>
    /// <param name="viewMode">ViewMode to apply</param>
    public void ApplyBoth(ThemeMode theme, ViewMode viewMode)
    {
        var app = Application.Current;
        if (app == null) return;
        
        Console.WriteLine($"[ATOMIC] Starting atomic dual personalization: {theme} + {viewMode}");
        
        // Use Dispatcher.BeginInvoke to ensure atomic UI thread operation
        app.Dispatcher.BeginInvoke(() => {
            try
            {
                using (app.Dispatcher.DisableProcessing()) // No re-entrancy during swap
                {
                    Console.WriteLine($"[ATOMIC] UI processing disabled - performing atomic swap");
                    
                    // Replace dictionaries at fixed indices to prevent conflicts
                    ReplaceAtomicTheme(theme, app);
                    ReplaceAtomicViewMode(viewMode, app);
                    
                    // Update internal state
                    _currentTheme = theme;
                    _currentViewMode = viewMode;
                    
                    Console.WriteLine($"[ATOMIC] Atomic swap completed successfully");
                }
                
                // Fire events after re-enabling processing
                OnPropertyChanged(nameof(CurrentTheme));
                OnPropertyChanged(nameof(CurrentViewMode));
                ThemeChanged?.Invoke(this, theme);
                ViewModeChanged?.Invoke(this, viewMode);
                
                Console.WriteLine($"[ATOMIC] Dual personalization complete: {theme} + {viewMode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ATOMIC] FATAL: Atomic operation failed: {ex.Message}");
            }
        }, System.Windows.Threading.DispatcherPriority.Send);
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
            ForceUIRefresh(app);
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
    private void ForceUIRefresh(Application app)
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[THEME DEBUG] ⚠️ UI refresh failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively refresh all visual elements in the tree
    /// </summary>
    private void RefreshVisualTree(System.Windows.DependencyObject parent)
    {
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
            
            // CRITICAL: Verify all required template keys exist
            var requiredTemplates = new[] {
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
                "ErrorDisplayTemplate"
            };
            
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
            
            // Load shared templates first, then ViewMode-specific templates
            LoadSharedTemplates(app);
            app.Resources.MergedDictionaries.Add(newViewMode);
            Console.WriteLine($"[VIEWMODE DEBUG] ViewMode template dictionary added to application");
            
            // COGNITIVE UI REFRESH - FORCE TEMPLATE RE-EVALUATION
            ForceCognitiveUIRefresh(app);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VIEWMODE DEBUG] FATAL: ViewMode loading failed: {ex.Message}");
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
    
    #region ATOMIC REPLACEMENT METHODS - Fixed Index Approach
    
    // FIXED INDICES to prevent resource dictionary chaos
    private const int THEME_DICT_INDEX = 0;
    private const int SHARED_TEMPLATES_INDEX = 1;
    private const int VIEWMODE_DICT_INDEX = 2;
    
    /// <summary>
    /// ATOMIC THEME REPLACEMENT - Replace at fixed index to prevent conflicts
    /// </summary>
    private void ReplaceAtomicTheme(ThemeMode theme, Application app)
    {
        try
        {
            var themeFile = GetThemeFilePath(theme);
            var uri = new Uri($"pack://application:,,,/Resources/Themes/{themeFile}");
            var newThemeDict = new ResourceDictionary { Source = uri };
            
            Console.WriteLine($"[ATOMIC] Loading theme: {uri}");
            
            // Ensure we have enough dictionaries in the collection
            while (app.Resources.MergedDictionaries.Count <= THEME_DICT_INDEX)
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary());
            }
            
            // Replace at fixed index - no add/remove chaos
            app.Resources.MergedDictionaries[THEME_DICT_INDEX] = newThemeDict;
            Console.WriteLine($"[ATOMIC] Theme replaced at index {THEME_DICT_INDEX}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ATOMIC] Theme replacement failed: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// ATOMIC VIEWMODE REPLACEMENT - Replace at fixed indices
    /// </summary>
    private void ReplaceAtomicViewMode(ViewMode viewMode, Application app)
    {
        try
        {
            Console.WriteLine($"[TEMPLATE DEBUG] ========== ATOMIC VIEWMODE REPLACEMENT START ==========");
            Console.WriteLine($"[TEMPLATE DEBUG] Target ViewMode: {viewMode}");
            Console.WriteLine($"[TEMPLATE DEBUG] Current resource dictionary count: {app.Resources.MergedDictionaries.Count}");
            
            // Load shared templates first
            var sharedUri = new Uri("pack://application:,,,/Resources/ViewModes/SharedTemplates.xaml");
            Console.WriteLine($"[TEMPLATE DEBUG] Loading SharedTemplates from: {sharedUri}");
            var sharedDict = new ResourceDictionary { Source = sharedUri };
            Console.WriteLine($"[TEMPLATE DEBUG] SharedTemplates loaded with {sharedDict.Count} resources");
            
            // Log SharedTemplates contents for verification
            Console.WriteLine($"[TEMPLATE DEBUG] SharedTemplates keys:");
            foreach (var key in sharedDict.Keys)
            {
                Console.WriteLine($"[TEMPLATE DEBUG]   - {key}");
            }
            
            // Load ViewMode-specific templates
            var viewModeFile = GetViewModeFilePath(viewMode);
            var viewModeUri = new Uri($"pack://application:,,,/Resources/ViewModes/{viewModeFile}");
            Console.WriteLine($"[TEMPLATE DEBUG] Loading ViewMode templates from: {viewModeUri}");
            var viewModeDict = new ResourceDictionary { Source = viewModeUri };
            Console.WriteLine($"[TEMPLATE DEBUG] ViewMode templates loaded with {viewModeDict.Count} resources");
            
            // Log ViewMode template contents for verification
            Console.WriteLine($"[TEMPLATE DEBUG] ViewMode template keys:");
            foreach (var key in viewModeDict.Keys)
            {
                Console.WriteLine($"[TEMPLATE DEBUG]   - {key}");
            }
            
            Console.WriteLine($"[ATOMIC] Loading ViewMode: {viewModeUri}");
            
            // Ensure we have enough dictionaries in the collection
            while (app.Resources.MergedDictionaries.Count <= VIEWMODE_DICT_INDEX)
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary());
                Console.WriteLine($"[TEMPLATE DEBUG] Added empty resource dictionary, count now: {app.Resources.MergedDictionaries.Count}");
            }
            
            Console.WriteLine($"[TEMPLATE DEBUG] Replacing at SHARED_TEMPLATES_INDEX ({SHARED_TEMPLATES_INDEX})");
            Console.WriteLine($"[TEMPLATE DEBUG] Replacing at VIEWMODE_DICT_INDEX ({VIEWMODE_DICT_INDEX})");
            
            // Replace at fixed indices - no add/remove chaos
            app.Resources.MergedDictionaries[SHARED_TEMPLATES_INDEX] = sharedDict;
            app.Resources.MergedDictionaries[VIEWMODE_DICT_INDEX] = viewModeDict;
            
            Console.WriteLine($"[ATOMIC] ViewMode replaced at indices {SHARED_TEMPLATES_INDEX}, {VIEWMODE_DICT_INDEX}");
            
            // Verify replacement was successful
            Console.WriteLine($"[TEMPLATE DEBUG] ========== POST-REPLACEMENT VERIFICATION ==========");
            Console.WriteLine($"[TEMPLATE DEBUG] Final resource dictionary count: {app.Resources.MergedDictionaries.Count}");
            
            // Verify SharedTemplates at correct index
            var verifyShared = app.Resources.MergedDictionaries[SHARED_TEMPLATES_INDEX];
            Console.WriteLine($"[TEMPLATE DEBUG] SharedTemplates at index {SHARED_TEMPLATES_INDEX}: {verifyShared.Count} resources");
            
            // Verify ViewMode templates at correct index
            var verifyViewMode = app.Resources.MergedDictionaries[VIEWMODE_DICT_INDEX];
            Console.WriteLine($"[TEMPLATE DEBUG] ViewMode templates at index {VIEWMODE_DICT_INDEX}: {verifyViewMode.Count} resources");
            
            // Test critical template resolution
            var testLoRATemplate = app.TryFindResource("LoRACardTemplate");
            Console.WriteLine($"[TEMPLATE DEBUG] LoRACardTemplate resolution: {(testLoRATemplate != null ? "SUCCESS" : "FAILED")}");
            
            if (testLoRATemplate == null)
            {
                Console.WriteLine($"[TEMPLATE DEBUG] ⚠️ CRITICAL: LoRACardTemplate not found after replacement!");
                Console.WriteLine($"[TEMPLATE DEBUG] Available resources in ViewMode dictionary:");
                foreach (var key in verifyViewMode.Keys)
                {
                    Console.WriteLine($"[TEMPLATE DEBUG]   - {key}");
                }
            }
            
            Console.WriteLine($"[TEMPLATE DEBUG] ========== ATOMIC VIEWMODE REPLACEMENT COMPLETE ==========");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ATOMIC] ViewMode replacement failed: {ex.Message}");
            throw;
        }
    }
    
    #endregion

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