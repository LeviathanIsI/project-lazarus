using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.ThreeDModels
{
    public class ThreeDModelsViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private string _searchQuery = "";
        private string _selectedCategory = "All";
        private string _sortOrder = "Name";
        private Model3D? _selectedModel;
        private bool _hasSelectedModel;
        private bool _isLoading;
        private string _loadingStatus = "";
        private bool _showWireframe;
        private bool _showNormals;
        private bool _showTextures = true;
        private double _viewportZoom = 1.0;
        private double _viewportRotationX = 0;
        private double _viewportRotationY = 0;
        private double _viewportRotationZ = 0;
        private string _selectedMaterial = "";
        private bool _isAnimationPlaying;
        private double _animationProgress = 0;
        private double _animationDuration = 100;
        private string _selectedLodLevel = "High";
        private bool _enableLighting = true;
        private string _lightingPreset = "Studio";
        private double _ambientLight = 0.3;
        private double _directionalLight = 0.7;
        private string _backgroundType = "Gradient";
        private int _polyCount = 0;
        private int _vertexCount = 0;
        private int _textureCount = 0;
        private long _modelFileSize = 0;
        private string _exportFormat = "OBJ";
        private string _exportQuality = "High";
        private CancellationTokenSource? _cancellationTokenSource;
        #endregion

        #region Properties
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SortOrder
        {
            get => _sortOrder;
            set
            {
                if (SetProperty(ref _sortOrder, value))
                {
                    ApplyFilters();
                }
            }
        }

        public Model3D? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value))
                {
                    HasSelectedModel = value != null;
                    OnPropertyChanged(nameof(CanLoadModel));
                    OnPropertyChanged(nameof(CanExportModel));
                    OnPropertyChanged(nameof(CanEditMaterial));
                    OnPropertyChanged(nameof(ModelInfoText));
                    if (value != null)
                    {
                        LoadModelDetails(value);
                    }
                }
            }
        }

        public bool HasSelectedModel
        {
            get => _hasSelectedModel;
            set => SetProperty(ref _hasSelectedModel, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanLoadModel));
                    OnPropertyChanged(nameof(CanImportModel));
                }
            }
        }

        public string LoadingStatus
        {
            get => _loadingStatus;
            set => SetProperty(ref _loadingStatus, value);
        }

        public bool ShowWireframe
        {
            get => _showWireframe;
            set => SetProperty(ref _showWireframe, value);
        }

        public bool ShowNormals
        {
            get => _showNormals;
            set => SetProperty(ref _showNormals, value);
        }

        public bool ShowTextures
        {
            get => _showTextures;
            set => SetProperty(ref _showTextures, value);
        }

        public double ViewportZoom
        {
            get => _viewportZoom;
            set => SetProperty(ref _viewportZoom, value);
        }

        public double ViewportRotationX
        {
            get => _viewportRotationX;
            set => SetProperty(ref _viewportRotationX, value);
        }

        public double ViewportRotationY
        {
            get => _viewportRotationY;
            set => SetProperty(ref _viewportRotationY, value);
        }

        public double ViewportRotationZ
        {
            get => _viewportRotationZ;
            set => SetProperty(ref _viewportRotationZ, value);
        }

        public string SelectedMaterial
        {
            get => _selectedMaterial;
            set => SetProperty(ref _selectedMaterial, value);
        }

        public bool IsAnimationPlaying
        {
            get => _isAnimationPlaying;
            set => SetProperty(ref _isAnimationPlaying, value);
        }

        public double AnimationProgress
        {
            get => _animationProgress;
            set => SetProperty(ref _animationProgress, value);
        }

        public double AnimationDuration
        {
            get => _animationDuration;
            set => SetProperty(ref _animationDuration, value);
        }

        public string SelectedLodLevel
        {
            get => _selectedLodLevel;
            set => SetProperty(ref _selectedLodLevel, value);
        }

        public bool EnableLighting
        {
            get => _enableLighting;
            set => SetProperty(ref _enableLighting, value);
        }

        public string LightingPreset
        {
            get => _lightingPreset;
            set => SetProperty(ref _lightingPreset, value);
        }

        public double AmbientLight
        {
            get => _ambientLight;
            set => SetProperty(ref _ambientLight, value);
        }

        public double DirectionalLight
        {
            get => _directionalLight;
            set => SetProperty(ref _directionalLight, value);
        }

        public string BackgroundType
        {
            get => _backgroundType;
            set => SetProperty(ref _backgroundType, value);
        }

        public int PolyCount
        {
            get => _polyCount;
            set => SetProperty(ref _polyCount, value);
        }

        public int VertexCount
        {
            get => _vertexCount;
            set => SetProperty(ref _vertexCount, value);
        }

        public int TextureCount
        {
            get => _textureCount;
            set => SetProperty(ref _textureCount, value);
        }

        public long ModelFileSize
        {
            get => _modelFileSize;
            set => SetProperty(ref _modelFileSize, value);
        }

        public string ExportFormat
        {
            get => _exportFormat;
            set => SetProperty(ref _exportFormat, value);
        }

        public string ExportQuality
        {
            get => _exportQuality;
            set => SetProperty(ref _exportQuality, value);
        }

        // Computed Properties
        public bool CanLoadModel => !IsLoading && HasSelectedModel;
        public bool CanImportModel => !IsLoading;
        public bool CanExportModel => !IsLoading && HasSelectedModel;
        public bool CanEditMaterial => HasSelectedModel && !IsLoading;
        public bool CanPlayAnimation => HasSelectedModel && SelectedModel?.HasAnimations == true;
        public string ModelInfoText => HasSelectedModel && SelectedModel != null 
            ? $"{SelectedModel.Name} • {PolyCountText} • {FileSizeText}"
            : "No model selected";
        public string PolyCountText => PolyCount > 1000 
            ? $"{PolyCount / 1000:F1}K polys" 
            : $"{PolyCount} polys";
        public string FileSizeText => ModelFileSize < 1024 * 1024 
            ? $"{ModelFileSize / 1024:F0} KB" 
            : $"{ModelFileSize / 1024.0 / 1024.0:F1} MB";
        public string AnimationTimeText => TimeSpan.FromSeconds(AnimationProgress).ToString(@"mm\:ss");
        public string AnimationDurationText => TimeSpan.FromSeconds(AnimationDuration).ToString(@"mm\:ss");
        #endregion

        #region Collections
        public ObservableCollection<Model3D> AllModels { get; } = new();
        public ObservableCollection<Model3D> FilteredModels { get; } = new();
        public ObservableCollection<Model3D> FavoriteModels { get; } = new();
        public ObservableCollection<ModelImportHistoryItem> ImportHistory { get; } = new();
        public ObservableCollection<Material3D> AvailableMaterials { get; } = new();
        public ObservableCollection<Animation3D> ModelAnimations { get; } = new();

        public List<string> ModelCategories { get; } = new()
        {
            "All", "Characters", "Objects", "Environments", "Vehicles", "Weapons", "Architecture", "Nature"
        };

        public List<string> SortOptions { get; } = new()
        {
            "Name", "Date Added", "File Size", "Poly Count", "Category"
        };

        public List<string> SupportedFormats { get; } = new()
        {
            "OBJ", "FBX", "GLB", "GLTF", "DAE", "3DS", "PLY", "STL"
        };

        public List<string> ExportFormats { get; } = new()
        {
            "OBJ", "FBX", "GLB", "GLTF", "STL", "PLY"
        };

        public List<string> ExportQualities { get; } = new()
        {
            "Low", "Medium", "High", "Ultra"
        };

        public List<string> LodLevels { get; } = new()
        {
            "High", "Medium", "Low", "Automatic"
        };

        public List<string> LightingPresets { get; } = new()
        {
            "Studio", "Outdoor", "Indoor", "Dramatic", "Soft", "Custom"
        };

        public List<string> BackgroundTypes { get; } = new()
        {
            "Gradient", "Solid Color", "HDRI", "Transparent"
        };
        #endregion

        #region Commands
        public ICommand LoadModelCommand { get; }
        public ICommand ImportModelCommand { get; }
        public ICommand ImportMultipleModelsCommand { get; }
        public ICommand ExportModelCommand { get; }
        public ICommand DeleteModelCommand { get; }
        public ICommand DuplicateModelCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand RemoveFromFavoritesCommand { get; }
        public ICommand ResetViewportCommand { get; }
        public ICommand FitToViewCommand { get; }
        public ICommand PlayAnimationCommand { get; }
        public ICommand PauseAnimationCommand { get; }
        public ICommand StopAnimationCommand { get; }
        public ICommand SetCategoryCommand { get; }
        public ICommand SetSortOrderCommand { get; }
        public ICommand CreateMaterialCommand { get; }
        public ICommand EditMaterialCommand { get; }
        public ICommand AssignMaterialCommand { get; }
        public ICommand OptimizeModelCommand { get; }
        public ICommand ValidateModelCommand { get; }
        public ICommand BatchConvertCommand { get; }
        public ICommand RefreshModelsCommand { get; }
        #endregion

        public ThreeDModelsViewModel()
        {
            // Initialize commands
            LoadModelCommand = new SimpleRelayCommand(async () => await LoadModelAsync(), () => CanLoadModel);
            ImportModelCommand = new SimpleRelayCommand(async () => await ImportModelAsync(), () => CanImportModel);
            ImportMultipleModelsCommand = new SimpleRelayCommand(async () => await ImportMultipleModelsAsync(), () => CanImportModel);
            ExportModelCommand = new SimpleRelayCommand(async () => await ExportModelAsync(), () => CanExportModel);
            DeleteModelCommand = new SimpleRelayCommand(DeleteModel, () => HasSelectedModel);
            DuplicateModelCommand = new SimpleRelayCommand(DuplicateModel, () => HasSelectedModel);
            AddToFavoritesCommand = new SimpleRelayCommand(AddToFavorites, () => HasSelectedModel);
            RemoveFromFavoritesCommand = new SimpleRelayCommand<Model3D>(RemoveFromFavorites);
            ResetViewportCommand = new SimpleRelayCommand(ResetViewport);
            FitToViewCommand = new SimpleRelayCommand(FitToView, () => HasSelectedModel);
            PlayAnimationCommand = new SimpleRelayCommand(PlayAnimation, () => CanPlayAnimation);
            PauseAnimationCommand = new SimpleRelayCommand(PauseAnimation);
            StopAnimationCommand = new SimpleRelayCommand(StopAnimation);
            SetCategoryCommand = new SimpleRelayCommand<string>(SetCategory);
            SetSortOrderCommand = new SimpleRelayCommand<string>(SetSortOrder);
            CreateMaterialCommand = new SimpleRelayCommand(CreateMaterial);
            EditMaterialCommand = new SimpleRelayCommand<Material3D>(EditMaterial);
            AssignMaterialCommand = new SimpleRelayCommand<Material3D>(AssignMaterial);
            OptimizeModelCommand = new SimpleRelayCommand(async () => await OptimizeModelAsync(), () => HasSelectedModel);
            ValidateModelCommand = new SimpleRelayCommand(async () => await ValidateModelAsync(), () => HasSelectedModel);
            BatchConvertCommand = new SimpleRelayCommand(async () => await BatchConvertAsync());
            RefreshModelsCommand = new SimpleRelayCommand(async () => await RefreshModelsAsync());

            // Initialize sample data
            InitializeSampleData();
            ApplyFilters();
        }

        #region Command Implementations
        private async Task LoadModelAsync()
        {
            if (SelectedModel == null) return;

            try
            {
                IsLoading = true;
                LoadingStatus = $"Loading {SelectedModel.Name}...";

                // Simulate model loading with progress
                for (int i = 0; i <= 100; i += 20)
                {
                    LoadingStatus = $"Loading model... {i}%";
                    await Task.Delay(100);
                }

                LoadingStatus = "Model loaded successfully!";
                
                // Reset viewport to show model
                ResetViewport();
                FitToView();
                
                await Task.Delay(1000);
                LoadingStatus = "";
            }
            catch (Exception ex)
            {
                LoadingStatus = $"Error loading model: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ImportModelAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "3D Model files|*.obj;*.fbx;*.glb;*.gltf;*.dae;*.3ds;*.ply;*.stl|All files|*.*",
                Title = "Import 3D Model"
            };

            if (openDialog.ShowDialog() == true)
            {
                await ImportModelFromPath(openDialog.FileName);
            }
        }

        private async Task ImportMultipleModelsAsync()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "3D Model files|*.obj;*.fbx;*.glb;*.gltf;*.dae;*.3ds;*.ply;*.stl",
                Title = "Import Multiple 3D Models",
                Multiselect = true
            };

            if (openDialog.ShowDialog() == true)
            {
                foreach (var file in openDialog.FileNames)
                {
                    await ImportModelFromPath(file);
                }
            }
        }

        private async Task ImportModelFromPath(string filePath)
        {
            try
            {
                IsLoading = true;
                LoadingStatus = $"Importing {Path.GetFileName(filePath)}...";

                // Simulate import process
                await Task.Delay(500);

                var model = new Model3D
                {
                    Id = Guid.NewGuid(),
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    Category = DetectModelCategory(filePath),
                    Format = Path.GetExtension(filePath).TrimStart('.').ToUpper(),
                    DateAdded = DateTime.Now,
                    FileSize = new FileInfo(filePath).Length,
                    HasAnimations = Path.GetExtension(filePath).ToLower() == ".fbx"
                };

                AllModels.Add(model);
                
                // Add to import history
                ImportHistory.Insert(0, new ModelImportHistoryItem
                {
                    FileName = model.Name,
                    FilePath = filePath,
                    Format = model.Format,
                    ImportedAt = DateTime.Now,
                    FileSize = model.FileSize
                });

                ApplyFilters();
                LoadingStatus = $"Imported {model.Name} successfully!";
                await Task.Delay(1000);
                LoadingStatus = "";
            }
            catch (Exception ex)
            {
                LoadingStatus = $"Import error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportModelAsync()
        {
            if (SelectedModel == null) return;

            var saveDialog = new SaveFileDialog
            {
                Filter = $"{ExportFormat} files|*.{ExportFormat.ToLower()}|All files|*.*",
                DefaultExt = $".{ExportFormat.ToLower()}",
                FileName = $"{SelectedModel.Name}_exported"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    LoadingStatus = $"Exporting to {ExportFormat}...";

                    // Simulate export process
                    for (int i = 0; i <= 100; i += 25)
                    {
                        LoadingStatus = $"Exporting... {i}%";
                        await Task.Delay(200);
                    }

                    LoadingStatus = $"Exported to {Path.GetFileName(saveDialog.FileName)}";
                    await Task.Delay(1000);
                    LoadingStatus = "";
                }
                catch (Exception ex)
                {
                    LoadingStatus = $"Export error: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void DeleteModel()
        {
            if (SelectedModel != null && AllModels.Contains(SelectedModel))
            {
                AllModels.Remove(SelectedModel);
                FavoriteModels.Remove(SelectedModel);
                ApplyFilters();
                LoadingStatus = "Model deleted";
            }
        }

        private void DuplicateModel()
        {
            if (SelectedModel == null) return;

            var duplicate = new Model3D
            {
                Id = Guid.NewGuid(),
                Name = $"{SelectedModel.Name}_copy",
                FilePath = SelectedModel.FilePath,
                Category = SelectedModel.Category,
                Format = SelectedModel.Format,
                DateAdded = DateTime.Now,
                FileSize = SelectedModel.FileSize,
                HasAnimations = SelectedModel.HasAnimations,
                PolyCount = SelectedModel.PolyCount,
                VertexCount = SelectedModel.VertexCount
            };

            AllModels.Add(duplicate);
            ApplyFilters();
            LoadingStatus = "Model duplicated";
        }

        private void AddToFavorites()
        {
            if (SelectedModel != null && !FavoriteModels.Contains(SelectedModel))
            {
                FavoriteModels.Add(SelectedModel);
                SelectedModel.IsFavorite = true;
                LoadingStatus = "Added to favorites";
            }
        }

        private void RemoveFromFavorites(Model3D? model)
        {
            if (model != null && FavoriteModels.Contains(model))
            {
                FavoriteModels.Remove(model);
                model.IsFavorite = false;
                LoadingStatus = "Removed from favorites";
            }
        }

        private void ResetViewport()
        {
            ViewportZoom = 1.0;
            ViewportRotationX = 0;
            ViewportRotationY = 0;
            ViewportRotationZ = 0;
            LoadingStatus = "Viewport reset";
        }

        private void FitToView()
        {
            // TODO: Calculate optimal zoom and rotation for model
            ViewportZoom = 0.8;
            LoadingStatus = "Fitted to view";
        }

        private void PlayAnimation()
        {
            IsAnimationPlaying = true;
            LoadingStatus = "Playing animation";
            // TODO: Start animation playback
        }

        private void PauseAnimation()
        {
            IsAnimationPlaying = false;
            LoadingStatus = "Animation paused";
        }

        private void StopAnimation()
        {
            IsAnimationPlaying = false;
            AnimationProgress = 0;
            LoadingStatus = "Animation stopped";
        }

        private void SetCategory(string? category)
        {
            if (!string.IsNullOrEmpty(category))
            {
                SelectedCategory = category;
            }
        }

        private void SetSortOrder(string? sortOrder)
        {
            if (!string.IsNullOrEmpty(sortOrder))
            {
                SortOrder = sortOrder;
            }
        }

        private void CreateMaterial()
        {
            var material = new Material3D
            {
                Id = Guid.NewGuid(),
                Name = $"Material_{DateTime.Now:HHmmss}",
                Type = "Standard",
                DiffuseColor = "#FFFFFF",
                Metallic = 0.0,
                Roughness = 0.5,
                CreatedAt = DateTime.Now
            };

            AvailableMaterials.Add(material);
            LoadingStatus = "Material created";
        }

        private void EditMaterial(Material3D? material)
        {
            if (material != null)
            {
                SelectedMaterial = material.Name;
                LoadingStatus = $"Editing material: {material.Name}";
            }
        }

        private void AssignMaterial(Material3D? material)
        {
            if (material != null && SelectedModel != null)
            {
                SelectedModel.AssignedMaterial = material.Name;
                LoadingStatus = $"Assigned material: {material.Name}";
            }
        }

        private async Task OptimizeModelAsync()
        {
            if (SelectedModel == null) return;

            try
            {
                IsLoading = true;
                LoadingStatus = "Optimizing model...";

                // Simulate optimization process
                for (int i = 0; i <= 100; i += 10)
                {
                    LoadingStatus = $"Optimizing... {i}%";
                    await Task.Delay(150);
                }

                // Simulate polygon reduction
                var originalPolyCount = SelectedModel.PolyCount;
                SelectedModel.PolyCount = (int)(originalPolyCount * 0.7); // 30% reduction
                PolyCount = SelectedModel.PolyCount;

                LoadingStatus = $"Optimized: {originalPolyCount - SelectedModel.PolyCount} polygons reduced";
                await Task.Delay(1500);
                LoadingStatus = "";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ValidateModelAsync()
        {
            if (SelectedModel == null) return;

            try
            {
                IsLoading = true;
                LoadingStatus = "Validating model...";

                await Task.Delay(1000);

                // Simulate validation results
                var issues = new List<string>();
                if (SelectedModel.PolyCount > 100000) issues.Add("High polygon count");
                if (SelectedModel.TextureCount > 10) issues.Add("Many textures");
                
                LoadingStatus = issues.Count == 0 
                    ? "Model validation passed!" 
                    : $"Validation issues: {string.Join(", ", issues)}";
                
                await Task.Delay(2000);
                LoadingStatus = "";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BatchConvertAsync()
        {
            LoadingStatus = "Starting batch conversion...";
            // TODO: Implement batch conversion logic
            await Task.Delay(1000);
            LoadingStatus = "Batch conversion completed";
        }

        private async Task RefreshModelsAsync()
        {
            try
            {
                IsLoading = true;
                LoadingStatus = "Refreshing model library...";

                await Task.Delay(500);
                ApplyFilters();
                LoadingStatus = "Model library refreshed";
                await Task.Delay(1000);
                LoadingStatus = "";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadModelDetails(Model3D model)
        {
            // Load detailed model information
            PolyCount = model.PolyCount;
            VertexCount = model.VertexCount;
            TextureCount = model.TextureCount;
            ModelFileSize = model.FileSize;

            // Load animations if available
            ModelAnimations.Clear();
            if (model.HasAnimations)
            {
                // Add sample animations
                ModelAnimations.Add(new Animation3D { Name = "Idle", Duration = 5.0, IsLooped = true });
                ModelAnimations.Add(new Animation3D { Name = "Walk", Duration = 2.0, IsLooped = true });
                AnimationDuration = ModelAnimations.FirstOrDefault()?.Duration ?? 0;
            }

            LoadingStatus = $"Loaded details for {model.Name}";
        }

        private string DetectModelCategory(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            
            if (fileName.Contains("character") || fileName.Contains("person") || fileName.Contains("human"))
                return "Characters";
            if (fileName.Contains("building") || fileName.Contains("house") || fileName.Contains("structure"))
                return "Architecture";
            if (fileName.Contains("car") || fileName.Contains("vehicle") || fileName.Contains("truck"))
                return "Vehicles";
            if (fileName.Contains("tree") || fileName.Contains("plant") || fileName.Contains("rock"))
                return "Nature";
            if (fileName.Contains("weapon") || fileName.Contains("sword") || fileName.Contains("gun"))
                return "Weapons";
            if (fileName.Contains("environment") || fileName.Contains("scene") || fileName.Contains("level"))
                return "Environments";
            
            return "Objects";
        }

        private void ApplyFilters()
        {
            FilteredModels.Clear();
            
            var query = AllModels.AsEnumerable();

            // Apply category filter
            if (SelectedCategory != "All")
            {
                query = query.Where(m => m.Category == SelectedCategory);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(m => m.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                        m.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            query = SortOrder switch
            {
                "Name" => query.OrderBy(m => m.Name),
                "Date Added" => query.OrderByDescending(m => m.DateAdded),
                "File Size" => query.OrderByDescending(m => m.FileSize),
                "Poly Count" => query.OrderByDescending(m => m.PolyCount),
                "Category" => query.OrderBy(m => m.Category).ThenBy(m => m.Name),
                _ => query.OrderBy(m => m.Name)
            };

            foreach (var model in query)
            {
                FilteredModels.Add(model);
            }
        }

        private void InitializeSampleData()
        {
            // Add sample 3D models
            AllModels.Add(new Model3D
            {
                Id = Guid.NewGuid(),
                Name = "Medieval Knight",
                Category = "Characters",
                Format = "FBX",
                DateAdded = DateTime.Now.AddDays(-5),
                FileSize = 15 * 1024 * 1024,
                PolyCount = 12500,
                VertexCount = 8200,
                TextureCount = 4,
                HasAnimations = true,
                AssignedMaterial = "Knight Armor"
            });

            AllModels.Add(new Model3D
            {
                Id = Guid.NewGuid(),
                Name = "Sci-Fi Rifle",
                Category = "Weapons",
                Format = "OBJ",
                DateAdded = DateTime.Now.AddDays(-2),
                FileSize = 8 * 1024 * 1024,
                PolyCount = 5600,
                VertexCount = 4200,
                TextureCount = 3,
                HasAnimations = false,
                AssignedMaterial = "Metal"
            });

            AllModels.Add(new Model3D
            {
                Id = Guid.NewGuid(),
                Name = "Forest Environment",
                Category = "Environments",
                Format = "GLB",
                DateAdded = DateTime.Now.AddDays(-1),
                FileSize = 45 * 1024 * 1024,
                PolyCount = 85000,
                VertexCount = 65000,
                TextureCount = 12,
                HasAnimations = false,
                AssignedMaterial = "Nature Pack"
            });

            // Add sample materials
            AvailableMaterials.Add(new Material3D
            {
                Name = "Knight Armor",
                Type = "PBR",
                DiffuseColor = "#8C8C8C",
                Metallic = 0.9,
                Roughness = 0.3
            });

            AvailableMaterials.Add(new Material3D
            {
                Name = "Metal",
                Type = "Standard",
                DiffuseColor = "#B8B8B8",
                Metallic = 1.0,
                Roughness = 0.1
            });
        }
        #endregion

        #region INotifyPropertyChanged
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
        #endregion
    }

    #region Data Models
    public class Model3D
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Category { get; set; } = "";
        public string Format { get; set; } = "";
        public DateTime DateAdded { get; set; }
        public long FileSize { get; set; }
        public int PolyCount { get; set; }
        public int VertexCount { get; set; }
        public int TextureCount { get; set; }
        public bool HasAnimations { get; set; }
        public bool IsFavorite { get; set; }
        public string AssignedMaterial { get; set; } = "";
        public BitmapSource? Thumbnail { get; set; }
        
        // Display Properties
        public string FileSizeText => FileSize < 1024 * 1024 
            ? $"{FileSize / 1024:F0} KB" 
            : $"{FileSize / 1024.0 / 1024.0:F1} MB";
        public string PolyCountText => PolyCount > 1000 
            ? $"{PolyCount / 1000:F1}K" 
            : $"{PolyCount}";
        public string DateAddedText => DateAdded.ToString("MM/dd/yyyy");
        public string ModelInfo => $"{Format} • {PolyCountText} polys • {FileSizeText}";
        public string AnimationStatus => HasAnimations ? "Animated" : "Static";
    }

    public class Material3D
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Standard";
        public string DiffuseColor { get; set; } = "#FFFFFF";
        public double Metallic { get; set; } = 0.0;
        public double Roughness { get; set; } = 0.5;
        public double Emission { get; set; } = 0.0;
        public string DiffuseTexture { get; set; } = "";
        public string NormalTexture { get; set; } = "";
        public string MetallicTexture { get; set; } = "";
        public string RoughnessTexture { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string MaterialInfo => $"{Type} • Metallic: {Metallic:F1} • Roughness: {Roughness:F1}";
    }

    public class Animation3D
    {
        public string Name { get; set; } = "";
        public double Duration { get; set; }
        public bool IsLooped { get; set; }
        public int FrameCount { get; set; }
        public double FrameRate { get; set; } = 30.0;
        
        public string DurationText => $"{Duration:F1}s";
        public string FrameInfo => $"{FrameCount} frames @ {FrameRate:F0}fps";
    }

    public class ModelImportHistoryItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Format { get; set; } = "";
        public DateTime ImportedAt { get; set; }
        public long FileSize { get; set; }
        
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - ImportedAt;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
                return ImportedAt.ToString("MM/dd HH:mm");
            }
        }

        public string FileSizeText => FileSize < 1024 * 1024 
            ? $"{FileSize / 1024:F0} KB" 
            : $"{FileSize / 1024.0 / 1024.0:F1} MB";
    }
    #endregion
}

