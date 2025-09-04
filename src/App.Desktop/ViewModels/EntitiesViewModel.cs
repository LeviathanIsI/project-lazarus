using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Entities section
/// </summary>
public partial class EntitiesViewModel : BaseViewModel
{
    private readonly ILogger<EntitiesViewModel> _logger;
    private EntityItem? _selectedEntity;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntitiesViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public EntitiesViewModel(ILogger<EntitiesViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Entities";
        StatusMessage = "Entity management tools coming soon";
        
        Entities = new ObservableCollection<EntityItem>();
        
        CreateEntityCommand = new RelayCommand(ExecuteCreateEntity);
        EditEntityCommand = new RelayCommand<EntityItem>(ExecuteEditEntity);
        DeleteEntityCommand = new RelayCommand<EntityItem>(ExecuteDeleteEntity);
        
        LoadSampleEntities();
        
        _logger.LogInformation("Entities view model initialized");
    }

    public string Title { get; }
    
    public EntityItem? SelectedEntity
    {
        get => _selectedEntity;
        set => SetProperty(ref _selectedEntity, value);
    }

    public ObservableCollection<EntityItem> Entities { get; }
    public IRelayCommand CreateEntityCommand { get; }
    public IRelayCommand<EntityItem> EditEntityCommand { get; }
    public IRelayCommand<EntityItem> DeleteEntityCommand { get; }

    private void ExecuteCreateEntity()
    {
        _logger.LogInformation("Creating new entity");
        
        var newEntity = new EntityItem(
            $"Entity_{DateTime.Now:HHmmss}",
            "Custom",
            "Active",
            new Dictionary<string, string>
            {
                { "Type", "AI Agent" },
                { "Purpose", "General Assistant" },
                { "Version", "1.0" }
            },
            DateTime.Now
        );
        
        Entities.Insert(0, newEntity);
        SelectedEntity = newEntity;
        StatusMessage = $"Entity '{newEntity.Name}' created";
    }

    private void ExecuteEditEntity(EntityItem? entity)
    {
        if (entity == null) return;
        
        _logger.LogInformation("Editing entity: {EntityName}", entity.Name);
        SelectedEntity = entity;
        StatusMessage = $"Editing entity '{entity.Name}'";
    }

    private void ExecuteDeleteEntity(EntityItem? entity)
    {
        if (entity == null) return;
        
        _logger.LogInformation("Deleting entity: {EntityName}", entity.Name);
        Entities.Remove(entity);
        
        if (SelectedEntity == entity)
        {
            SelectedEntity = null;
        }
        
        StatusMessage = $"Entity '{entity.Name}' deleted";
    }

    private void LoadSampleEntities()
    {
        Entities.Clear();
        
        Entities.Add(new EntityItem("AI Assistant", "Conversational", "Active", 
            new Dictionary<string, string> { { "Model", "GPT-4" }, { "Language", "Multi" }, { "Context", "General" } }, 
            DateTime.Now.AddDays(-5)));
            
        Entities.Add(new EntityItem("Code Reviewer", "Specialized", "Active", 
            new Dictionary<string, string> { { "Domain", "Software" }, { "Languages", "C#, Python, JS" }, { "Experience", "Senior" } }, 
            DateTime.Now.AddDays(-10)));
            
        Entities.Add(new EntityItem("Data Analyst", "Analytics", "Inactive", 
            new Dictionary<string, string> { { "Focus", "Statistics" }, { "Tools", "SQL, Python" }, { "Level", "Expert" } }, 
            DateTime.Now.AddDays(-2)));
            
        Entities.Add(new EntityItem("Creative Writer", "Content", "Active", 
            new Dictionary<string, string> { { "Genre", "Technical" }, { "Style", "Professional" }, { "Tone", "Informative" } }, 
            DateTime.Now.AddDays(-1)));
    }
}

public class EntityItem
{
    public EntityItem(string name, string type, string status, Dictionary<string, string> properties, DateTime createdDate)
    {
        Name = name;
        Type = type;
        Status = status;
        Properties = properties;
        CreatedDate = createdDate;
    }

    public string Name { get; }
    public string Type { get; }
    public string Status { get; }
    public Dictionary<string, string> Properties { get; }
    public DateTime CreatedDate { get; }
}