using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Desktop.Services;
using Lazarus.Shared.Utilities;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Datasets and RAG Sources management ViewModel
/// Essential for any serious LLM platform - knowledge base management is critical for RAG workflows
/// </summary>
public class DatasetsViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navigationService;
    
    public ObservableCollection<DatasetViewModel> Datasets { get; } = new();
    public ObservableCollection<DataSourceViewModel> RagSources { get; } = new();
    public ObservableCollection<EmbeddingIndexViewModel> EmbeddingIndexes { get; } = new();
    
    private bool _isLoading;
    private bool _hasDatasets;
    private bool _hasRagSources;
    private bool _hasEmbeddingIndexes;
    private string _selectedDatasetPath = string.Empty;
    private DatasetViewModel? _selectedDataset;
    
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool HasDatasets
    {
        get => _hasDatasets;
        private set
        {
            if (_hasDatasets != value)
            {
                _hasDatasets = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }
    
    public bool HasRagSources
    {
        get => _hasRagSources;
        private set
        {
            if (_hasRagSources != value)
            {
                _hasRagSources = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool HasEmbeddingIndexes
    {
        get => _hasEmbeddingIndexes;
        private set
        {
            if (_hasEmbeddingIndexes != value)
            {
                _hasEmbeddingIndexes = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string SelectedDatasetPath
    {
        get => _selectedDatasetPath;
        set
        {
            if (_selectedDatasetPath != value)
            {
                _selectedDatasetPath = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DatasetViewModel? SelectedDataset
    {
        get => _selectedDataset;
        set
        {
            if (_selectedDataset != value)
            {
                _selectedDataset = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool ShowEmptyState => !HasDatasets && !HasRagSources && !HasEmbeddingIndexes && !IsLoading;
    
    // Commands
    public ICommand ImportDatasetCommand { get; }
    public ICommand CreateDatasetCommand { get; }
    public ICommand AddDocumentSourceCommand { get; }
    public ICommand ScanDirectoryCommand { get; }
    public ICommand CreateEmbeddingIndexCommand { get; }
    public ICommand RefreshDatasetsCommand { get; }
    public ICommand BrowseDatasetPathCommand { get; }
    
    public DatasetsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        // Initialize commands
        ImportDatasetCommand = new RelayCommand(_ => ImportDataset());
        CreateDatasetCommand = new RelayCommand(_ => CreateDataset());
        AddDocumentSourceCommand = new RelayCommand(_ => AddDocumentSource());
        ScanDirectoryCommand = new RelayCommand(_ => ScanDirectory());
        CreateEmbeddingIndexCommand = new RelayCommand(_ => CreateEmbeddingIndex());
        RefreshDatasetsCommand = new RelayCommand(_ => RefreshDatasets());
        BrowseDatasetPathCommand = new RelayCommand(_ => BrowseDatasetPath());
        
        // Initialize with sample data
        InitializeSampleData();
        
        Console.WriteLine("DatasetsViewModel: Initialized knowledge base management system");
    }
    
    private void InitializeSampleData()
    {
        // Add sample datasets
        Datasets.Add(new DatasetViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Programming Documentation",
            Description = "Technical documentation and API references for software development",
            Type = DatasetType.Documents,
            DocumentCount = 1247,
            SizeInMB = 89.3,
            LastModified = DateTime.Now.AddDays(-3),
            EmbeddingModel = "text-embedding-3-small",
            Status = DatasetStatus.Ready
        });
        
        Datasets.Add(new DatasetViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Research Papers",
            Description = "Academic papers on machine learning and AI research",
            Type = DatasetType.Research,
            DocumentCount = 532,
            SizeInMB = 156.7,
            LastModified = DateTime.Now.AddDays(-1),
            EmbeddingModel = "text-embedding-3-large",
            Status = DatasetStatus.Processing
        });
        
        // Add sample RAG sources
        RagSources.Add(new DataSourceViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Local Knowledge Base",
            Type = DataSourceType.LocalDirectory,
            Path = @"C:\Users\Documents\KnowledgeBase",
            FileCount = 89,
            LastScanned = DateTime.Now.AddHours(-6),
            Status = DataSourceStatus.Active
        });
        
        RagSources.Add(new DataSourceViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Web Scraping Results",
            Type = DataSourceType.WebScraping,
            Path = "https://docs.example.com",
            FileCount = 234,
            LastScanned = DateTime.Now.AddDays(-2),
            Status = DataSourceStatus.NeedsUpdate
        });
        
        // Add sample embedding indexes
        EmbeddingIndexes.Add(new EmbeddingIndexViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "main-knowledge-index",
            EmbeddingModel = "text-embedding-3-small",
            VectorCount = 15432,
            Dimensions = 1536,
            SizeInMB = 234.5,
            LastUpdated = DateTime.Now.AddHours(-12),
            Status = EmbeddingIndexStatus.Ready
        });
        
        UpdateCounts();
    }
    
    private void UpdateCounts()
    {
        HasDatasets = Datasets.Count > 0;
        HasRagSources = RagSources.Count > 0;
        HasEmbeddingIndexes = EmbeddingIndexes.Count > 0;
        OnPropertyChanged(nameof(ShowEmptyState));
    }
    
    private void ImportDataset()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Import Dataset",
            Filter = "Dataset files (*.jsonl;*.csv;*.parquet;*.txt)|*.jsonl;*.csv;*.parquet;*.txt|All files (*.*)|*.*",
            Multiselect = true
        };

        if (ofd.ShowDialog() == true)
        {
            foreach (var file in ofd.FileNames)
            {
                try
                {
                    var info = new FileInfo(file);
                    var dataset = new DatasetViewModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = Path.GetFileNameWithoutExtension(info.Name),
                        Description = $"Imported {info.Extension.ToUpperInvariant()} dataset",
                        Type = GetDatasetTypeFromExtension(info.Extension),
                        DocumentCount = 0,
                        SizeInMB = Math.Round(info.Length / 1024d / 1024d, 2),
                        LastModified = info.LastWriteTime,
                        Status = DatasetStatus.Ready
                    };

                    Datasets.Add(dataset);
                    Console.WriteLine($"DatasetsViewModel: Imported dataset - {dataset.Name} ({dataset.Type})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DatasetsViewModel: Failed to import '{file}': {ex.Message}");
                }
            }

            UpdateCounts();
        }
    }
    
    private void CreateDataset()
    {
        var dataset = new DatasetViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "New Dataset",
            Description = "User created dataset",
            Type = DatasetType.Custom,
            DocumentCount = 0,
            SizeInMB = 0,
            LastModified = DateTime.Now,
            Status = DatasetStatus.Empty
        };
        
        Datasets.Add(dataset);
        SelectedDataset = dataset;
        UpdateCounts();
        Console.WriteLine($"DatasetsViewModel: Created new dataset - {dataset.Name}");
    }
    
    private void AddDocumentSource()
    {
        var source = new DataSourceViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "New Document Source",
            Type = DataSourceType.LocalDirectory,
            Path = SelectedDatasetPath,
            FileCount = 0,
            LastScanned = DateTime.Now,
            Status = DataSourceStatus.Scanning
        };
        
        RagSources.Add(source);
        UpdateCounts();
        Console.WriteLine($"DatasetsViewModel: Added document source - {source.Name}");
    }
    
    private void ScanDirectory()
    {
        if (!string.IsNullOrEmpty(SelectedDatasetPath) && Directory.Exists(SelectedDatasetPath))
        {
            // TODO: Implement actual directory scanning
            var fileCount = Directory.GetFiles(SelectedDatasetPath, "*.*", SearchOption.AllDirectories).Length;
            Console.WriteLine($"DatasetsViewModel: Scanned directory {SelectedDatasetPath} - found {fileCount} files");
        }
    }
    
    private void CreateEmbeddingIndex()
    {
        var index = new EmbeddingIndexViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "new-embedding-index",
            EmbeddingModel = "text-embedding-3-small",
            VectorCount = 0,
            Dimensions = 1536,
            SizeInMB = 0,
            LastUpdated = DateTime.Now,
            Status = EmbeddingIndexStatus.Creating
        };
        
        EmbeddingIndexes.Add(index);
        UpdateCounts();
        Console.WriteLine($"DatasetsViewModel: Creating embedding index - {index.Name}");
    }
    
    private async void RefreshDatasets()
    {
        IsLoading = true;

        try
        {
            // Simulate dataset preparation for the currently selected dataset if present
            if (SelectedDataset != null)
            {
                SelectedDataset.Status = DatasetStatus.Processing;
            }

            await Task.Delay(1000);

            if (SelectedDataset != null)
            {
                SelectedDataset.Status = DatasetStatus.Ready;
                SelectedDataset.LastModified = DateTime.Now;
            }

            Console.WriteLine("DatasetsViewModel: Refreshed datasets and RAG sources");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void BrowseDatasetPath()
    {
        // Use file dialog to pick a file and derive the folder path (more reliable cross-target)
        var ofd = new OpenFileDialog
        {
            Title = "Select a file in the dataset folder",
            Filter = "All files (*.*)|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(ofd.FileName) ?? string.Empty;
            SelectedDatasetPath = dir;
            Console.WriteLine($"DatasetsViewModel: Selected path - {SelectedDatasetPath}");
        }
    }

    private static DatasetType GetDatasetTypeFromExtension(string extension)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".jsonl":
                return DatasetType.Training;
            case ".csv":
                return DatasetType.Documents;
            case ".parquet":
                return DatasetType.Research;
            case ".txt":
                return DatasetType.Custom;
            default:
                return DatasetType.Custom;
        }
    }
    
    #region INotifyPropertyChanged
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    #endregion
}

/// <summary>
/// Individual dataset ViewModel
/// </summary>
public class DatasetViewModel : INotifyPropertyChanged
{
    private DatasetStatus _status;
    private string _statusMessage = string.Empty;
    
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DatasetType Type { get; set; }
    public int DocumentCount { get; set; }
    public double SizeInMB { get; set; }
    public DateTime LastModified { get; set; }
    public string EmbeddingModel { get; set; } = string.Empty;
    
    public DatasetStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsProcessing));
            }
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string StatusText => Status switch
    {
        DatasetStatus.Empty => "Empty",
        DatasetStatus.Processing => "Processing",
        DatasetStatus.Ready => "Ready",
        DatasetStatus.Error => "Error",
        _ => "Unknown"
    };
    
    public string TypeIcon => Type switch
    {
        DatasetType.Documents => "📄",
        DatasetType.Research => "🔬",
        DatasetType.Training => "🎯",
        DatasetType.Custom => "📁",
        _ => "📊"
    };
    
    public string SizeText => SizeInMB < 1 ? $"{SizeInMB * 1024:F0} KB" : $"{SizeInMB:F1} MB";
    public bool IsProcessing => Status == DatasetStatus.Processing;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Data source ViewModel for RAG integration
