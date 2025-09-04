using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Datasets section
/// </summary>
public partial class DatasetsViewModel : BaseViewModel
{
    private readonly ILogger<DatasetsViewModel> _logger;
    private DatasetItem? _selectedDataset;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetsViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public DatasetsViewModel(ILogger<DatasetsViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Datasets";
        StatusMessage = "Dataset management coming soon";
        
        // Initialize collections
        Datasets = new ObservableCollection<DatasetItem>();
        
        // Initialize commands
        UploadDatasetCommand = new AsyncRelayCommand(UploadDatasetAsync);
        DeleteDatasetCommand = new RelayCommand<DatasetItem>(ExecuteDeleteDataset);
        
        // Load sample data
        LoadSampleDatasets();
        
        _logger.LogInformation("Datasets view model initialized");
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the selected dataset
    /// </summary>
    public DatasetItem? SelectedDataset
    {
        get => _selectedDataset;
        set => SetProperty(ref _selectedDataset, value);
    }

    /// <summary>
    /// Gets the collection of datasets
    /// </summary>
    public ObservableCollection<DatasetItem> Datasets { get; }

    /// <summary>
    /// Gets the upload dataset command
    /// </summary>
    public IAsyncRelayCommand UploadDatasetCommand { get; }

    /// <summary>
    /// Gets the delete dataset command
    /// </summary>
    public IRelayCommand<DatasetItem> DeleteDatasetCommand { get; }

    /// <summary>
    /// Uploads a new dataset
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task UploadDatasetAsync()
    {
        try
        {
            SetBusyState(true, "Uploading dataset...");
            _logger.LogInformation("Uploading new dataset");

            // Simulate upload process
            await Task.Delay(2000);

            var newDataset = new DatasetItem(
                $"Dataset {DateTime.Now:HHmmss}",
                "Mixed",
                Random.Shared.Next(1000, 50000),
                (Random.Shared.NextDouble() * 500).ToString("F1") + " MB",
                DateTime.Now
            );

            Datasets.Insert(0, newDataset);
            SelectedDataset = newDataset;
            
            SetBusyState(false, "Dataset uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset");
            SetBusyState(false, "Failed to upload dataset");
        }
    }

    /// <summary>
    /// Executes the delete dataset command
    /// </summary>
    /// <param name="dataset">The dataset to delete</param>
    private void ExecuteDeleteDataset(DatasetItem? dataset)
    {
        if (dataset == null) return;
        
        _logger.LogInformation("Deleting dataset: {DatasetName}", dataset.Name);
        Datasets.Remove(dataset);
        
        if (SelectedDataset == dataset)
        {
            SelectedDataset = null;
        }
        
        StatusMessage = $"Dataset '{dataset.Name}' deleted";
    }

    /// <summary>
    /// Loads sample dataset data
    /// </summary>
    private void LoadSampleDatasets()
    {
        Datasets.Clear();
        
        Datasets.Add(new DatasetItem("CIFAR-10", "Image", 60000, "163.0 MB", DateTime.Now.AddDays(-5)));
        Datasets.Add(new DatasetItem("Common Voice", "Audio", 15000, "2.3 GB", DateTime.Now.AddDays(-10)));
        Datasets.Add(new DatasetItem("WikiText-103", "Text", 28000, "183.0 MB", DateTime.Now.AddDays(-2)));
        Datasets.Add(new DatasetItem("Custom Images", "Image", 5000, "1.2 GB", DateTime.Now.AddDays(-1)));
        Datasets.Add(new DatasetItem("Medical Scans", "Image", 12000, "4.8 GB", DateTime.Now.AddDays(-7)));
    }
}

/// <summary>
/// Represents a dataset item
/// </summary>
public class DatasetItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetItem"/> class
    /// </summary>
    /// <param name="name">The dataset name</param>
    /// <param name="type">The dataset type</param>
    /// <param name="sampleCount">The number of samples</param>
    /// <param name="size">The dataset size</param>
    /// <param name="uploadDate">When the dataset was uploaded</param>
    public DatasetItem(string name, string type, int sampleCount, string size, DateTime uploadDate)
    {
        Name = name;
        Type = type;
        SampleCount = sampleCount;
        Size = size;
        UploadDate = uploadDate;
    }

    /// <summary>
    /// Gets the dataset name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the dataset type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the number of samples
    /// </summary>
    public int SampleCount { get; }

    /// <summary>
    /// Gets the dataset size
    /// </summary>
    public string Size { get; }

    /// <summary>
    /// Gets when the dataset was uploaded
    /// </summary>
    public DateTime UploadDate { get; }
}