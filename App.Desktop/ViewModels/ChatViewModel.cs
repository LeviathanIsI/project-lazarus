using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;        // for RelayCommand
using Lazarus.Shared.OpenAI;          // for ChatCompletionRequest/Response

namespace Lazarus.Desktop.ViewModels
{
    public class UiChatMessage
    {
        public string Role { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class ChatViewModel : INotifyPropertyChanged
    {
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
                    // force WPF to re-check CanExecute
                    (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<UiChatMessage> Messages { get; } = new();

        public ICommand SendCommand { get; }

        public ChatViewModel()
        {
            SendCommand = new RelayCommand(async _ => await SendAsync(), _ => !string.IsNullOrWhiteSpace(Input));
        }

        private async Task SendAsync()
        {
            var text = Input.Trim();
            if (string.IsNullOrEmpty(text)) return;

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

                var response = await ApiClient.ChatCompletionAsync(request);

                if (response?.Choices != null && response.Choices.Count > 0)
                {
                    var content = response.Choices[0].Message.Content ?? "";

                    Messages.Add(new UiChatMessage
                    {
                        Role = "assistant",
                        Text = content
                    });
                }
                else
                {
                    Messages.Add(new UiChatMessage { Role = "system", Text = "[No response from API]" });
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new UiChatMessage { Role = "system", Text = $"Error: {ex.Message}" });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
