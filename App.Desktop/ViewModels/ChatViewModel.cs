using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using Lazarus.Desktop.Helpers;        // for RelayCommand
using Lazarus.Shared.OpenAI;          // for ChatCompletionRequest/Response
using Lazarus.Shared.Models;          // for AppliedLoRAInfo

namespace Lazarus.Desktop.ViewModels
{
    public class UiChatMessage
    {
        public string Role { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class ChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DynamicParameterViewModel _parameterViewModel;
        private readonly BaseModelViewModel _baseModelViewModel;
        private readonly LorAsViewModel _lorAsViewModel;
        private string _input = string.Empty;

        public string Input
        {
            get => _input;
            set
            {
                if (_input != value)
                {
                    _input = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<UiChatMessage> Messages { get; } = new();

        public ICommand SendCommand { get; }

        public DynamicParameterViewModel ParameterViewModel => _parameterViewModel;
        public BaseModelViewModel BaseModelViewModel => _baseModelViewModel;
        public LorAsViewModel LorAsViewModel => _lorAsViewModel;
        
        public bool HasBaseModel => _baseModelViewModel.SelectedModel != null;
        public bool CanChat => HasBaseModel && !string.IsNullOrWhiteSpace(Input);

        public ChatViewModel(DynamicParameterViewModel parameterViewModel, BaseModelViewModel baseModelViewModel, LorAsViewModel lorAsViewModel)
        {
            _parameterViewModel = parameterViewModel;
            _baseModelViewModel = baseModelViewModel;
            _lorAsViewModel = lorAsViewModel;
            
            SendCommand = new RelayCommand(async _ => await SendAsync(), _ => CanChat);
            
            // Subscribe to model changes to update UI state
            _baseModelViewModel.PropertyChanged += OnBaseModelViewModelPropertyChanged;
            _lorAsViewModel.PropertyChanged += OnLorAsViewModelPropertyChanged;
            
            // Subscribe to cross-tab LoRA state changes
            LorAsViewModel.LoRAStateChanged += OnCrossTabLoRAStateChanged;
            
            _ = InitializeParametersAsync();
        }
        
        private void OnBaseModelViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseModelViewModel.SelectedModel))
            {
                OnPropertyChanged(nameof(HasBaseModel));
                OnPropertyChanged(nameof(CanChat));
            }
        }
        
        private void OnLorAsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LorAsViewModel.AppliedLoRAsCount) || 
                e.PropertyName == nameof(LorAsViewModel.TotalWeight))
            {
                // Trigger UI update for LoRA status in conversation tab
                OnPropertyChanged(nameof(LorAsViewModel));
                
                // Update parameter panel with LoRA-aware capabilities
                _ = UpdateParameterPanelWithLoRAsAsync();
            }
        }
        
        /// <summary>
        /// Handle cross-tab LoRA state changes for real-time synchronization
        /// </summary>
        private void OnCrossTabLoRAStateChanged(object? sender, EventArgs e)
        {
            try
            {
                Console.WriteLine($"[ChatViewModel] 🔔 Cross-tab LoRA state change detected! Applied: {LorAsViewModel.AppliedLoRAsCount}, Total: {LorAsViewModel.TotalAppliedLoRAsCount}");
                
                // Simple property notification - no complex async operations
                OnPropertyChanged(nameof(LorAsViewModel));
                
                // Update parameter panel safely
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateParameterPanelWithLoRAsAsync();
                    }
                    catch
                    {
                        // Silent fail to prevent crashes
                    }
                });
            }
            catch
            {
                // Silent fail to prevent crashes
            }
        }
        
        /// <summary>
        /// Update the parameter panel to reflect active LoRAs from orchestrator truth
        /// </summary>
        private async Task UpdateParameterPanelWithLoRAsAsync()
        {
            try
            {
                Console.WriteLine($"[ChatViewModel] Querying orchestrator for ground truth LoRA state...");
                
                // CRITICAL: Query orchestrator for actual LoRA state, not local view model state
                var orchestratorLoRAs = await ApiClient.GetAppliedLoRAsAsync() ?? new List<AppliedLoRAInfo>();
                
                Console.WriteLine($"[ChatViewModel] Found {orchestratorLoRAs.Count} active LoRAs in orchestrator");
                
                // Update parameter view model with orchestrator truth
                _parameterViewModel.UpdateAppliedLoRAs(orchestratorLoRAs);
                
                // Force refresh of model capabilities with LoRA-aware introspection
                await _parameterViewModel.RefreshCapabilitiesAsync();
                
                Console.WriteLine($"[ChatViewModel] ✅ Parameter panel synchronized with orchestrator LoRA state ({orchestratorLoRAs.Count} LoRAs)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatViewModel] Failed to sync parameter panel with orchestrator LoRA state: {ex.Message}");
                
                // Fallback to empty LoRA state if orchestrator query fails
                _parameterViewModel.UpdateAppliedLoRAs(new List<AppliedLoRAInfo>());
            }
        }
        
        /// <summary>
        /// Determine adapter type from LoRA name for parameter influence calculation
        /// </summary>
        private static string DetermineAdapterType(string loraName)
        {
            var name = loraName.ToLowerInvariant();
            
            if (name.Contains("style") || name.Contains("art")) return "style";
            if (name.Contains("character") || name.Contains("char") || name.Contains("persona")) return "character";
            if (name.Contains("concept") || name.Contains("subject")) return "concept";
            if (name.Contains("pose") || name.Contains("position")) return "pose";
            if (name.Contains("clothing") || name.Contains("outfit")) return "clothing";
            if (name.Contains("background") || name.Contains("scene")) return "scene";
            
            return "general";
        }

        private async Task InitializeParametersAsync()
        {
            await _parameterViewModel.LoadModelCapabilitiesAsync("current");
            
            // Initial LoRA status update
            await UpdateParameterPanelWithLoRAsAsync();
            
            // Start periodic LoRA state synchronization
            _ = Task.Run(async () => await PeriodicLoRAStateSyncAsync());
        }
        
        /// <summary>
        /// Periodically sync LoRA state with orchestrator to catch changes made in LoRA management tab
        /// </summary>
        private async Task PeriodicLoRAStateSyncAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3)); // Check every 3 seconds for LoRA state changes
                    
                    // Query orchestrator for current LoRA state
                    var currentLoRAs = await ApiClient.GetAppliedLoRAsAsync() ?? new List<AppliedLoRAInfo>();
                    var currentCount = currentLoRAs.Count;
                    var localCount = _parameterViewModel.AppliedLoRAs.Count;
                    
                    // Check if LoRA state has changed
                    if (currentCount != localCount || HasLoRAStateChanged(currentLoRAs, _parameterViewModel.AppliedLoRAs))
                    {
                        Console.WriteLine($"[ChatViewModel] LoRA state change detected: {localCount} → {currentCount} LoRAs");
                        
                        // Update parameter panel on UI thread
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await UpdateParameterPanelWithLoRAsAsync();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatViewModel] Periodic LoRA sync failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Check if the LoRA state has meaningfully changed
        /// </summary>
        private static bool HasLoRAStateChanged(List<AppliedLoRAInfo> orchestratorLoRAs, List<AppliedLoRAInfo> localLoRAs)
        {
            if (orchestratorLoRAs.Count != localLoRAs.Count) return true;
            
            // Check for changes in LoRA IDs, weights, or enabled state
            var orchestratorState = orchestratorLoRAs.OrderBy(l => l.Id)
                .Select(l => $"{l.Id}:{l.Weight:F2}:{l.IsEnabled}")
                .ToList();
                
            var localState = localLoRAs.OrderBy(l => l.Id)
                .Select(l => $"{l.Id}:{l.Weight:F2}:{l.IsEnabled}")
                .ToList();
            
            return !orchestratorState.SequenceEqual(localState);
        }

        private async Task SendAsync()
        {
            var text = Input.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Console.WriteLine($"[ChatViewModel] Sending message: {text}");
            Messages.Add(new UiChatMessage { Role = "user", Text = text });
            Input = string.Empty;

            try
            {
                var request = new ChatCompletionRequest
                {
                    Model = "Qwen2.5-32B-Instruct-Q5_K_M.gguf",
                    Messages = new()
                    {
                        new Lazarus.Shared.OpenAI.ChatMessage { Role = "user", Content = text }
                    }
                };

                // Inject dynamic parameters - CRITICAL neural pathway wiring
                var parameters = _parameterViewModel.GetParameterValues();
                Console.WriteLine($"[ChatViewModel] Injecting {parameters.Count} dynamic parameters:");
                
                foreach (var (paramName, value) in parameters)
                {
                    Console.WriteLine($"[ChatViewModel] Parameter: {paramName} = {value}");
                    
                    switch (paramName.ToLowerInvariant())
                    {
                        case "temperature":
                            if (value is double tempDouble) request.Temperature = tempDouble;
                            else if (double.TryParse(value.ToString(), out var temp)) request.Temperature = temp;
                            break;
                        case "max_tokens":
                        case "n_predict":
                            if (value is int maxTokensInt) request.MaxTokens = maxTokensInt;
                            else if (int.TryParse(value.ToString(), out var maxTokens)) request.MaxTokens = maxTokens;
                            break;
                        case "top_p":
                            if (value is double topPDouble) request.TopP = topPDouble;
                            else if (double.TryParse(value.ToString(), out var topP)) request.TopP = topP;
                            break;
                        case "top_k":
                            if (value is int topKInt) request.TopK = topKInt;
                            else if (int.TryParse(value.ToString(), out var topK)) request.TopK = topK;
                            break;
                        case "min_p":
                            if (value is double minPDouble) request.MinP = minPDouble;
                            else if (double.TryParse(value.ToString(), out var minP)) request.MinP = minP;
                            break;
                        case "typical_p":
                            if (value is double typicalPDouble) request.TypicalP = typicalPDouble;
                            else if (double.TryParse(value.ToString(), out var typicalP)) request.TypicalP = typicalP;
                            break;
                        case "repetition_penalty":
                        case "repeat_penalty":
                            if (value is double repPenaltyDouble) request.RepetitionPenalty = repPenaltyDouble;
                            else if (double.TryParse(value.ToString(), out var repPenalty)) request.RepetitionPenalty = repPenalty;
                            break;
                        case "frequency_penalty":
                            if (value is double freqPenaltyDouble) request.FrequencyPenalty = freqPenaltyDouble;
                            else if (double.TryParse(value.ToString(), out var freqPenalty)) request.FrequencyPenalty = freqPenalty;
                            break;
                        case "presence_penalty":
                            if (value is double presPenaltyDouble) request.PresencePenalty = presPenaltyDouble;
                            else if (double.TryParse(value.ToString(), out var presPenalty)) request.PresencePenalty = presPenalty;
                            break;
                        case "seed":
                            if (value is int seedInt) request.Seed = seedInt;
                            else if (int.TryParse(value.ToString(), out var seed)) request.Seed = seed;
                            break;
                        case "mirostat":
                        case "mirostat_mode":
                            if (value is int mirostatInt) request.MirostatMode = mirostatInt;
                            else if (int.TryParse(value.ToString(), out var mirostat)) request.MirostatMode = mirostat;
                            break;
                        case "mirostat_tau":
                            if (value is double mirostatTauDouble) request.MirostatTau = mirostatTauDouble;
                            else if (double.TryParse(value.ToString(), out var mirostatTau)) request.MirostatTau = mirostatTau;
                            break;
                        case "mirostat_eta":
                            if (value is double mirostatEtaDouble) request.MirostatEta = mirostatEtaDouble;
                            else if (double.TryParse(value.ToString(), out var mirostatEta)) request.MirostatEta = mirostatEta;
                            break;
                        case "tfs_z":
                            if (value is double tfsZDouble) request.TfsZ = tfsZDouble;
                            else if (double.TryParse(value.ToString(), out var tfsZ)) request.TfsZ = tfsZ;
                            break;
                        case "eta_cutoff":
                            if (value is double etaCutoffDouble) request.EtaCutoff = etaCutoffDouble;
                            else if (double.TryParse(value.ToString(), out var etaCutoff)) request.EtaCutoff = etaCutoff;
                            break;
                        case "epsilon_cutoff":
                            if (value is double epsilonCutoffDouble) request.EpsilonCutoff = epsilonCutoffDouble;
                            else if (double.TryParse(value.ToString(), out var epsilonCutoff)) request.EpsilonCutoff = epsilonCutoff;
                            break;
                        default:
                            Console.WriteLine($"[ChatViewModel] Warning: Unknown parameter '{paramName}' with value '{value}' - skipped");
                            break;
                    }
                }

                Console.WriteLine($"[ChatViewModel] Final request parameters - Temperature: {request.Temperature}, TopP: {request.TopP}, TopK: {request.TopK}");

                Console.WriteLine($"[ChatViewModel] Created request for model: {request.Model}");
                var response = await ApiClient.ChatCompletionAsync(request);
                Console.WriteLine($"[ChatViewModel] Received response: {response != null}");

                if (response?.Choices != null && response.Choices.Count > 0)
                {
                    var content = response.Choices[0].Message.Content ?? "";
                    Debug.WriteLine($"[ChatViewModel] Response content length: {content.Length}");

                    Messages.Add(new UiChatMessage
                    {
                        Role = "assistant",
                        Text = content
                    });
                }
                else
                {
                    Debug.WriteLine($"[ChatViewModel] No valid response - response null: {response == null}, choices null: {response?.Choices == null}, choices count: {response?.Choices?.Count ?? 0}");
                    Messages.Add(new UiChatMessage { Role = "system", Text = "[No response from API]" });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatViewModel] SendAsync exception: {ex.GetType().Name}: {ex.Message}");
                Messages.Add(new UiChatMessage { Role = "system", Text = $"Error: {ex.Message}" });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Unsubscribe from ViewModels to prevent memory leaks
                _baseModelViewModel.PropertyChanged -= OnBaseModelViewModelPropertyChanged;
                _lorAsViewModel.PropertyChanged -= OnLorAsViewModelPropertyChanged;
                _disposed = true;
            }
        }

        #endregion
    }
}
