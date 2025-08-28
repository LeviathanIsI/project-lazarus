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

        public ChatViewModel()
        {
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
        }

        #region Properties

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
            set => SetProperty(ref _modelName, value);
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

                // Simulate streaming for now - replace with actual API call
                await StreamResponseAsync(assistantMessage, _cancellationTokenSource.Token);

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
                Stream = true // Enable streaming
            };
        }

        private async Task StreamResponseAsync(ChatMessageVm message, CancellationToken cancellationToken)
        {
            // Simulate streaming response - replace with actual SSE implementation
            var responseText = "This is a simulated streaming response that demonstrates the chat functionality. " +
                             "In a real implementation, this would connect to the orchestrator's chat completions endpoint " +
                             "and stream the response token by token. The UI will update in real-time as tokens arrive.";

            var words = responseText.Split(' ');
            var tokenCount = 0;

            foreach (var word in words)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                message.Content += (message.Content.Length > 0 ? " " : "") + word;
                tokenCount++;
                
                // Update tokens per second in real-time
                var elapsed = (DateTime.Now - message.Timestamp).TotalSeconds;
                if (elapsed > 0)
                {
                    message.TokensPerSecond = tokenCount / elapsed;
                    TokensPerSecond = message.TokensPerSecond;
                }

                // Simulate natural typing speed
                await Task.Delay(50, cancellationToken);
            }

            message.TokenCount = tokenCount;
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