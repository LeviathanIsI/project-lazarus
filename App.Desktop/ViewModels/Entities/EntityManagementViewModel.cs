using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;

namespace Lazarus.Desktop.ViewModels.Entities
{
    public class EntityManagementViewModel : INotifyPropertyChanged
    {
        private string _searchText = "";
        private string _selectedFilter = "All Types";
        private bool _isGridView = true;
        private EntityInfo? _selectedEntity;

        public EntityManagementViewModel()
        {
            Entities = new ObservableCollection<EntityInfo>
            {
                new() { 
                    Name = "Dr. Watson", 
                    EntityType = "Academic Expert", 
                    Icon = "🎓", 
                    Status = "Active", 
                    Version = "v2.1", 
                    PersonalitySummary = "A thoughtful academic with expertise in research and analysis. Passionate about learning and sharing knowledge with others.",
                    EmpathyLevel = 8.5,
                    CreativityLevel = 7.2,
                    FormalityLevel = 8.8,
                    TotalConversations = 342,
                    AverageRating = 9.1,
                    LastUsedDate = DateTime.Now.AddDays(-2),
                    VersionHistory = new[]
                    {
                        new VersionInfo { Version = "v2.1", Description = "Enhanced research capabilities", Date = DateTime.Now.AddDays(-7) },
                        new VersionInfo { Version = "v2.0", Description = "Major personality update", Date = DateTime.Now.AddDays(-30) },
                        new VersionInfo { Version = "v1.5", Description = "Improved conversation flow", Date = DateTime.Now.AddDays(-60) }
                    }
                },
                new() { 
                    Name = "Luna", 
                    EntityType = "Creative Assistant", 
                    Icon = "🎨", 
                    Status = "Active", 
                    Version = "v1.8", 
                    PersonalitySummary = "A vibrant creative spirit with boundless imagination. Loves exploring artistic ideas and helping with creative projects.",
                    EmpathyLevel = 9.2,
                    CreativityLevel = 9.8,
                    FormalityLevel = 4.5,
                    TotalConversations = 156,
                    AverageRating = 8.7,
                    LastUsedDate = DateTime.Now.AddHours(-6),
                    VersionHistory = new[]
                    {
                        new VersionInfo { Version = "v1.8", Description = "Added poetry generation", Date = DateTime.Now.AddDays(-14) },
                        new VersionInfo { Version = "v1.7", Description = "Enhanced creativity algorithms", Date = DateTime.Now.AddDays(-35) }
                    }
                },
                new() { 
                    Name = "Marcus", 
                    EntityType = "Technical Advisor", 
                    Icon = "⚡", 
                    Status = "Beta", 
                    Version = "v0.9", 
                    PersonalitySummary = "A precise technical expert with deep knowledge of systems and programming. Focuses on practical solutions.",
                    EmpathyLevel = 6.8,
                    CreativityLevel = 7.5,
                    FormalityLevel = 7.9,
                    TotalConversations = 89,
                    AverageRating = 8.9,
                    LastUsedDate = DateTime.Now.AddDays(-1),
                    VersionHistory = new[]
                    {
                        new VersionInfo { Version = "v0.9", Description = "Beta release with core features", Date = DateTime.Now.AddDays(-5) }
                    }
                }
            };

            FilterOptions = new ObservableCollection<string>
            {
                "All Types",
                "Academic Expert",
                "Creative Assistant", 
                "Technical Advisor",
                "Emotional Support",
                "Conversation Partner"
            };

            CreateEntityCommand = new SimpleRelayCommand(CreateEntity);
            EditEntityCommand = new SimpleRelayCommand<EntityInfo>(EditEntity);
            DeleteEntityCommand = new SimpleRelayCommand<EntityInfo>(DeleteEntity);
            DuplicateEntityCommand = new SimpleRelayCommand<EntityInfo>(DuplicateEntity);
            ExportEntityCommand = new SimpleRelayCommand<EntityInfo>(ExportEntity);
            ImportEntityCommand = new SimpleRelayCommand(ImportEntity);
            ToggleViewCommand = new SimpleRelayCommand(ToggleView);
            RefreshEntitiesCommand = new SimpleRelayCommand(RefreshEntities);
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set => SetProperty(ref _selectedFilter, value);
        }

