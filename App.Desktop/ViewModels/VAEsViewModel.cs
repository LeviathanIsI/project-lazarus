using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;

namespace Lazarus.Desktop.ViewModels
{
    public class VAEsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _statusText = "Ready to encode reality...";
        private bool _isLoading;
        private VAEModel? _selectedVAE;
        private VAEModel? _activeVAE;
        private string _searchFilter = string.Empty;
        private string _selectedCategory = "All";

        public ObservableCollection<VAEModel> AvailableVAEs { get; set; }
        public ObservableCollection<string> Categories { get; set; }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public VAEModel? SelectedVAE
        {
            get => _selectedVAE;
            set
            {
                _selectedVAE = value;
                OnPropertyChanged();
            }
        }

        public VAEModel? ActiveVAE
        {
            get => _activeVAE;
            set
            {
                _activeVAE = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveVAE));
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                OnPropertyChanged();
                FilterVAEs();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterVAEs();
            }
        }

        public bool HasActiveVAE => ActiveVAE != null;

        public int TotalVAEsCount => AvailableVAEs.Count;

        public ICommand LoadVAECommand { get; }
        public ICommand UnloadVAECommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand TestVAECommand { get; }

        private ObservableCollection<VAEModel> _allVAEs;

        public VAEsViewModel()
        {
            AvailableVAEs = new ObservableCollection<VAEModel>();
            _allVAEs = new ObservableCollection<VAEModel>();
            Categories = new ObservableCollection<string>
            {
                "All",
                "General",
                "Anime",
                "Realistic",
                "Artistic",
                "SDXL",
                "Custom"
            };

            LoadVAECommand = new ActionCommand<VAEModel>(LoadVAE);
            UnloadVAECommand = new ActionCommand(UnloadVAE);
            RefreshCommand = new ActionCommand(LoadVAEs);
            TestVAECommand = new ActionCommand<VAEModel>(TestVAE);

            LoadSampleVAEs();
        }

        private async void LoadVAEs()
        {
            IsLoading = true;
            StatusText = "Scanning the digital abyss for VAE models...";

            await Task.Run(async () =>
            {
                await Task.Delay(1200); // Simulate scanning the depths

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _allVAEs.Clear();
                    AvailableVAEs.Clear();
                    LoadSampleVAEs();

                    IsLoading = false;
                    StatusText = $"Discovered {TotalVAEsCount} VAE encoders in the void";
                });
            });
        }

        private void LoadSampleVAEs()
        {
            var sampleVAEs = new[]
            {
                new VAEModel
                {
                    Name = "vae-ft-mse-840000-ema-pruned",
                    Category = "General",
                    Description = "The classic encoder - reliable as death and twice as efficient at crushing pixels into latent nightmares",
                    FileSize = "334 MB",
                    Architecture = "VAE-MSE",
                    TrainingSteps = 840000,
                    Resolution = "512x512",
                    IsLoaded = false,
                    Quality = "High",
                    Compatibility = "SD 1.x/2.x",
                    Hash = "735e4c3a44"
                },
                new VAEModel
                {
                    Name = "kl-f8-anime2",
                    Category = "Anime",
                    Description = "Anime-tuned VAE that transforms waifu dreams into digital essence with supernatural precision",
                    FileSize = "334 MB",
                    Architecture = "KL-f8",
                    TrainingSteps = 600000,
                    Resolution = "512x512",
                    IsLoaded = true,
                    Quality = "Excellent",
                    Compatibility = "SD 1.x",
                    Hash = "df3c506e51"
                },
                new VAEModel
                {
                    Name = "sdxl_vae",
                    Category = "SDXL",
                    Description = "SDXL's native VAE - handles 1024px reality with the grace of a digital seraph",
                    FileSize = "334 MB",
                    Architecture = "SDXL-VAE",
                    TrainingSteps = 1000000,
                    Resolution = "1024x1024",
                    IsLoaded = false,
                    Quality = "Excellent",
                    Compatibility = "SDXL",
                    Hash = "235745af8d"
                },
                new VAEModel
                {
                    Name = "blessed2.vae",
                    Category = "Realistic",
                    Description = "Realistic VAE blessed by the pixel gods - transforms reality with unholy accuracy",
                    FileSize = "334 MB",
                    Architecture = "VAE-MSE",
                    TrainingSteps = 900000,
                    Resolution = "512x512",
                    IsLoaded = false,
                    Quality = "High",
                    Compatibility = "SD 1.x/2.x",
                    Hash = "6569e224f8"
                },
                new VAEModel
                {
                    Name = "orangemix.vae",
                    Category = "Artistic",
                    Description = "Artistic VAE that bleeds vibrant colors across the latent space like digital paint",
                    FileSize = "334 MB",
                    Architecture = "VAE-EMA",
                    TrainingSteps = 750000,
                    Resolution = "512x512",
                    IsLoaded = false,
                    Quality = "Good",
                    Compatibility = "SD 1.x",
                    Hash = "c6a580b13a"
                },
                new VAEModel
                {
                    Name = "anything-v4.vae",
                    Category = "Anime",
                    Description = "Anything V4's companion VAE - specializes in anime perfection with obsessive detail",
                    FileSize = "334 MB",
                    Architecture = "VAE-MSE",
                    TrainingSteps = 840000,
                    Resolution = "512x512",
                    IsLoaded = false,
                    Quality = "Excellent",
                    Compatibility = "SD 1.x",
                    Hash = "f458b5aa94"
                }
            };

            foreach (var vae in sampleVAEs)
            {
                _allVAEs.Add(vae);
                AvailableVAEs.Add(vae);
            }

            // Set active VAE to the loaded one
            ActiveVAE = _allVAEs.FirstOrDefault(v => v.IsLoaded);
        }

        private void LoadVAE(VAEModel? vae)
        {
            if (vae == null) return;

            // Unload current VAE first
            if (ActiveVAE != null)
            {
                ActiveVAE.IsLoaded = false;
            }

            // Load the new VAE
            vae.IsLoaded = true;
            ActiveVAE = vae;

            StatusText = $"VAE '{vae.Name}' now encodes your reality into {vae.Architecture} nightmares";
        }

        private void UnloadVAE()
        {
            if (ActiveVAE == null) return;

            ActiveVAE.IsLoaded = false;
            var vaeName = ActiveVAE.Name;
            ActiveVAE = null;

            StatusText = $"Unloaded '{vaeName}' - returning to the default encoder's cold embrace";
        }

        private void TestVAE(VAEModel? vae)
        {
            if (vae == null) return;

            StatusText = $"Testing '{vae.Name}' encoding quality... reality bends to its will";

            // Simulate test
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusText = $"'{vae.Name}' test complete - {vae.Quality} quality encoding confirmed";
                });
            });
        }

        private void FilterVAEs()
        {
            AvailableVAEs.Clear();

            var filtered = _allVAEs.Where(vae =>
            {
                bool matchesCategory = SelectedCategory == "All" || vae.Category == SelectedCategory;
                bool matchesSearch = string.IsNullOrEmpty(SearchFilter) ||
                                   vae.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                   vae.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase);

                return matchesCategory && matchesSearch;
            });

            foreach (var vae in filtered)
            {
                AvailableVAEs.Add(vae);
            }

            OnPropertyChanged(nameof(TotalVAEsCount));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class VAEModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isLoaded;

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public int TrainingSteps { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public string Compatibility { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;

        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }

        public string FormattedTrainingSteps => TrainingSteps.ToString("N0") + " steps";

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}