/// </summary>
public class DataSourceViewModel : INotifyPropertyChanged
{
    private DataSourceStatus _status;
    
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public DateTime LastScanned { get; set; }
    
    public DataSourceStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }
    
    public string StatusText => Status switch
    {
        DataSourceStatus.Active => "Active",
        DataSourceStatus.Scanning => "Scanning",
        DataSourceStatus.NeedsUpdate => "Needs Update",
        DataSourceStatus.Error => "Error",
        _ => "Unknown"
    };
    
    public string TypeIcon => Type switch
    {
        DataSourceType.LocalDirectory => "📁",
        DataSourceType.WebScraping => "🌐",
        DataSourceType.Database => "🗄️",
        DataSourceType.Api => "🔌",
        _ => "📊"
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Embedding index ViewModel
/// </summary>
public class EmbeddingIndexViewModel : INotifyPropertyChanged
{
    private EmbeddingIndexStatus _status;
    
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int VectorCount { get; set; }
    public int Dimensions { get; set; }
    public double SizeInMB { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public EmbeddingIndexStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }
    
    public string StatusText => Status switch
    {
        EmbeddingIndexStatus.Ready => "Ready",
        EmbeddingIndexStatus.Creating => "Creating",
        EmbeddingIndexStatus.Updating => "Updating",
        EmbeddingIndexStatus.Error => "Error",
        _ => "Unknown"
    };
    
    public string SizeText => SizeInMB < 1 ? $"{SizeInMB * 1024:F0} KB" : $"{SizeInMB:F1} MB";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Enums
public enum DatasetType
{
    Documents,
    Research,
    Training,
    Custom
}

public enum DatasetStatus
{
    Empty,
    Processing,
    Ready,
    Error
}

public enum DataSourceType
{
    LocalDirectory,
    WebScraping,
    Database,
    Api
}

public enum DataSourceStatus
{
    Active,
    Scanning,
    NeedsUpdate,
    Error
}

public enum EmbeddingIndexStatus
{
    Ready,
    Creating,
    Updating,
    Error
}