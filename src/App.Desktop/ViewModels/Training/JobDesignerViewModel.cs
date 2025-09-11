using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class JobDesignerViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ITrainingService _trainingService;
        private TrainingJob? _currentJob;
        
        // Draft configuration that exists before job creation
        public TrainingDraft Draft { get; } = new();
        public bool HasJob => _currentJob != null;
        
        // Datasets tab
        public ObservableCollection<TrainingDatasetRef> AvailableDatasets { get; } = new();
        public ObservableCollection<TrainingDatasetRef> SelectedDatasets { get; } = new();
        
        // Configuration tab
        public ObservableCollection<string> Models { get; } = new();
        public ObservableCollection<string> Recipes { get; } = new();
        
        private string? _selectedModel;
        public string? SelectedModel { get => _selectedModel; set => SetProperty(ref _selectedModel, value); }
        
        private string? _selectedRecipe;
        public string? SelectedRecipe { get => _selectedRecipe; set => SetProperty(ref _selectedRecipe, value); }
        
        private string? _outputPath;
        public string? OutputPath { get => _outputPath; set => SetProperty(ref _outputPath, value); }
        
        // Resources tab
        public ObservableCollection<GpuInfo> Gpus { get; } = new();
        public ObservableCollection<string> Precisions { get; } = new();
        
        private GpuInfo? _selectedGpu;
        public GpuInfo? SelectedGpu { get => _selectedGpu; set => SetProperty(ref _selectedGpu, value); }
        
        private string? _selectedPrecision;
        public string? SelectedPrecision { get => _selectedPrecision; set => SetProperty(ref _selectedPrecision, value); }
        
        private int _batchSize = 1;
        public int BatchSize { get => _batchSize; set => SetProperty(ref _batchSize, value); }
        
        private long _estimatedVRAM;
        public long EstimatedVRAM { get => _estimatedVRAM; set => SetProperty(ref _estimatedVRAM, value); }

        // Overview strip
        private string? _overviewStatus;
        public string? OverviewStatus { get => _overviewStatus; set => SetProperty(ref _overviewStatus, value); }
        
        private double _overviewProgress;
        public double OverviewProgress { get => _overviewProgress; set => SetProperty(ref _overviewProgress, value); }
        
        private string? _overviewEta;
        public string? OverviewEta { get => _overviewEta; set => SetProperty(ref _overviewEta, value); }
        
        // Dataset presenter for modality-specific views
        private object? _datasetsPresenter;
        public object? DatasetsPresenter { get => _datasetsPresenter; set => SetProperty(ref _datasetsPresenter, value); }
        
        // Commands
        public ICommand SelectDatasetCommand { get; }
        public ICommand RemoveDatasetCommand { get; }
        public ICommand PreviewDatasetCommand { get; }
        public ICommand EstimateResourcesCommand { get; }
        
        // Conversation-specific commands
        public ICommand ImportConversationDatasetCommand { get; }
        public ICommand ImportChatHistoryCommand { get; }
        public ICommand CreateDatasetTemplateCommand { get; }
        public ICommand ValidateConversationFormatCommand { get; }
        public ICommand EditDatasetCommand { get; }
        public ICommand CreateJobCommand { get; }
        
        // Parameter indexer for unified draft/job editing
        public string this[string key]
        {
            get
            {
                if (HasJob && _currentJob!.ConfigId != null) 
                {
                    // TODO: Get from job config
                    return "";
                }
                return Draft.Params.TryGetValue(key, out var value) ? value : "";
            }
            set
            {
                if (HasJob && _currentJob!.ConfigId != null)
                {
                    // TODO: Update job config
                }
                else
                {
                    Draft.Params[key] = value;
                }
                OnPropertyChanged($"Item[{key}]");
                OnPropertyChanged(nameof(Draft));
            }
        }
        
        public JobDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            
            SelectDatasetCommand = new RelayCommand<TrainingDatasetRef>(dataset => SelectDataset(dataset));
            RemoveDatasetCommand = new RelayCommand<TrainingDatasetRef>(dataset => RemoveDataset(dataset));
            PreviewDatasetCommand = new RelayCommand<TrainingDatasetRef>(dataset => PreviewDataset(dataset));
            EstimateResourcesCommand = new RelayCommand(async _ => await EstimateResourcesAsync());
            
            // Conversation-specific commands
            ImportConversationDatasetCommand = new RelayCommand(async _ => await ImportConversationDatasetAsync());
            ImportChatHistoryCommand = new RelayCommand(async _ => await ImportChatHistoryAsync());
            CreateDatasetTemplateCommand = new RelayCommand(async _ => await CreateDatasetTemplateAsync());
            ValidateConversationFormatCommand = new RelayCommand(async _ => await ValidateConversationFormatAsync());
            EditDatasetCommand = new RelayCommand<TrainingDatasetRef>(dataset => EditDataset(dataset));
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            
            // TODO(training): Load initial data
            LoadModelsAndRecipes();
            LoadGpusAndPrecisions();
        }
        
        public void SetSelectedJob(TrainingJob? job)
        {
            _currentJob = job;
            OnPropertyChanged(nameof(HasJob));
            OnPropertyChanged(nameof(CurrentJob));
            
            if (job != null)
            {
                _ = LoadJobDataAsync(job);
            }
            // Don't clear draft - keep it for when user deselects
        }
        
        public TrainingJob? CurrentJob => _currentJob;
        
        public void Dispose()
        {
            // TODO(training): Clean up subscriptions
        }
        
        private void LoadModelsAndRecipes()
        {
            // TODO(training): Load from model service
            Models.Add("llama-7b-chat");
            Models.Add("llama-13b-chat");
            Models.Add("mistral-7b-instruct");
            
            Recipes.Add("LoRA");
            Recipes.Add("QLoRA");
            Recipes.Add("Full Fine-tune");
        }
        
        private void LoadGpusAndPrecisions()
        {
            // TODO(training): Load from training service
            Precisions.Add("FP32");
            Precisions.Add("FP16");
            Precisions.Add("BF16");
            Precisions.Add("INT8");
            Precisions.Add("INT4");
            
            _ = LoadAvailableGpusAsync();
        }
        
        private async Task LoadAvailableGpusAsync()
        {
            try
            {
                var gpus = await _trainingService.GetAvailableGpusAsync();
                Gpus.Clear();
                foreach (var gpu in gpus)
                {
                    Gpus.Add(gpu);
                }
                if (Gpus.Any())
                {
                    SelectedGpu = Gpus.First();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load GPUs: {ex.Message}");
            }
        }
        
        private async Task LoadJobDataAsync(TrainingJob job)
        {
            try
            {
                // TODO(training): Load job configuration and datasets
                var datasets = await _trainingService.GetDatasetsAsync(job.Modality);
                AvailableDatasets.Clear();
                foreach (var dataset in datasets)
                {
                    AvailableDatasets.Add(dataset);
                }
                
                OverviewStatus = job.Status.ToString();
                OverviewProgress = job.Progress;
                OverviewEta = job.EstimatedTimeRemaining?.ToString(@"hh\:mm\:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load job data: {ex.Message}");
            }
        }
        
        private void SelectDataset(TrainingDatasetRef? dataset)
        {
            if (dataset == null) return;
            
            if (HasJob)
            {
                if (!SelectedDatasets.Contains(dataset))
                {
                    SelectedDatasets.Add(dataset);
                    _ = EstimateResourcesAsync();
                }
            }
            else
            {
                // Add to draft
                if (!Draft.Datasets.Contains(dataset))
                {
                    Draft.Datasets.Add(dataset);
                    OnPropertyChanged(nameof(Draft));
                    _ = EstimateResourcesAsync();
                }
            }
        }
        
        private async Task CreateJobAsync()
        {
            if (HasJob) return;
            
            try
            {
                var job = await _trainingService.CreateJobAsync(Draft.Name, Draft.Modality);
                job.OutputPath = Draft.Params.TryGetValue("OutputPath", out var path) ? path : $"./models/{Draft.Name}";
                
                // Transfer datasets from draft
                foreach (var dataset in Draft.Datasets)
                {
                    job.DatasetIds.Add(dataset.Id);
                }
                
                // TODO: Transfer other configuration from draft
                await _trainingService.UpdateJobAsync(job);
                
                // This will be handled by the parent ViewModel to update selection
                System.Diagnostics.Debug.WriteLine($"Created job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }
        
        private void RemoveDataset(TrainingDatasetRef? dataset)
        {
            if (dataset != null)
            {
                SelectedDatasets.Remove(dataset);
                _ = EstimateResourcesAsync();
            }
        }
        
        private void PreviewDataset(TrainingDatasetRef? dataset)
        {
            // TODO(training): Show dataset preview in inspector
            System.Diagnostics.Debug.WriteLine($"Preview dataset: {dataset?.Name}");
        }
        
        private async Task EstimateResourcesAsync()
        {
            if (_currentJob == null) return;
            
            try
            {
                // TODO(training): Create config and resources from current settings
                var config = new TrainingConfig
                {
                    Id = Guid.NewGuid().ToString(),
                    ModelId = SelectedModel ?? "",
                    Recipe = SelectedRecipe ?? "LoRA",
                    Modality = _currentJob.Modality,
                    OutputPath = OutputPath ?? ""
                };
                
                var resources = new TrainingResources
                {
                    Id = Guid.NewGuid().ToString(),
                    GpuIds = SelectedGpu != null ? new[] { SelectedGpu.Id }.ToList() : new(),
                    BatchSize = BatchSize,
                    Precision = Enum.TryParse<PrecisionType>(SelectedPrecision, out var precision) ? precision : PrecisionType.FP16
                };
                
                var estimate = await _trainingService.EstimateResourcesAsync(config, resources);
                EstimatedVRAM = estimate.EstimatedVramBytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to estimate resources: {ex.Message}");
            }
        }
        
        // Conversation-specific dataset management
        private async Task ImportConversationDatasetAsync()
        {
            // TODO(training): Show file dialog to select JSONL files
            System.Diagnostics.Debug.WriteLine("Import JSONL conversation dataset");
            await Task.CompletedTask;
        }
        
        private async Task ImportChatHistoryAsync()
        {
            // TODO(training): Convert existing chat sessions to training format
            System.Diagnostics.Debug.WriteLine("Import from chat history");
            await Task.CompletedTask;
        }
        
        private async Task CreateDatasetTemplateAsync()
        {
            // TODO(training): Create conversation dataset template with example format
            System.Diagnostics.Debug.WriteLine("Create conversation template");
            await Task.CompletedTask;
        }
        
        private async Task ValidateConversationFormatAsync()
        {
            // TODO(training): Validate JSONL format and compute statistics
            System.Diagnostics.Debug.WriteLine("Validate conversation format");
            await Task.CompletedTask;
        }
        
        private void EditDataset(TrainingDatasetRef? dataset)
        {
            // TODO(training): Open dataset editor for conversation format adjustment
            if (dataset != null)
            {
                System.Diagnostics.Debug.WriteLine($"Edit dataset: {dataset.Name}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        private void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}