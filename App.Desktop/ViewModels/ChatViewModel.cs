using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Models;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels
{
    /// <summary>
    /// Chat message view model with complete technical metrics
    /// </summary>
    public class ChatMessageVm : INotifyPropertyChanged
    {
        private string _role = "";
        private string _content = "";
        private DateTime _timestamp = DateTime.Now;
        private int _tokenCount = 0;
        private double _generationTime = 0;
        private double _tokensPerSecond = 0;

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public int TokenCount
        {
            get => _tokenCount;
            set => SetProperty(ref _tokenCount, value);
        }

        public double GenerationTime
        {
            get => _generationTime;
            set => SetProperty(ref _generationTime, value);
        }

        public double TokensPerSecond
        {
            get => _tokensPerSecond;
            set => SetProperty(ref _tokensPerSecond, value);
        }

        public bool IsAssistant => Role == "assistant";

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

    /// <summary>
    /// Complete ChatViewModel implementation with exact specifications
    /// </summary>
    public sealed class ChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DynamicParameterViewModel _parameterViewModel;
        private string _userInput = "";
        private bool _isStreaming = false;
        private double _tokensPerSecond = 0;
        private string _modelName = "No Model Loaded";
        private double _temperature = 0.7;
        private double _topP = 0.95;
        private int _maxTokens = 512;
        private double _repetitionPenalty = 1.0;
        private string _systemPrompt = "";
        private CancellationTokenSource? _cancellationTokenSource;

        private readonly GlobalModelStateService _globalState;

        public ChatViewModel(DynamicParameterViewModel parameterViewModel, GlobalModelStateService globalState)
        {
            _parameterViewModel = parameterViewModel;
            _globalState = globalState;
            Messages = new ObservableCollection<ChatMessageVm>();
            SendCommand = new RelayCommand(async _ => await SendAsync(), _ => CanSend);
            StopCommand = new RelayCommand(_ => StopGeneration(), _ => IsStreaming);
            
            // Initialize with test message for UI validation
            Messages.Add(new ChatMessageVm
            {
                Role = "assistant",
                Content = "Chat interface initialized. Ready for conversation.",
                Timestamp = DateTime.Now,
                TokenCount = 8,
                GenerationTime = 0.1,
                TokensPerSecond = 80
            });

            // Bind to global model state
            _globalState.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(GlobalModelStateService.CurrentModel) ||
                    args.PropertyName == nameof(GlobalModelStateService.LoadStatus))
                {
                    var cm = _globalState.CurrentModel;
                    ModelName = cm?.Name ?? "No Model Loaded";
                }
            };
            // Initialize from persisted state
            _globalState.Restore();
            ModelName = _globalState.CurrentModel?.Name ?? "No Model Loaded";
        }

        #region Properties

        public DynamicParameterViewModel ParameterViewModel => _parameterViewModel;

        public bool HasBaseModel => !string.IsNullOrWhiteSpace(ModelName) && !string.Equals(ModelName, "No Model Loaded", StringComparison.OrdinalIgnoreCase);

        public ObservableCollection<ChatMessageVm> Messages { get; }

        public string UserInput
        {
            get => _userInput;
            set
            {
                if (SetProperty(ref _userInput, value))
                {
                    OnPropertyChanged(nameof(CanSend));
                }
            }
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            private set
            {
                if (SetProperty(ref _isStreaming, value))
                {
                    OnPropertyChanged(nameof(CanSend));
                }
            }
        }

        public double TokensPerSecond
        {
            get => _tokensPerSecond;
            private set => SetProperty(ref _tokensPerSecond, value);
        }

        public string ModelName
        {
            get => _modelName;
            set
            {
                if (SetProperty(ref _modelName, value))
                {
                    OnPropertyChanged(nameof(HasBaseModel));
                }
            }
        }

        // Advanced parameters
        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public double TopP
        {
            get => _topP;
            set => SetProperty(ref _topP, value);
        }

        public int MaxTokens
        {
            get => _maxTokens;
            set => SetProperty(ref _maxTokens, value);
        }

        public double RepetitionPenalty
        {
            get => _repetitionPenalty;
            set => SetProperty(ref _repetitionPenalty, value);
        }

        public string SystemPrompt
        {
            get => _systemPrompt;
            set => SetProperty(ref _systemPrompt, value);
        }

        public bool CanSend => !string.IsNullOrWhiteSpace(UserInput) && !IsStreaming;

        #endregion

        #region Commands

        public ICommand SendCommand { get; }
        public ICommand StopCommand { get; }

        #endregion

        #region Methods

        private async Task SendAsync()
        {
            var message = UserInput.Trim();
            if (string.IsNullOrEmpty(message) || IsStreaming) return;

            // Add user message
            var userMessage = new ChatMessageVm
            {
                Role = "user",
                Content = message,
                Timestamp = DateTime.Now,
                TokenCount = EstimateTokenCount(message)
            };
            Messages.Add(userMessage);
            UserInput = "";

            // Start streaming response
            IsStreaming = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var startTime = DateTime.Now;

            try
            {
                var request = CreateChatRequest(message);
                var assistantMessage = new ChatMessageVm
                {
                    Role = "assistant",
                    Content = "",
                    Timestamp = DateTime.Now
                };
                Messages.Add(assistantMessage);

                // Call real API for chat completion
                await GetRealResponseAsync(request, assistantMessage, _cancellationTokenSource.Token);

                // Update metrics
                var endTime = DateTime.Now;
                assistantMessage.GenerationTime = (endTime - startTime).TotalSeconds;
                assistantMessage.TokenCount = EstimateTokenCount(assistantMessage.Content);
                
                if (assistantMessage.GenerationTime > 0)
                {
                    assistantMessage.TokensPerSecond = assistantMessage.TokenCount / assistantMessage.GenerationTime;
                    TokensPerSecond = assistantMessage.TokensPerSecond;
                }
            }
            catch (OperationCanceledException)
            {
                // Generation was cancelled
                if (Messages.LastOrDefault()?.Role == "assistant")
                {
                    Messages.Last().Content += " [Generation stopped]";
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessageVm
                {
                    Role = "system",
                    Content = $"Error: {ex.Message}",
                    Timestamp = DateTime.Now,
                    TokenCount = EstimateTokenCount(ex.Message)
                };
                Messages.Add(errorMessage);
            }
            finally
            {
                IsStreaming = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private ChatCompletionRequest CreateChatRequest(string userMessage)
        {
            var messages = new List<Lazarus.Shared.OpenAI.ChatMessage>();

            // Add system prompt if provided
            if (!string.IsNullOrWhiteSpace(SystemPrompt))
            {
                messages.Add(new Lazarus.Shared.OpenAI.ChatMessage
                {
                    Role = "system",
                    Content = SystemPrompt
                });
            }

            // Add recent conversation history (last 10 messages)
            var recentMessages = Messages.TakeLast(10).Where(m => m.Role != "system");
            foreach (var msg in recentMessages)
            {
                messages.Add(new Lazarus.Shared.OpenAI.ChatMessage
                {
                    Role = msg.Role,
                    Content = msg.Content
                });
            }

            return new ChatCompletionRequest
            {
                Model = ModelName,
                Messages = messages,
                Temperature = Temperature,
                TopP = TopP,
                MaxTokens = MaxTokens,
                RepetitionPenalty = RepetitionPenalty,
                Stream = false // Disable streaming for now - orchestrator doesn't support SSE yet
            };
        }

        private async Task GetRealResponseAsync(ChatCompletionRequest request, ChatMessageVm message, CancellationToken cancellationToken)
        {
            try
            {
                // Call the actual API
                var response = await ApiClient.ChatCompletionAsync(request);
                
                if (response?.Choices?.Count > 0)
                {
                    var content = response.Choices[0].Message?.Content ?? "";
                    
                    // For now, set the full response at once
                    // TODO: Implement actual SSE streaming when orchestrator supports it
                    message.Content = content;
                    message.TokenCount = EstimateTokenCount(content);
                    
                    // Update tokens per second
                    var elapsed = (DateTime.Now - message.Timestamp).TotalSeconds;
                    if (elapsed > 0)
                    {
                        message.TokensPerSecond = message.TokenCount / elapsed;
                        TokensPerSecond = message.TokensPerSecond;
                    }
                }
                else
                {
                    message.Content = "No response received from the model. Please check your connection and try again.";
                    message.TokenCount = EstimateTokenCount(message.Content);
                }
            }
            catch (Exception ex)
            {
                message.Content = $"Error communicating with AI: {ex.Message}";
                message.TokenCount = EstimateTokenCount(message.Content);
                Console.WriteLine($"[ChatViewModel] API error: {ex.Message}");
            }
        }

        private void StopGeneration()
        {
            _cancellationTokenSource?.Cancel();
        }

        private static int EstimateTokenCount(string text)
        {
            // Simple token estimation - approximately 4 characters per token
            return Math.Max(1, text.Length / 4);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}