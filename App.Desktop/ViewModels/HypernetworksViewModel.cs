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
    public class HypernetworkItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _description = string.Empty;
        private string _fileSize = string.Empty;
        private string _hash = string.Empty;
        private string _architecture = string.Empty;
        private string _baseModel = string.Empty;
        private string _quality = string.Empty;
        private bool _isActive;
        private double _strength = 1.0;
        private string _author = string.Empty;
        private int _layerCount;
        private string _activationFunction = string.Empty;
        private DateTime _createdDate = DateTime.Now;

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

        public string FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public string Hash
        {
            get => _hash;
            set => SetProperty(ref _hash, value);
        }

        public string Architecture
        {
            get => _architecture;
            set => SetProperty(ref _architecture, value);
        }

        public string BaseModel
        {
            get => _baseModel;
            set => SetProperty(ref _baseModel, value);
        }

        public string Quality
        {
            get => _quality;
            set => SetProperty(ref _quality, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public double Strength
        {
            get => _strength;
            set => SetProperty(ref _strength, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public int LayerCount
        {
            get => _layerCount;
            set => SetProperty(ref _layerCount, value);
        }

        public string ActivationFunction
        {
            get => _activationFunction;
            set => SetProperty(ref _activationFunction, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public string StrengthDisplay => $"{Strength:F1}x";
        public string LayerDisplay => $"{LayerCount} layers";
        public string CreatedDateFormatted => CreatedDate.ToString("MMM dd, yyyy");

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

    public class HypernetworksViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<HypernetworkItem> _availableHypernetworks = new();
        private ObservableCollection<HypernetworkItem> _activeHypernetworks = new();
        private ObservableCollection<string> _categories = new();
        private string _searchFilter = string.Empty;
        private string _selectedCategory = "All";
        private bool _isLoading;
        private string _statusText = "Ready";

        public ObservableCollection<HypernetworkItem> AvailableHypernetworks
        {
            get => _availableHypernetworks;
            set => SetProperty(ref _availableHypernetworks, value);
        }

        public ObservableCollection<HypernetworkItem> ActiveHypernetworks
        {
            get => _activeHypernetworks;
            set => SetProperty(ref _activeHypernetworks, value);
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
                FilterHypernetworks();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                FilterHypernetworks();
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

        public int TotalHypernetworksCount => AvailableHypernetworks.Count;
        public int ActiveHypernetworksCount => ActiveHypernetworks.Count;
        public bool HasActiveHypernetworks => ActiveHypernetworks.Count > 0;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ActivateHypernetworkCommand { get; }
        public ICommand DeactivateHypernetworkCommand { get; }
        public ICommand TestHypernetworkCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand AdjustStrengthCommand { get; }

        private readonly ObservableCollection<HypernetworkItem> _allHypernetworks = new();

        public HypernetworksViewModel()
        {
            RefreshCommand = new Helpers.RelayCommand(_ => _ = Task.Run(async () => await RefreshHypernetworks()));
            ActivateHypernetworkCommand = new Helpers.RelayCommand(param => ActivateHypernetwork(param as HypernetworkItem));
            DeactivateHypernetworkCommand = new Helpers.RelayCommand(param => DeactivateHypernetwork(param as HypernetworkItem));
            TestHypernetworkCommand = new Helpers.RelayCommand(param => _ = Task.Run(async () => await TestHypernetwork(param as HypernetworkItem)));
            ClearAllCommand = new Helpers.RelayCommand(_ => ClearAllHypernetworks());
            AdjustStrengthCommand = new Helpers.RelayCommand(param => AdjustStrength(param as HypernetworkItem));

            InitializeCategories();
            _ = Task.Run(LoadSampleData);
        }

        private void InitializeCategories()
        {
            Categories.Clear();
            Categories.Add("All");
            Categories.Add("Style");
            Categories.Add("Character");
            Categories.Add("Artist");
            Categories.Add("Concept");
            Categories.Add("Technique");
            Categories.Add("Experimental");
        }

        private async Task LoadSampleData()
        {
            IsLoading = true;
            StatusText = "Loading hypernetworks...";

            try
            {
                await Task.Delay(900);

                var samples = new List<HypernetworkItem>
                {
                    new() {
                        Name = "anime_style_v3",
                        Category = "Style",
                        Description = "Enhanced anime style with improved shading and line art",
                        FileSize = "156.8 MB",
                        Hash = "a1b2c3",
                        Architecture = "Standard",
                        BaseModel = "SD 1.5",
                        Quality = "High",
                        Author = "animaster",
                        LayerCount = 12,
                        ActivationFunction = "ReLU",
                        CreatedDate = DateTime.Now.AddDays(-15)
                    },
                    new() {
                        Name = "photorealism_enhanced",
                        Category = "Style",
                        Description = "Hypernetwork trained for photorealistic portrait enhancement",
                        FileSize = "243.2 MB",
                        Hash = "d4e5f6",
                        Architecture = "Deep",
                        BaseModel = "SD 1.5",
                        Quality = "Premium",
                        Author = "photodev",
                        LayerCount = 16,
                        ActivationFunction = "Swish",
                        CreatedDate = DateTime.Now.AddDays(-8)
                    },
                    new() {
                        Name = "cyberpunk_aesthetics",
                        Category = "Style",
                        Description = "Cyberpunk and neon-noir aesthetic enhancement",
                        FileSize = "189.5 MB",
                        Hash = "g7h8i9",
                        Architecture = "Standard",
                        BaseModel = "SD 1.5",
                        Quality = "High",
                        Author = "neonmaster",
                        LayerCount = 14,
                        ActivationFunction = "GELU",
                        CreatedDate = DateTime.Now.AddDays(-22)
                    },
                    new() {
                        Name = "character_consistency_v2",
                        Category = "Character",
                        Description = "Improves character consistency across multiple generations",
                        FileSize = "201.3 MB",
                        Hash = "j0k1l2",
                        Architecture = "Deep",
                        BaseModel = "SD 1.5",
                        Quality = "High",
                        Author = "consistency_ai",
                        LayerCount = 18,
                        ActivationFunction = "ReLU",
                        CreatedDate = DateTime.Now.AddDays(-5)
                    },
                    new() {
                        Name = "monet_style_transfer",
                        Category = "Artist",
                        Description = "Applies Claude Monet's impressionist style to any subject",
                        FileSize = "167.9 MB",
                        Hash = "m3n4o5",
                        Architecture = "Artistic",
                        BaseModel = "SD 1.5",
                        Quality = "Premium",
                        Author = "artnet_team",
                        LayerCount = 10,
                        ActivationFunction = "Tanh",
                        CreatedDate = DateTime.Now.AddDays(-12)
                    }
                };

                _allHypernetworks.Clear();
                foreach (var hypernetwork in samples)
                {
                    _allHypernetworks.Add(hypernetwork);
                }

                FilterHypernetworks();
                StatusText = $"Loaded {samples.Count} hypernetworks";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterHypernetworks()
        {
            var filtered = _allHypernetworks.AsEnumerable();

            if (SelectedCategory != "All")
            {
                filtered = filtered.Where(h => h.Category == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var search = SearchFilter.ToLowerInvariant();
                filtered = filtered.Where(h =>
                    h.Name.ToLowerInvariant().Contains(search) ||
                    h.Description.ToLowerInvariant().Contains(search) ||
                    h.Author.ToLowerInvariant().Contains(search));
            }

            AvailableHypernetworks.Clear();
            foreach (var hypernetwork in filtered)
            {
                AvailableHypernetworks.Add(hypernetwork);
            }

            OnPropertyChanged(nameof(TotalHypernetworksCount));
        }

        private async Task RefreshHypernetworks()
        {
            await LoadSampleData();
        }

        private void ActivateHypernetwork(HypernetworkItem? hypernetwork)
        {
            if (hypernetwork == null) return;

            if (ActiveHypernetworks.Any(h => h.Name == hypernetwork.Name)) return;

            var activeHypernetwork = new HypernetworkItem
            {
                Name = hypernetwork.Name,
                Category = hypernetwork.Category,
                Description = hypernetwork.Description,
                FileSize = hypernetwork.FileSize,
                Hash = hypernetwork.Hash,
                Architecture = hypernetwork.Architecture,
                BaseModel = hypernetwork.BaseModel,
                Quality = hypernetwork.Quality,
                Author = hypernetwork.Author,
                LayerCount = hypernetwork.LayerCount,
                ActivationFunction = hypernetwork.ActivationFunction,
                CreatedDate = hypernetwork.CreatedDate,
                IsActive = true,
                Strength = 1.0
            };

            ActiveHypernetworks.Add(activeHypernetwork);
            OnPropertyChanged(nameof(ActiveHypernetworksCount));
            OnPropertyChanged(nameof(HasActiveHypernetworks));

            StatusText = $"Activated: {hypernetwork.Name}";
        }

        private void DeactivateHypernetwork(HypernetworkItem? hypernetwork)
        {
            if (hypernetwork == null) return;

            var toRemove = ActiveHypernetworks.FirstOrDefault(h => h.Name == hypernetwork.Name);
            if (toRemove != null)
            {
                ActiveHypernetworks.Remove(toRemove);
                OnPropertyChanged(nameof(ActiveHypernetworksCount));
                OnPropertyChanged(nameof(HasActiveHypernetworks));
                StatusText = $"Deactivated: {hypernetwork.Name}";
            }
        }

        private void ClearAllHypernetworks()
        {
            ActiveHypernetworks.Clear();
            OnPropertyChanged(nameof(ActiveHypernetworksCount));
            OnPropertyChanged(nameof(HasActiveHypernetworks));
            StatusText = "Cleared all active hypernetworks";
        }

        private void AdjustStrength(HypernetworkItem? hypernetwork)
        {
            if (hypernetwork == null) return;
            StatusText = $"Adjusting strength for {hypernetwork.Name}";
        }

        private async Task TestHypernetwork(HypernetworkItem? hypernetwork)
        {
            if (hypernetwork == null) return;

            IsLoading = true;
            StatusText = $"Testing hypernetwork: {hypernetwork.Name}";

            try
            {
                await Task.Delay(2500);
                StatusText = $"Test completed: {hypernetwork.Name}";
            }
            finally
            {
                IsLoading = false;
            }
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