using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Entities
{
    public class InteractionTestingViewModel : INotifyPropertyChanged
    {
        private string _messageText = "";
        private string _selectedScenario = "General Conversation";
        private string _selectedEntityName = "Select Entity";
        private double _responseConsistency = 8.5;
        private double _emotionalAppropriateness = 9.2;
        private double _topicAdherence = 7.8;
        private double _averageResponseTime = 1.2;
        private int _totalInteractions = 47;
        private bool _isSessionActive = false;

        public InteractionTestingViewModel()
        {
            MessageHistory = new ObservableCollection<ChatMessage>();
            TestScenarios = new ObservableCollection<string>
            {
                "General Conversation",
                "Technical Discussion",
                "Emotional Support",
                "Creative Collaboration",
                "Problem Solving",
                "Philosophical Debate",
                "Casual Chat",
                "Educational Context"
            };

            AvailableEntities = new ObservableCollection<string>
            {
                "Select Entity",
                "Dr. Watson - Academic Expert",
                "Luna - Creative Assistant",
                "Marcus - Technical Advisor",
                "Sophia - Emotional Support",
                "Phoenix - Philosopher"
            };

            SendMessageCommand = new SimpleRelayCommand(SendMessage, CanSendMessage);
            ClearHistoryCommand = new SimpleRelayCommand(ClearHistory);
            SaveSessionCommand = new SimpleRelayCommand(SaveSession);
            StartSessionCommand = new SimpleRelayCommand(StartSession);
            EndSessionCommand = new SimpleRelayCommand(EndSession);
            ExportDataCommand = new SimpleRelayCommand(ExportData);
        }

        #region Properties

        public string MessageText
        {
            get => _messageText;
            set => SetProperty(ref _messageText, value);
        }

        public string SelectedScenario
        {
            get => _selectedScenario;
            set => SetProperty(ref _selectedScenario, value);
        }

        public string SelectedEntityName
        {
            get => _selectedEntityName;
            set => SetProperty(ref _selectedEntityName, value);
        }

        public double ResponseConsistency
        {
            get => _responseConsistency;
            set => SetProperty(ref _responseConsistency, value);
        }

        public double EmotionalAppropriateness
        {
            get => _emotionalAppropriateness;
            set => SetProperty(ref _emotionalAppropriateness, value);
        }

        public double TopicAdherence
        {
            get => _topicAdherence;
            set => SetProperty(ref _topicAdherence, value);
        }

        public double AverageResponseTime
        {
            get => _averageResponseTime;
            set => SetProperty(ref _averageResponseTime, value);
        }

        public int TotalInteractions
        {
            get => _totalInteractions;
            set => SetProperty(ref _totalInteractions, value);
        }

        public bool IsSessionActive
        {
            get => _isSessionActive;
            set => SetProperty(ref _isSessionActive, value);
        }

        public ObservableCollection<ChatMessage> MessageHistory { get; }
        public ObservableCollection<string> TestScenarios { get; }
        public ObservableCollection<string> AvailableEntities { get; }

        #endregion

        #region Commands

        public ICommand SendMessageCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand SaveSessionCommand { get; }
        public ICommand StartSessionCommand { get; }
        public ICommand EndSessionCommand { get; }
        public ICommand ExportDataCommand { get; }

        #endregion

        #region Methods

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(MessageText) && IsSessionActive && SelectedEntityName != "Select Entity";
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageText)) return;

            var userMessage = new ChatMessage
            {
                Content = MessageText,
                IsFromUser = true,
                Timestamp = DateTime.Now,
                Sender = "You"
            };

            MessageHistory.Add(userMessage);

            var entityResponse = new ChatMessage
            {
                Content = GenerateResponse(MessageText),
                IsFromUser = false,
                Timestamp = DateTime.Now,
                Sender = SelectedEntityName
            };

            MessageHistory.Add(entityResponse);

            TotalInteractions++;
            MessageText = "";
            
            UpdateMetrics();
        }

        private string GenerateResponse(string userMessage)
        {
            var responses = new[]
            {
                "That's a fascinating perspective. Could you elaborate on that thought?",
                "I appreciate you sharing that with me. How does this relate to your broader goals?",
                "Interesting point. From my understanding, there might be another angle to consider...",
                "Thank you for bringing this up. Let me think about this carefully...",
                "I see what you're getting at. This reminds me of a similar concept I've encountered before.",
                "That's a thoughtful observation. What led you to this conclusion?",
                "I find that perspective compelling. Have you considered the implications of...",
                "Your insight is valuable here. This connects to something I've been thinking about..."
            };

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private void UpdateMetrics()
        {
            var random = new Random();
            ResponseConsistency = Math.Max(5.0, Math.Min(10.0, ResponseConsistency + (random.NextDouble() - 0.5) * 0.3));
            EmotionalAppropriateness = Math.Max(5.0, Math.Min(10.0, EmotionalAppropriateness + (random.NextDouble() - 0.5) * 0.2));
            TopicAdherence = Math.Max(5.0, Math.Min(10.0, TopicAdherence + (random.NextDouble() - 0.5) * 0.4));
            AverageResponseTime = Math.Max(0.5, Math.Min(3.0, AverageResponseTime + (random.NextDouble() - 0.5) * 0.1));
        }

        private void ClearHistory()
        {
            MessageHistory.Clear();
        }

        private void SaveSession()
        {
            System.Diagnostics.Debug.WriteLine("Saving interaction session");
            // TODO: Implement session saving
        }

        private void StartSession()
        {
            if (SelectedEntityName == "Select Entity") return;
            
            IsSessionActive = true;
            System.Diagnostics.Debug.WriteLine($"Starting interaction session with {SelectedEntityName}");
            
            var welcomeMessage = new ChatMessage
            {
                Content = $"Hello! I'm {SelectedEntityName.Split(' ')[0]}. I'm ready to interact with you in the {SelectedScenario} scenario. How can we begin?",
                IsFromUser = false,
                Timestamp = DateTime.Now,
                Sender = SelectedEntityName
            };
            
            MessageHistory.Add(welcomeMessage);
        }

        private void EndSession()
        {
            IsSessionActive = false;
            System.Diagnostics.Debug.WriteLine("Ending interaction session");
            
            var endMessage = new ChatMessage
            {
                Content = "Session ended. Thank you for the interaction!",
                IsFromUser = false,
                Timestamp = DateTime.Now,
                Sender = "System"
            };
            
            MessageHistory.Add(endMessage);
        }

        private void ExportData()
        {
            System.Diagnostics.Debug.WriteLine("Exporting interaction data");
            // TODO: Implement data export functionality
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content = "";
        private bool _isFromUser = false;
        private DateTime _timestamp = DateTime.Now;
        private string _sender = "";

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFromUser
        {
            get => _isFromUser;
            set
            {
                if (_isFromUser != value)
                {
                    _isFromUser = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Sender
        {
            get => _sender;
            set
            {
                if (_sender != value)
                {
                    _sender = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}