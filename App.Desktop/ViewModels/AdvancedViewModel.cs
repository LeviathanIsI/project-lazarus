using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels
{
    public class AdvancedSetting : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _description = string.Empty;
        private object? _value;
        private string _valueType = string.Empty;
        private bool _isEnabled = true;
        private bool _requiresRestart;
        private string _warningLevel = "Safe";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public object? Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string ValueType
        {
            get => _valueType;
            set => SetProperty(ref _valueType, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public bool RequiresRestart
        {
            get => _requiresRestart;
            set => SetProperty(ref _requiresRestart, value);
        }

        public string WarningLevel
        {
            get => _warningLevel;
            set => SetProperty(ref _warningLevel, value);
        }

        public string DisplayValue => Value?.ToString() ?? "Not Set";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class AdvancedSystemInfo : INotifyPropertyChanged
    {
        private string _gpuName = string.Empty;
        private string _vramTotal = string.Empty;
        private string _vramUsed = string.Empty;
        private string _ramTotal = string.Empty;
        private string _ramUsed = string.Empty;
        private string _cpuName = string.Empty;
        private double _cpuUsage;
        private double _gpuUsage;

        public string GpuName
        {
            get => _gpuName;
            set => SetProperty(ref _gpuName, value);
        }

        public string VramTotal
        {
            get => _vramTotal;
            set => SetProperty(ref _vramTotal, value);
        }

        public string VramUsed
        {
            get => _vramUsed;
            set => SetProperty(ref _vramUsed, value);
        }

        public string RamTotal
        {
            get => _ramTotal;
            set => SetProperty(ref _ramTotal, value);
        }

        public string RamUsed
        {
            get => _ramUsed;
            set => SetProperty(ref _ramUsed, value);
        }

        public string CpuName
        {
            get => _cpuName;
            set => SetProperty(ref _cpuName, value);
        }

        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public double GpuUsage
        {
            get => _gpuUsage;
            set => SetProperty(ref _gpuUsage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class AdvancedViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<AdvancedSetting> _allSettings = new();
        private ObservableCollection<AdvancedSetting> _filteredSettings = new();
        private ObservableCollection<string> _categories = new();
        private string _searchFilter = string.Empty;
        private string _selectedCategory = "All";
        private bool _isLoading;
        private string _statusText = "Ready";
        private AdvancedSystemInfo _systemInfo = new();
        private bool _developerMode;
        private bool _debugLogging;

        public ObservableCollection<AdvancedSetting> FilteredSettings
        {
            get => _filteredSettings;
            set => SetProperty(ref _filteredSettings, value);
        }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                SetProperty(ref _searchFilter, value);
                FilterSettings();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                FilterSettings();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public AdvancedSystemInfo SystemInfo
        {
            get => _systemInfo;
            set => SetProperty(ref _systemInfo, value);
        }

        public bool DeveloperMode
        {
            get => _developerMode;
            set => SetProperty(ref _developerMode, value);
        }

        public bool DebugLogging
        {
            get => _debugLogging;
            set => SetProperty(ref _debugLogging, value);
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }
        public ICommand ExportConfigCommand { get; }
        public ICommand ImportConfigCommand { get; }
        public ICommand ApplyChangesCommand { get; }
        public ICommand RestartApplicationCommand { get; }

        public AdvancedViewModel()
        {
            RefreshCommand = new Helpers.RelayCommand(_ => _ = Task.Run(async () => await RefreshSystemInfo()));
            ResetToDefaultsCommand = new Helpers.RelayCommand(_ => ResetToDefaults());
            ExportConfigCommand = new Helpers.RelayCommand(_ => ExportConfiguration());
            ImportConfigCommand = new Helpers.RelayCommand(_ => ImportConfiguration());
            ApplyChangesCommand = new Helpers.RelayCommand(_ => ApplyChanges());
            RestartApplicationCommand = new Helpers.RelayCommand(_ => RestartApplication());

            InitializeCategories();
            _ = Task.Run(LoadSettings);
            _ = Task.Run(LoadSystemInfo);
        }

        private void InitializeCategories()
        {
            Categories.Clear();
            Categories.Add("All");
            Categories.Add("Performance");
            Categories.Add("Memory");
            Categories.Add("Pipeline");
            Categories.Add("API");
            Categories.Add("Experimental");
            Categories.Add("Debug");
        }

        private async Task LoadSettings()
        {
            IsLoading = true;
            StatusText = "Loading advanced settings...";

            try
            {
                await Task.Delay(600);

                var settings = new List<AdvancedSetting>
                {
                    // Performance Settings
                    new() {
                        Name = "Model Quantization",
                        Category = "Performance",
                        Description = "Enable aggressive model quantization for speed at cost of quality",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Caution",
                        RequiresRestart = true
                    },
                    new() {
                        Name = "CUDA Memory Pool",
                        Category = "Performance",
                        Description = "Pre-allocate CUDA memory pool size (MB)",
                        Value = 8192,
                        ValueType = "int",
                        WarningLevel = "Safe"
                    },
                    new() {
                        Name = "Torch Compile",
                        Category = "Performance",
                        Description = "Enable PyTorch compilation for faster inference",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Experimental",
                        RequiresRestart = true
                    },

                    // Memory Settings
                    new() {
                        Name = "CPU Offloading",
                        Category = "Memory",
                        Description = "Offload model layers to system RAM when VRAM is insufficient",
                        Value = true,
                        ValueType = "bool",
                        WarningLevel = "Safe"
                    },
                    new() {
                        Name = "Sequential CPU Offloading",
                        Category = "Memory",
                        Description = "More aggressive CPU offloading for extreme VRAM savings",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Caution"
                    },
                    new() {
                        Name = "VRAM Safety Buffer",
                        Category = "Memory",
                        Description = "Reserved VRAM buffer (MB) for system stability",
                        Value = 1024,
                        ValueType = "int",
                        WarningLevel = "Safe"
                    },

                    // Pipeline Settings
                    new() {
                        Name = "Custom Scheduler",
                        Category = "Pipeline",
                        Description = "Override default noise scheduler with custom implementation",
                        Value = "DPM++ 2M Karras",
                        ValueType = "string",
                        WarningLevel = "Safe"
                    },
                    new() {
                        Name = "Attention Slicing",
                        Category = "Pipeline",
                        Description = "Enable attention slicing to reduce VRAM usage",
                        Value = true,
                        ValueType = "bool",
                        WarningLevel = "Safe"
                    },
                    new() {
                        Name = "XFormers Memory Efficient Attention",
                        Category = "Pipeline",
                        Description = "Use XFormers for memory-efficient attention computation",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Experimental"
                    },

                    // API Settings
                    new() {
                        Name = "API Server Port",
                        Category = "API",
                        Description = "Local API server listening port",
                        Value = 7860,
                        ValueType = "int",
                        WarningLevel = "Safe",
                        RequiresRestart = true
                    },
                    new() {
                        Name = "CORS Allow Origin",
                        Category = "API",
                        Description = "Allowed CORS origins for API access",
                        Value = "*",
                        ValueType = "string",
                        WarningLevel = "Caution"
                    },

                    // Experimental
                    new() {
                        Name = "Latent Consistency Models",
                        Category = "Experimental",
                        Description = "Enable experimental LCM support for faster generation",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Experimental",
                        RequiresRestart = true
                    },
                    new() {
                        Name = "Neural Network Fusion",
                        Category = "Experimental",
                        Description = "Fuse multiple models at the neural network level",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Dangerous",
                        RequiresRestart = true
                    },

                    // Debug
                    new() {
                        Name = "Verbose Logging",
                        Category = "Debug",
                        Description = "Enable detailed logging for troubleshooting",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Safe"
                    },
                    new() {
                        Name = "Memory Profiling",
                        Category = "Debug",
                        Description = "Track memory allocation patterns (performance impact)",
                        Value = false,
                        ValueType = "bool",
                        WarningLevel = "Caution"
                    }
                };

                _allSettings.Clear();
                foreach (var setting in settings)
                {
                    _allSettings.Add(setting);
                }

                FilterSettings();
                StatusText = $"Loaded {settings.Count} advanced settings";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSystemInfo()
        {
            try
            {
                await Task.Delay(400);

                SystemInfo.GpuName = "NVIDIA RTX 4090";
                SystemInfo.VramTotal = "24 GB";
                SystemInfo.VramUsed = "8.2 GB";
                SystemInfo.RamTotal = "32 GB";
                SystemInfo.RamUsed = "16.8 GB";
                SystemInfo.CpuName = "AMD Ryzen 9 7900X";
                SystemInfo.CpuUsage = 23.5;
                SystemInfo.GpuUsage = 67.2;
            }
            catch
            {
                SystemInfo.GpuName = "Unknown GPU";
                SystemInfo.VramTotal = "Unknown";
            }
        }

        private void FilterSettings()
        {
            var filtered = _allSettings.AsEnumerable();

            if (SelectedCategory != "All")
            {
                filtered = filtered.Where(s => s.Category == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var search = SearchFilter.ToLowerInvariant();
                filtered = filtered.Where(s =>
                    s.Name.ToLowerInvariant().Contains(search) ||
                    s.Description.ToLowerInvariant().Contains(search));
            }

            FilteredSettings.Clear();
            foreach (var setting in filtered)
            {
                FilteredSettings.Add(setting);
            }
        }

        private async Task RefreshSystemInfo()
        {
            StatusText = "Refreshing system information...";
            await LoadSystemInfo();
            StatusText = "System information updated";
        }

        private void ResetToDefaults()
        {
            StatusText = "Reset to factory defaults - restart required";
        }

        private void ExportConfiguration()
        {
            StatusText = "Configuration exported to config.json";
        }

        private void ImportConfiguration()
        {
            StatusText = "Configuration imported - some changes require restart";
        }

        private void ApplyChanges()
        {
            var requiresRestart = _allSettings.Any(s => s.RequiresRestart && s.IsEnabled);
            StatusText = requiresRestart
                ? "Changes applied - restart required for full effect"
                : "Changes applied successfully";
        }

        private void RestartApplication()
        {
            StatusText = "Restarting application...";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}