        public bool IsGridView
        {
            get => _isGridView;
            set => SetProperty(ref _isGridView, value);
        }

        public EntityInfo? SelectedEntity
        {
            get => _selectedEntity;
            set => SetProperty(ref _selectedEntity, value);
        }

        public ObservableCollection<EntityInfo> Entities { get; }
        public ObservableCollection<string> FilterOptions { get; }

        #endregion

        #region Commands

        public ICommand CreateEntityCommand { get; }
        public ICommand EditEntityCommand { get; }
        public ICommand DeleteEntityCommand { get; }
        public ICommand DuplicateEntityCommand { get; }
        public ICommand ExportEntityCommand { get; }
        public ICommand ImportEntityCommand { get; }
        public ICommand ToggleViewCommand { get; }
        public ICommand RefreshEntitiesCommand { get; }

        #endregion

        #region Methods

        private void CreateEntity()
        {
            System.Diagnostics.Debug.WriteLine("Creating new entity");
            // TODO: Open entity creation dialog
        }

        private void EditEntity(EntityInfo? entity)
        {
            if (entity == null) return;
            System.Diagnostics.Debug.WriteLine($"Editing entity: {entity.Name}");
            // TODO: Open entity edit dialog
        }

        private void DeleteEntity(EntityInfo? entity)
        {
            if (entity == null) return;
            System.Diagnostics.Debug.WriteLine($"Deleting entity: {entity.Name}");
            Entities.Remove(entity);
        }

        private void DuplicateEntity(EntityInfo? entity)
        {
            if (entity == null) return;
            System.Diagnostics.Debug.WriteLine($"Duplicating entity: {entity.Name}");
            
            var duplicate = new EntityInfo
            {
                Name = $"{entity.Name} Copy",
                EntityType = entity.EntityType,
                Icon = entity.Icon,
                Status = "Draft",
                Version = "v1.0",
                PersonalitySummary = entity.PersonalitySummary,
                EmpathyLevel = entity.EmpathyLevel,
                CreativityLevel = entity.CreativityLevel,
                FormalityLevel = entity.FormalityLevel,
                TotalConversations = 0,
                AverageRating = 0.0,
                LastUsedDate = DateTime.Now,
                VersionHistory = new[] { new VersionInfo { Version = "v1.0", Description = "Initial copy", Date = DateTime.Now } }
            };
            
            Entities.Add(duplicate);
        }

        private void ExportEntity(EntityInfo? entity)
        {
            if (entity == null) return;
            System.Diagnostics.Debug.WriteLine($"Exporting entity: {entity.Name}");
            // TODO: Implement entity export
        }

        private void ImportEntity()
        {
            System.Diagnostics.Debug.WriteLine("Importing entity");
            // TODO: Implement entity import
        }

        private void ToggleView()
        {
            IsGridView = !IsGridView;
        }

        private void RefreshEntities()
        {
            System.Diagnostics.Debug.WriteLine("Refreshing entities");
            // TODO: Reload entities from storage
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

    public class EntityInfo : INotifyPropertyChanged
    {
        private string _name = "";
        private string _entityType = "";
        private string _icon = "";
        private string _status = "";
        private string _version = "";
        private string _personalitySummary = "";
        private double _empathyLevel;
        private double _creativityLevel;
        private double _formalityLevel;
        private int _totalConversations;
        private double _averageRating;
        private DateTime _lastUsedDate;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string EntityType
        {
            get => _entityType;
            set => SetProperty(ref _entityType, value);
        }

        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string PersonalitySummary
        {
            get => _personalitySummary;
            set => SetProperty(ref _personalitySummary, value);
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

        public double FormalityLevel
        {
            get => _formalityLevel;
            set => SetProperty(ref _formalityLevel, value);
        }

        public int TotalConversations
        {
            get => _totalConversations;
            set => SetProperty(ref _totalConversations, value);
        }

        public double AverageRating
        {
            get => _averageRating;
            set => SetProperty(ref _averageRating, value);
        }

        public DateTime LastUsedDate
        {
            get => _lastUsedDate;
            set => SetProperty(ref _lastUsedDate, value);
        }

        public VersionInfo[] VersionHistory { get; set; } = Array.Empty<VersionInfo>();

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class VersionInfo
    {
        public string Version { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }
    }
}