using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Training section
/// </summary>
public partial class TrainingViewModel : BaseViewModel
{
    private readonly ILogger<TrainingViewModel> _logger;
    private TrainingConfigItem? _selectedConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public TrainingViewModel(ILogger<TrainingViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Training";
        StatusMessage = "Advanced training tools coming soon";
        
        TrainingConfigs = new ObservableCollection<TrainingConfigItem>();
        
        StartTrainingCommand = new AsyncRelayCommand<TrainingConfigItem>(StartTrainingAsync);
        CreateConfigCommand = new RelayCommand(ExecuteCreateConfig);
        StopTrainingCommand = new RelayCommand<TrainingConfigItem>(ExecuteStopTraining);
        
        LoadSampleConfigs();
        
        _logger.LogInformation("Training view model initialized");
    }

    public string Title { get; }
    
    public TrainingConfigItem? SelectedConfig
    {
        get => _selectedConfig;
        set => SetProperty(ref _selectedConfig, value);
    }

    public ObservableCollection<TrainingConfigItem> TrainingConfigs { get; }
    public IAsyncRelayCommand<TrainingConfigItem> StartTrainingCommand { get; }
    public IRelayCommand CreateConfigCommand { get; }
    public IRelayCommand<TrainingConfigItem> StopTrainingCommand { get; }

    private async Task StartTrainingAsync(TrainingConfigItem? config)
    {
        if (config == null) return;
        
        try
        {
            SetBusyState(true, $"Starting training for '{config.Name}'...");
            _logger.LogInformation("Starting training: {ConfigName}", config.Name);

            await Task.Delay(2000);
            
            SetBusyState(false, $"Training started for '{config.Name}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training");
            SetBusyState(false, "Failed to start training");
        }
    }

    private void ExecuteCreateConfig()
    {
        _logger.LogInformation("Creating new training configuration");
        
        var newConfig = new TrainingConfigItem(
            $"Config_{DateTime.Now:HHmmss}",
            "Custom",
            "Pending",
            0.001,
            100,
            32,
            DateTime.Now
        );
        
        TrainingConfigs.Insert(0, newConfig);
        SelectedConfig = newConfig;
        StatusMessage = $"Training configuration '{newConfig.Name}' created";
    }

    private void ExecuteStopTraining(TrainingConfigItem? config)
    {
        if (config == null) return;
        
        _logger.LogInformation("Stopping training: {ConfigName}", config.Name);
        StatusMessage = $"Stopping training for '{config.Name}'";
    }

    private void LoadSampleConfigs()
    {
        TrainingConfigs.Clear();
        
        TrainingConfigs.Add(new TrainingConfigItem("GPT Fine-tune", "Language Model", "Running", 0.0001, 50, 16, DateTime.Now.AddHours(-2)));
        TrainingConfigs.Add(new TrainingConfigItem("Vision Classifier", "Image Classification", "Completed", 0.001, 200, 32, DateTime.Now.AddDays(-1)));
        TrainingConfigs.Add(new TrainingConfigItem("Custom Transformer", "Transformer", "Pending", 0.0005, 100, 24, DateTime.Now.AddMinutes(-15)));
        TrainingConfigs.Add(new TrainingConfigItem("Audio Processor", "Audio Processing", "Failed", 0.002, 75, 64, DateTime.Now.AddHours(-6)));
    }
}

public class TrainingConfigItem
{
    public TrainingConfigItem(string name, string modelType, string status, double learningRate, int epochs, int batchSize, DateTime createdDate)
    {
        Name = name;
        ModelType = modelType;
        Status = status;
        LearningRate = learningRate;
        Epochs = epochs;
        BatchSize = batchSize;
        CreatedDate = createdDate;
    }

    public string Name { get; }
    public string ModelType { get; }
    public string Status { get; }
    public double LearningRate { get; }
    public int Epochs { get; }
    public int BatchSize { get; }
    public DateTime CreatedDate { get; }
}