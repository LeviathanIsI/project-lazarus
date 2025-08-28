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
    public class EmbeddingItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _trigger = string.Empty;
        private string _category = string.Empty;
        private string _description = string.Empty;
        private string _fileSize = string.Empty;
        private string _hash = string.Empty;
        private int _dimensions;
        private string _baseModel = string.Empty;
        private string _quality = string.Empty;
        private bool _isActive;
        private double _strength = 1.0;
        private string _author = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Trigger
        {
            get => _trigger;
            set => SetProperty(ref _trigger, value);
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

        public int Dimensions
        {
            get => _dimensions;
            set => SetProperty(ref _dimensions, value);
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

        public string FormattedDimensions => $"{Dimensions}D";
        public string TriggerDisplay => string.IsNullOrEmpty(Trigger) ? "No trigger" : $"<{Trigger}>";
        public string StrengthDisplay => $"{Strength:F1}x";

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

    public class EmbeddingsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<EmbeddingItem> _availableEmbeddings = new();
        private ObservableCollection<EmbeddingItem> _activeEmbeddings = new();
        private ObservableCollection<string> _categories = new();
        private string _searchFilter = string.Empty;
        private string _selectedCategory = "All";
        private bool _isLoading;
        private string _statusText = "Ready";

        public ObservableCollection<EmbeddingItem> AvailableEmbeddings
        {
            get => _availableEmbeddings;
            set => SetProperty(ref _availableEmbeddings, value);
        }

        public ObservableCollection<EmbeddingItem> ActiveEmbeddings
        {
            get => _activeEmbeddings;
            set => SetProperty(ref _activeEmbeddings, value);
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
                FilterEmbeddings();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                FilterEmbeddings();
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

        public int TotalEmbeddingsCount => AvailableEmbeddings.Count;
        public int ActiveEmbeddingsCount => ActiveEmbeddings.Count;
        public bool HasActiveEmbeddings => ActiveEmbeddings.Count > 0;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ActivateEmbeddingCommand { get; }
        public ICommand DeactivateEmbeddingCommand { get; }
        public ICommand TestEmbeddingCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand AdjustStrengthCommand { get; }

        private readonly ObservableCollection<EmbeddingItem> _allEmbeddings = new();

        public EmbeddingsViewModel()
        {
            RefreshCommand = new Helpers.RelayCommand(async _ => await RefreshEmbeddings());
            ActivateEmbeddingCommand = new Helpers.RelayCommand(param => ActivateEmbedding(param as EmbeddingItem));
            DeactivateEmbeddingCommand = new Helpers.RelayCommand(param => DeactivateEmbedding(param as EmbeddingItem));
            TestEmbeddingCommand = new Helpers.RelayCommand(async param => await TestEmbedding(param as EmbeddingItem));
            ClearAllCommand = new Helpers.RelayCommand(_ => ClearAllEmbeddings());
            AdjustStrengthCommand = new Helpers.RelayCommand(param => AdjustStrength(param as EmbeddingItem));

            InitializeCategories();
            _ = LoadSampleDataAsync();
        }

        private void InitializeCategories()
        {
            Categories.Clear();
            Categories.Add("All");
            Categories.Add("Character");
            Categories.Add("Style");
            Categories.Add("Object");
            Categories.Add("Concept");
            Categories.Add("Negative");
            Categories.Add("Artist");
        }

        private async Task LoadSampleDataAsync()
        {
            // UI updates must happen on UI thread
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                StatusText = "Loading embeddings...";
            });
            
            // Background work
            await LoadSampleData();
        }
        
        private async Task LoadSampleData()
        {

            try
            {
                await Task.Delay(800);

                var samples = new List<EmbeddingItem>
                {
                    new() {
                        Name = "badhandv4",
                        Trigger = "badhandv4",
                        Category = "Negative",
                        Description = "Fixes hand anatomy issues and deformities",
                        FileSize = "25.6 KB",
                        Hash = "5e40d1",
                        Dimensions = 768,
                        BaseModel = "SD 1.5",
                        Quality = "High",
                        Author = "yesyeahvh"
                    },
                    new() {
                        Name = "easynegative",
                        Trigger = "easynegative",
                        Category = "Negative",
                        Description = "General negative embedding for quality improvement",
                        FileSize = "25.6 KB",
                        Hash = "c74b4e",
                        Dimensions = 768,
                        BaseModel = "SD 1.5",
                        Quality = "Medium",
                        Author = "gsdf"
                    },
                    new() {
                        Name = "ng_deepnegative_v1_75t",
                        Trigger = "ng_deepnegative_v1_75t",
                        Category = "Negative",
                        Description = "Deep negative embedding trained on anatomical issues",
                        FileSize = "49.2 KB",
                        Hash = "54e7e4",
                        Dimensions = 768,
                        BaseModel = "SD 1.5",
                        Quality = "Premium",
                        Author = "nolanaatama"
                    },
                    new() {
                        Name = "charturnerv2",
                        Trigger = "charturnerv2",
                        Category = "Character",
                        Description = "Character consistency embedding for anime-style faces",
                        FileSize = "92.8 KB",
                        Hash = "a8b2c3",
                        Dimensions = 768,
                        BaseModel = "SD 1.5",
                        Quality = "High",
                        Author = "charadev"
                    },
                    new() {
                        Name = "verybadimagenegative",
                        Trigger = "verybadimagenegative",
                        Category = "Negative",
                        Description = "Comprehensive negative embedding for image artifacts",
                        FileSize = "38.1 KB",
                        Hash = "d4f6a9",
                        Dimensions = 768,
                        BaseModel = "SD 1.5",
                        Quality = "High",
                        Author = "supernegative"
                    }
                };

                _allEmbeddings.Clear();
                foreach (var embedding in samples)
                {
                    _allEmbeddings.Add(embedding);
                }

                            // Update UI on UI thread safely
            await UpdateUIAsync(() =>
            {
                FilterEmbeddings();
                StatusText = $"Loaded {samples.Count} embeddings";
            });
            }
            finally
            {
                // Update UI on UI thread safely
                await UpdateUIAsync(() => IsLoading = false);
            }
        }

        private void FilterEmbeddings()
        {
            var filtered = _allEmbeddings.AsEnumerable();

            if (SelectedCategory != "All")
            {
                filtered = filtered.Where(e => e.Category == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var search = SearchFilter.ToLowerInvariant();
                filtered = filtered.Where(e =>
                    e.Name.ToLowerInvariant().Contains(search) ||
                    e.Description.ToLowerInvariant().Contains(search) ||
                    e.Trigger.ToLowerInvariant().Contains(search));
            }

            AvailableEmbeddings.Clear();
            foreach (var embedding in filtered)
            {
                AvailableEmbeddings.Add(embedding);
            }
            OnPropertyChanged(nameof(TotalEmbeddingsCount));
        }

        private async Task RefreshEmbeddings()
        {
            await LoadSampleData();
        }

        private void ActivateEmbedding(EmbeddingItem? embedding)
        {
            if (embedding == null) return;

            if (ActiveEmbeddings.Any(e => e.Name == embedding.Name)) return;

            var activeEmbedding = new EmbeddingItem
            {
                Name = embedding.Name,
                Trigger = embedding.Trigger,
                Category = embedding.Category,
                Description = embedding.Description,
                FileSize = embedding.FileSize,
                Hash = embedding.Hash,
                Dimensions = embedding.Dimensions,
                BaseModel = embedding.BaseModel,
                Quality = embedding.Quality,
                Author = embedding.Author,
                IsActive = true,
                Strength = 1.0
            };

            ActiveEmbeddings.Add(activeEmbedding);
            OnPropertyChanged(nameof(ActiveEmbeddingsCount));
            OnPropertyChanged(nameof(HasActiveEmbeddings));

            StatusText = $"Activated: {embedding.Name}";
        }

        private void DeactivateEmbedding(EmbeddingItem? embedding)
        {
            if (embedding == null) return;

            var toRemove = ActiveEmbeddings.FirstOrDefault(e => e.Name == embedding.Name);
            if (toRemove != null)
            {
                ActiveEmbeddings.Remove(toRemove);
                OnPropertyChanged(nameof(ActiveEmbeddingsCount));
                OnPropertyChanged(nameof(HasActiveEmbeddings));
                StatusText = $"Deactivated: {embedding.Name}";
            }
        }

        private void ClearAllEmbeddings()
        {
            ActiveEmbeddings.Clear();
            OnPropertyChanged(nameof(ActiveEmbeddingsCount));
            OnPropertyChanged(nameof(HasActiveEmbeddings));
            StatusText = "Cleared all active embeddings";
        }

        private void AdjustStrength(EmbeddingItem? embedding)
        {
            if (embedding == null) return;
            // This would open a dialog or inline editor
            StatusText = $"Adjusting strength for {embedding.Name}";
        }

        private async Task TestEmbedding(EmbeddingItem? embedding)
        {
            if (embedding == null) return;

            // Update UI on UI thread safely
            await UpdateUIAsync(() =>
            {
                IsLoading = true;
                StatusText = $"Testing embedding: {embedding.Name}";
            });

            try
            {
                await Task.Delay(2000); // Simulate test
                
                // Update UI on UI thread safely
                await UpdateUIAsync(() => StatusText = $"Test completed: {embedding.Name}");
            }
            finally
            {
                // Update UI on UI thread safely
                await UpdateUIAsync(() => IsLoading = false);
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

        #region UI Thread Safety

        /// <summary>
        /// Safely update UI properties on the UI thread
        /// </summary>
        private async Task UpdateUIAsync(Action uiAction)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app?.Dispatcher == null) return;

                if (app.Dispatcher.CheckAccess())
                {
                    uiAction();
                }
                else
                {
                    await app.Dispatcher.InvokeAsync(uiAction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmbeddingsViewModel] UI update failed: {ex.Message}");
            }
        }

        #endregion
    }


}