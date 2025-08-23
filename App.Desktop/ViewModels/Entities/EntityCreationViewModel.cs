using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Entities
{
    public class EntityCreationViewModel : INotifyPropertyChanged
    {
        private string _entityName = "";
        private string _entityType = "Virtual Assistant";
        private string _personalityDescription = "";
        private string _selected3DModel = "Custom Model";
        private string _selectedVoiceModel = "Sarah (Professional)";
        private double _formalityLevel = 5;
        private double _empathyLevel = 7;
        private double _creativityLevel = 6;
        private bool _has3DModel = false;
        private bool _isCreating = false;
        private string _creationStatus = "";

        public EntityCreationViewModel()
        {
            Browse3DModelCommand = new SimpleRelayCommand(Browse3DModel);
            CreateEntityCommand = new SimpleRelayCommand(CreateEntity, CanCreateEntity);
            RotateModelCommand = new SimpleRelayCommand(RotateModel);
            AnimateModelCommand = new SimpleRelayCommand(AnimateModel);
            TestVoiceCommand = new SimpleRelayCommand(TestVoice);
            ExportEntityCommand = new SimpleRelayCommand(ExportEntity, CanExportEntity);
            ApplyTemplateCommand = new SimpleRelayCommand<EntityTemplate>(ApplyTemplate);
            PreviewTemplateCommand = new SimpleRelayCommand<EntityTemplate>(PreviewTemplate);
            SaveTemplateCommand = new SimpleRelayCommand(SaveTemplate);
            ImportTemplateCommand = new SimpleRelayCommand(ImportTemplate);

            EntityTemplates = new ObservableCollection<EntityTemplate>
            {
                new() { Name = "Aria", Type = "Virtual Assistant", Icon = "🤖", Description = "Professional AI assistant with expertise in productivity and organization" },
                new() { Name = "Dr. Knowledge", Type = "Expert Advisor", Icon = "👨‍🔬", Description = "Academic expert specializing in research and educational guidance" },
                new() { Name = "Cassia", Type = "Conversation Partner", Icon = "👥", Description = "Warm and engaging conversationalist for casual interactions" },
                new() { Name = "Raven", Type = "Creative Collaborator", Icon = "🎭", Description = "Imaginative entity focused on artistic and creative endeavors" },
                new() { Name = "Luna", Type = "Character Roleplay", Icon = "🌙", Description = "Mystical character for fantasy and roleplay scenarios" },
                new() { Name = "Professor Sage", Type = "Educational Guide", Icon = "📚", Description = "Patient teacher specialized in explaining complex concepts" },
                new() { Name = "Echo", Type = "Conversation Partner", Icon = "💫", Description = "Philosophical thinker for deep discussions and debates" },
                new() { Name = "Zara", Type = "Virtual Assistant", Icon = "⚡", Description = "High-energy assistant for fast-paced professional environments" }
            };
        }

        public string EntityName
        {
            get => _entityName;
            set => SetProperty(ref _entityName, value);
        }

        public string EntityType
        {
            get => _entityType;
            set => SetProperty(ref _entityType, value);
        }

        public string PersonalityDescription
        {
            get => _personalityDescription;
            set => SetProperty(ref _personalityDescription, value);
        }

        public string Selected3DModel
        {
            get => _selected3DModel;
            set => SetProperty(ref _selected3DModel, value);
        }

        public string SelectedVoiceModel
        {
            get => _selectedVoiceModel;
            set => SetProperty(ref _selectedVoiceModel, value);
        }

        public double FormalityLevel
        {
            get => _formalityLevel;
            set => SetProperty(ref _formalityLevel, value);
        }

        public double EmpathyLevel
        {
            get => _empathyLevel;
            set => SetProperty(ref _empathyLevel, value);
        }

        public double CreativityLevel
        {
            get => _creativityLevel;
            set => SetProperty(ref _creativityLevel, value);
        }

        public bool Has3DModel
        {
            get => _has3DModel;
            set => SetProperty(ref _has3DModel, value);
        }

        public bool IsCreating
        {
            get => _isCreating;
            set => SetProperty(ref _isCreating, value);
        }

        public string CreationStatus
        {
            get => _creationStatus;
            set => SetProperty(ref _creationStatus, value);
        }

        public ObservableCollection<EntityTemplate> EntityTemplates { get; }

        public ICommand Browse3DModelCommand { get; }
        public ICommand CreateEntityCommand { get; }
        public ICommand RotateModelCommand { get; }
        public ICommand AnimateModelCommand { get; }
        public ICommand TestVoiceCommand { get; }
        public ICommand ExportEntityCommand { get; }
        public ICommand ApplyTemplateCommand { get; }
        public ICommand PreviewTemplateCommand { get; }
        public ICommand SaveTemplateCommand { get; }
        public ICommand ImportTemplateCommand { get; }

        private void Browse3DModel()
        {
            // TODO: Implement 3D model file browser
            Has3DModel = true;
            System.Diagnostics.Debug.WriteLine("Browse 3D model clicked");
        }

        private bool CanCreateEntity()
        {
            return !IsCreating && !string.IsNullOrWhiteSpace(EntityName);
        }

        private bool CanExportEntity()
        {
            return !IsCreating && Has3DModel;
        }

        private async void CreateEntity()
        {
            try
            {
                IsCreating = true;
                CreationStatus = "Initializing entity creation...";
                await Task.Delay(500);

                CreationStatus = "Loading 3D model and textures...";
                await Task.Delay(1500);

                CreationStatus = "Configuring voice synthesis...";
                await Task.Delay(1000);

                CreationStatus = "Training personality parameters...";
                await Task.Delay(2000);

                CreationStatus = "Optimizing behavioral patterns...";
                await Task.Delay(1500);

                CreationStatus = "Finalizing entity configuration...";
                await Task.Delay(1000);

                Has3DModel = true;
                CreationStatus = $"Entity '{EntityName}' created successfully!";
                await Task.Delay(2000);
                CreationStatus = "";
            }
            catch (Exception ex)
            {
                CreationStatus = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Entity creation error: {ex.Message}");
            }
            finally
            {
                IsCreating = false;
            }
        }

        private void RotateModel()
        {
            System.Diagnostics.Debug.WriteLine("Rotate 3D model");
        }

        private void AnimateModel()
        {
            System.Diagnostics.Debug.WriteLine("Animate 3D model");
        }

        private void TestVoice()
        {
            System.Diagnostics.Debug.WriteLine("Test entity voice");
        }

        private void ExportEntity()
        {
            System.Diagnostics.Debug.WriteLine("Export entity");
        }

        private void ApplyTemplate(EntityTemplate? template)
        {
            if (template == null) return;

            EntityName = template.Name;
            EntityType = template.Type;
            PersonalityDescription = template.Description;

            // Set behavioral parameters based on template type
            switch (template.Type)
            {
                case "Virtual Assistant":
                    FormalityLevel = 7;
                    EmpathyLevel = 6;
                    CreativityLevel = 4;
                    break;
                case "Expert Advisor":
                    FormalityLevel = 8;
                    EmpathyLevel = 5;
                    CreativityLevel = 3;
                    break;
                case "Conversation Partner":
                    FormalityLevel = 4;
                    EmpathyLevel = 8;
                    CreativityLevel = 7;
                    break;
                case "Creative Collaborator":
                    FormalityLevel = 3;
                    EmpathyLevel = 7;
                    CreativityLevel = 9;
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Applied template: {template.Name}");
        }

        private void PreviewTemplate(EntityTemplate? template)
        {
            if (template == null) return;
            System.Diagnostics.Debug.WriteLine($"Preview template: {template.Name}");
        }

        private void SaveTemplate()
        {
            System.Diagnostics.Debug.WriteLine("Save current configuration as template");
        }

        private void ImportTemplate()
        {
            System.Diagnostics.Debug.WriteLine("Import entity template");
        }

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

    public class EntityTemplate
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Description { get; set; } = "";
    }
}