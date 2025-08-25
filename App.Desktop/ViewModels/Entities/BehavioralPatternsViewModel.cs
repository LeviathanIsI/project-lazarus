using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Entities
{
    public class BehavioralPatternsViewModel : INotifyPropertyChanged
    {
        private string _selectedConversationalStyle = "Formal Academic";
        private string _customStyleDescription = "";
        private bool _isCustomStyle = false;
        
        private double _empathyLevel = 7;
        private double _curiosityLevel = 6;
        private double _assertivenessLevel = 5;
        private double _humorLevel = 4;
        private double _formalityLevel = 6;
        
        private bool _rememberPersonalDetails = true;
        private bool _rememberConversationHistory = true;
        private bool _rememberPreferences = true;
        private bool _rememberTechnicalKnowledge = false;
        private bool _rememberCreativeWorks = false;
        
        private string _greetingStyle = "Hello! I'm pleased to meet you. How may I assist you today?";
        private string _conflictResolution = "I understand we may have different perspectives. Let me try to find common ground and address your concerns constructively.";
        private string _topicTransitions = "That's an interesting point. Speaking of that, I'm curious about your thoughts on...";
        private string _customBehavioralScript = "// Define custom behavioral patterns here\n// Example: if (topic == 'emotional') { empathy_level += 2; }";
        
        private bool _contentFilteringEnabled = true;
        private bool _harmPreventionEnabled = true;
        private bool _privacyProtectionEnabled = true;
        private bool _truthfulnessEnabled = true;

        public BehavioralPatternsViewModel()
        {
            PersonalityQuirks = new ObservableCollection<PersonalityQuirk>
            {
                new() { Name = "Uses philosophical metaphors", Intensity = 6 },
                new() { Name = "Tends to ask follow-up questions", Intensity = 8 },
                new() { Name = "References literature occasionally", Intensity = 4 }
            };

            ApplyChangesCommand = new SimpleRelayCommand(ApplyChanges);
            PreviewPersonalityCommand = new SimpleRelayCommand(PreviewPersonality);
            SavePatternsCommand = new SimpleRelayCommand(SavePatterns);
            ResetToDefaultCommand = new SimpleRelayCommand(ResetToDefault);
            AddQuirkCommand = new SimpleRelayCommand(AddQuirk);
            EditQuirkCommand = new SimpleRelayCommand<PersonalityQuirk>(EditQuirk);
            RemoveQuirkCommand = new SimpleRelayCommand<PersonalityQuirk>(RemoveQuirk);
        }

        #region Properties

        public string SelectedConversationalStyle
        {
            get => _selectedConversationalStyle;
            set
            {
                if (SetProperty(ref _selectedConversationalStyle, value))
                {
                    IsCustomStyle = value == "Custom Style";
                }
            }
        }

        public string CustomStyleDescription
        {
            get => _customStyleDescription;
            set => SetProperty(ref _customStyleDescription, value);
        }

        public bool IsCustomStyle
        {
            get => _isCustomStyle;
            set => SetProperty(ref _isCustomStyle, value);
        }

        public double EmpathyLevel
        {
            get => _empathyLevel;
            set => SetProperty(ref _empathyLevel, value);
        }

        public double CuriosityLevel
        {
            get => _curiosityLevel;
            set => SetProperty(ref _curiosityLevel, value);
        }

        public double AssertivenessLevel
        {
            get => _assertivenessLevel;
            set => SetProperty(ref _assertivenessLevel, value);
        }

        public double HumorLevel
        {
            get => _humorLevel;
            set => SetProperty(ref _humorLevel, value);
        }

        public double FormalityLevel
        {
            get => _formalityLevel;
            set => SetProperty(ref _formalityLevel, value);
        }

        public bool RememberPersonalDetails
        {
            get => _rememberPersonalDetails;
            set => SetProperty(ref _rememberPersonalDetails, value);
        }

        public bool RememberConversationHistory
        {
            get => _rememberConversationHistory;
            set => SetProperty(ref _rememberConversationHistory, value);
        }

        public bool RememberPreferences
        {
            get => _rememberPreferences;
            set => SetProperty(ref _rememberPreferences, value);
        }

        public bool RememberTechnicalKnowledge
        {
            get => _rememberTechnicalKnowledge;
            set => SetProperty(ref _rememberTechnicalKnowledge, value);
        }

        public bool RememberCreativeWorks
        {
            get => _rememberCreativeWorks;
            set => SetProperty(ref _rememberCreativeWorks, value);
        }

        public string GreetingStyle
        {
            get => _greetingStyle;
            set => SetProperty(ref _greetingStyle, value);
        }

        public string ConflictResolution
        {
            get => _conflictResolution;
            set => SetProperty(ref _conflictResolution, value);
        }

        public string TopicTransitions
        {
            get => _topicTransitions;
            set => SetProperty(ref _topicTransitions, value);
        }

        public string CustomBehavioralScript
        {
            get => _customBehavioralScript;
            set => SetProperty(ref _customBehavioralScript, value);
        }

        public bool ContentFilteringEnabled
        {
            get => _contentFilteringEnabled;
            set => SetProperty(ref _contentFilteringEnabled, value);
        }

        public bool HarmPreventionEnabled
        {
            get => _harmPreventionEnabled;
            set => SetProperty(ref _harmPreventionEnabled, value);
        }

        public bool PrivacyProtectionEnabled
        {
            get => _privacyProtectionEnabled;
            set => SetProperty(ref _privacyProtectionEnabled, value);
        }

        public bool TruthfulnessEnabled
        {
            get => _truthfulnessEnabled;
            set => SetProperty(ref _truthfulnessEnabled, value);
        }

        public ObservableCollection<PersonalityQuirk> PersonalityQuirks { get; }

        #endregion

        #region Commands

        public ICommand ApplyChangesCommand { get; }
        public ICommand PreviewPersonalityCommand { get; }
        public ICommand SavePatternsCommand { get; }
        public ICommand ResetToDefaultCommand { get; }
        public ICommand AddQuirkCommand { get; }
        public ICommand EditQuirkCommand { get; }
        public ICommand RemoveQuirkCommand { get; }

        #endregion

        #region Methods

        private void ApplyChanges()
        {
            System.Diagnostics.Debug.WriteLine("Applying behavioral changes to entity");
            // TODO: Apply behavioral pattern changes to active entity
        }

        private void PreviewPersonality()
        {
            System.Diagnostics.Debug.WriteLine("Previewing personality with current settings");
            // TODO: Generate and display personality preview
        }

        private void SavePatterns()
        {
            System.Diagnostics.Debug.WriteLine("Saving behavioral patterns");
            // TODO: Save current behavioral patterns to file or database
        }

        private void ResetToDefault()
        {
            System.Diagnostics.Debug.WriteLine("Resetting to default behavioral patterns");
            
            EmpathyLevel = 7;
            CuriosityLevel = 6;
            AssertivenessLevel = 5;
            HumorLevel = 4;
            FormalityLevel = 6;
            
            RememberPersonalDetails = true;
            RememberConversationHistory = true;
            RememberPreferences = true;
            RememberTechnicalKnowledge = false;
            RememberCreativeWorks = false;
            
            GreetingStyle = "Hello! I'm pleased to meet you. How may I assist you today?";
            ConflictResolution = "I understand we may have different perspectives. Let me try to find common ground and address your concerns constructively.";
            TopicTransitions = "That's an interesting point. Speaking of that, I'm curious about your thoughts on...";
            
            PersonalityQuirks.Clear();
            PersonalityQuirks.Add(new PersonalityQuirk { Name = "Uses philosophical metaphors", Intensity = 6 });
            PersonalityQuirks.Add(new PersonalityQuirk { Name = "Tends to ask follow-up questions", Intensity = 8 });
            PersonalityQuirks.Add(new PersonalityQuirk { Name = "References literature occasionally", Intensity = 4 });
        }

        private void AddQuirk()
        {
            System.Diagnostics.Debug.WriteLine("Adding new personality quirk");
            PersonalityQuirks.Add(new PersonalityQuirk 
            { 
                Name = "New personality quirk", 
                Intensity = 5 
            });
        }

        private void EditQuirk(PersonalityQuirk? quirk)
        {
            if (quirk == null) return;
            System.Diagnostics.Debug.WriteLine($"Editing quirk: {quirk.Name}");
            // TODO: Open edit dialog for personality quirk
        }

        private void RemoveQuirk(PersonalityQuirk? quirk)
        {
            if (quirk == null) return;
            System.Diagnostics.Debug.WriteLine($"Removing quirk: {quirk.Name}");
            PersonalityQuirks.Remove(quirk);
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

    public class PersonalityQuirk : INotifyPropertyChanged
    {
        private string _name = "";
        private double _intensity = 5;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Intensity
        {
            get => _intensity;
            set
            {
                if (_intensity != value)
                {
                    _intensity = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}