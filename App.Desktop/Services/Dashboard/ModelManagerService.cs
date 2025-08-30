using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Services.Dashboard
{
    /// <summary>
    /// Implementation of IModelManager using existing SystemStateViewModel and GlobalModelStateService
    /// Provides model management data for Dashboard widgets
    /// </summary>
    public class ModelManagerService : IModelManager, IDisposable
    {
        private readonly SystemStateViewModel _systemState;
        private readonly GlobalModelStateService _globalModelState;
        private readonly ObservableCollection<ModelInfo> _availableModels = new();
        private readonly object _lockObject = new object();
        
        private ModelInfo? _currentModel;
        private PerformanceMetrics _performanceMetrics = new();

        public ModelManagerService(SystemStateViewModel systemState, GlobalModelStateService globalModelState)
        {
            _systemState = systemState ?? throw new ArgumentNullException(nameof(systemState));
            _globalModelState = globalModelState ?? throw new ArgumentNullException(nameof(globalModelState));
            
            // Subscribe to state changes
            _systemState.PropertyChanged += OnSystemStateChanged;
            _globalModelState.PropertyChanged += OnGlobalModelStateChanged;
            
            // Initial data load
            UpdateCurrentModel();
            LoadAvailableModels();
        }

        #region IModelManager Implementation

        public ModelInfo GetCurrentModel()
        {
            lock (_lockObject)
            {
                if (_currentModel == null || _currentModel.Name != _systemState.CurrentModel)
                {
                    UpdateCurrentModel();
                }
                
                return _currentModel ?? new ModelInfo { Name = "No model loaded" };
            }
        }

        public ObservableCollection<ModelInfo> GetAvailableModels()
        {
            lock (_lockObject)
            {
                return new ObservableCollection<ModelInfo>(_availableModels);
            }
        }

        public PerformanceMetrics GetModelPerformance()
        {
            lock (_lockObject)
            {
                // Update performance metrics from SystemState
                _performanceMetrics.TokensPerSecond = ParseTokensPerSecond(_systemState.TokensPerSecond);
                _performanceMetrics.AverageResponseTime = TimeSpan.FromMilliseconds(500); // TODO: Get from actual metrics
                _performanceMetrics.TotalRequests = 0; // TODO: Track actual requests
                _performanceMetrics.FailedRequests = 0; // TODO: Track actual failures
                
                return _performanceMetrics;
            }
        }

        public async Task SwitchModelAsync(ModelInfo model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            
            // TODO: Implement actual model switching through orchestrator
            await Task.Delay(100); // Placeholder
            
            var oldModel = _currentModel;
            _currentModel = model;
            
            ModelChanged?.Invoke(this, new ModelChangedEventArgs
            {
                OldModel = oldModel,
                NewModel = model
            });
        }

        public event EventHandler<ModelChangedEventArgs>? ModelChanged;

        #endregion

        #region Private Methods

        private void UpdateCurrentModel()
        {
            var currentModelName = _systemState.CurrentModel;
            
            if (string.IsNullOrEmpty(currentModelName) || currentModelName == "No model loaded")
            {
                _currentModel = null;
                return;
            }

            // Try to find the model in available models
            var existingModel = _availableModels.FirstOrDefault(m => m.Name == currentModelName);
            if (existingModel != null)
            {
                existingModel.IsLoaded = true;
                existingModel.LastUsed = DateTime.Now;
                _currentModel = existingModel;
            }
            else
            {
                // Create a new model info for the current model
                _currentModel = new ModelInfo
                {
                    Name = currentModelName,
                    Path = "Unknown",
                    Format = "Unknown",
                    PreferredRunner = GetRunnerTypeFromString(_systemState.CurrentRunner),
                    SizeBytes = 0,
                    ContextLength = ParseContextLength(_systemState.ContextLength),
                    IsLoaded = true,
                    LastUsed = DateTime.Now
                };
            }
        }

        private void LoadAvailableModels()
        {
            // TODO: Load actual models from file system or orchestrator
            // For now, create some placeholder models
            _availableModels.Clear();
            
            // Add some common model types as placeholders
            var placeholderModels = new[]
            {
                new ModelInfo
                {
                    Name = "Llama-3-8B-Instruct",
                    Path = @"C:\Models\llama-3-8b-instruct.gguf",
                    Format = "GGUF",
                    PreferredRunner = RunnerType.LlamaCpp,
                    SizeBytes = 8_500_000_000,
                    ContextLength = 8192,
                    IsLoaded = false,
                    LastUsed = DateTime.Now.AddDays(-1)
                },
                new ModelInfo
                {
                    Name = "Mistral-7B-Instruct",
                    Path = @"C:\Models\mistral-7b-instruct.gguf",
                    Format = "GGUF",
                    PreferredRunner = RunnerType.LlamaCpp,
                    SizeBytes = 7_200_000_000,
                    ContextLength = 32768,
                    IsLoaded = false,
                    LastUsed = DateTime.Now.AddDays(-2)
                },
                new ModelInfo
                {
                    Name = "CodeLlama-13B",
                    Path = @"C:\Models\codellama-13b.gguf",
                    Format = "GGUF",
                    PreferredRunner = RunnerType.LlamaCpp,
                    SizeBytes = 13_000_000_000,
                    ContextLength = 16384,
                    IsLoaded = false,
                    LastUsed = DateTime.Now.AddDays(-5)
                }
            };

            foreach (var model in placeholderModels)
            {
                _availableModels.Add(model);
            }
        }

        private RunnerType GetRunnerTypeFromString(string? runnerName)
        {
            if (string.IsNullOrEmpty(runnerName)) return RunnerType.LlamaCpp;
            
            return runnerName.ToLowerInvariant() switch
            {
                "llama.cpp" => RunnerType.LlamaCpp,
                "llama-server" => RunnerType.LlamaServer,
                "vllm" => RunnerType.vLLM,
                "exllamav2" => RunnerType.ExLlamaV2,
                "ollama" => RunnerType.Ollama,
                _ => RunnerType.LlamaCpp
            };
        }

        private int ParseContextLength(string? contextLength)
        {
            if (string.IsNullOrEmpty(contextLength)) return 0;
            
            if (int.TryParse(contextLength.Replace("k", "000").Replace("K", "000"), out int length))
            {
                return length;
            }
            
            return 0;
        }

        private double ParseTokensPerSecond(string? tokensPerSecond)
        {
            if (string.IsNullOrEmpty(tokensPerSecond)) return 0.0;
            
            var cleanString = tokensPerSecond.Replace("t/s", "").Replace("tokens/s", "").Trim();
            if (double.TryParse(cleanString, out double tokens))
            {
                return tokens;
            }
            
            return 0.0;
        }

        private void OnSystemStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemStateViewModel.CurrentModel))
            {
                var oldModel = _currentModel;
                UpdateCurrentModel();
                
                if (oldModel?.Name != _currentModel?.Name)
                {
                    ModelChanged?.Invoke(this, new ModelChangedEventArgs
                    {
                        OldModel = oldModel,
                        NewModel = _currentModel
                    });
                }
            }
        }

        private void OnGlobalModelStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle global model state changes if needed
            // TODO: Implement based on GlobalModelStateService properties
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_systemState != null)
            {
                _systemState.PropertyChanged -= OnSystemStateChanged;
            }
            
            if (_globalModelState != null)
            {
                _globalModelState.PropertyChanged -= OnGlobalModelStateChanged;
            }
        }

        #endregion
    }
}
