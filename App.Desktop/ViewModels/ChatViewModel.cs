using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

namespace Lazarus.Desktop.ViewModels
{
    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public sealed class ChatViewModel : INotifyPropertyChanged
    {
        private string _input = "";
        private bool _isBusy;
        private string _modelId = "local-dev";

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string Input
        {
            get => _input;
            set { _input = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        public string ModelId
        {
            get => _modelId;
            set { _modelId = value; OnPropertyChanged(); }
        }

        public ICommand SendCommand { get; }

        public ChatViewModel()
        {
            // Remove the CanExecute check - let the button styling handle visual state
            SendCommand = new RelayCommand(async _ => await SendAsync());
        }

        private async Task SendAsync()
        {
            var user = Input.Trim();
            if (string.IsNullOrEmpty(user)) return; // Still validate in the method

            Messages.Add(new ChatMessage { Role = "user", Text = user });
            Input = "";
            IsBusy = true;

            try
            {
                var resp = await ApiClient.ChatAsync(user);

                if (resp.HasValue)
                {
                    var (id, model, content) = resp.Value;
                    Messages.Add(new ChatMessage { Role = "assistant", Text = content ?? "No response" });
                }
                else
                {
                    Messages.Add(new ChatMessage { Role = "system", Text = "Failed to get response from API" });
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Role = "system", Text = $"Error: {ex.Message}" });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Predicate<object?>? _canExecute;
        private readonly Func<object?, Task> _execute;

        public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter) => await _execute(parameter);

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}