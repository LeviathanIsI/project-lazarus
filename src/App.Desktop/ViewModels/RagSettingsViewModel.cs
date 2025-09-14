using System.Windows.Input;
using Lazarus.Shared.Settings;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for RAG/Embeddings settings section
/// </summary>
public class RagSettingsViewModel : SettingsSectionBase
{
    // Core properties
    private string _vectorDbType = "SQLite";
    private int _chunkSize = 512;
    private int _overlapSize = 50;
    private double _similarityThreshold = 0.7;
    private bool _enableVectorStore = true;
    private string _databasePath = "";
    private bool _useSQLiteVss;
    private string _storageEngine = "SQLite";

    // Embedding Model properties
    private string _embeddingProvider = "OpenAI";
    private string _embeddingModel = "text-embedding-3-large";
    private int _embeddingDimensions = 1536;

    // Document Processing properties
    private string _chunkingMethod = "Fixed Size";
    private int _chunkOverlap = 100;

    // Retrieval Settings
    private string _searchType = "Cosine Similarity";
    private int _topKResults = 5;
    private bool _enableReranking;

    // Data Sources
    private string _documentDirectory = "";
    private bool _supportPdf = true;
    private bool _supportDocx = true;
    private bool _supportTxt = true;
    private bool _supportMarkdown = true;
    private bool _supportHtml = true;
    private bool _supportCsv;

    // Advanced Settings
    private bool _extractMetadata = true;
    private bool _useMetadataFiltering;
    private bool _indexMetadata;
    private int _indexingBatchSize = 10;
    private bool _cacheEmbeddings = true;
    private bool _cacheSearchResults;

    public RagSettingsViewModel(SettingsViewModel settings) : base(settings, "RAG/Embeddings")
    {
        SectionDescription = "Configure retrieval-augmented generation and vector embeddings";
        BrowseDatabaseCommand = new RelayCommand(BrowseDatabase);
        BrowseDocumentDirectoryCommand = new RelayCommand(BrowseDocumentDirectory);
        IndexDocumentsCommand = new RelayCommand(IndexDocuments);
        ClearIndexCommand = new RelayCommand(ClearIndex);
        TestRetrievalCommand = new RelayCommand(TestRetrieval);
        ResetToDefaultCommand = new RelayCommand(() => { ResetToDefault(); OnPropertyChanged(""); });

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    // Embedding Model Properties
    public string EmbeddingProvider
    {
        get => _embeddingProvider;
        set { if (SetProperty(ref _embeddingProvider, value)) MarkAsChanged(); }
    }

    public string EmbeddingModel
    {
        get => _embeddingModel;
        set { if (SetProperty(ref _embeddingModel, value)) MarkAsChanged(); }
    }

    public int EmbeddingDimensions
    {
        get => _embeddingDimensions;
        set { if (SetProperty(ref _embeddingDimensions, value)) MarkAsChanged(); }
    }

    // Vector Database Properties
    public string VectorDbType
    {
        get => _vectorDbType;
        set { if (SetProperty(ref _vectorDbType, value)) MarkAsChanged(); }
    }

    public bool RagEnableVectorStore
    {
        get => _enableVectorStore;
        set { if (SetProperty(ref _enableVectorStore, value)) MarkAsChanged(); }
    }

    public string RagStorageEngine
    {
        get => _storageEngine;
        set { if (SetProperty(ref _storageEngine, value)) MarkAsChanged(); }
    }

    public string RagDatabasePath
    {
        get => _databasePath;
        set { if (SetProperty(ref _databasePath, value)) MarkAsChanged(); }
    }

    // Document Processing Properties
    public string ChunkingMethod
    {
        get => _chunkingMethod;
        set { if (SetProperty(ref _chunkingMethod, value)) MarkAsChanged(); }
    }

    public int ChunkSize
    {
        get => _chunkSize;
        set { if (SetProperty(ref _chunkSize, value)) MarkAsChanged(); }
    }

    public int RagDocumentChunkTokens
    {
        get => _chunkSize;
        set { if (SetProperty(ref _chunkSize, value)) { ChunkSize = value; MarkAsChanged(); } }
    }

    public int ChunkOverlap
    {
        get => _chunkOverlap;
        set { if (SetProperty(ref _chunkOverlap, value)) MarkAsChanged(); }
    }

    public int OverlapSize
    {
        get => _overlapSize;
        set { if (SetProperty(ref _overlapSize, value)) MarkAsChanged(); }
    }

    // Retrieval Settings Properties
    public string SearchType
    {
        get => _searchType;
        set { if (SetProperty(ref _searchType, value)) MarkAsChanged(); }
    }

    public int TopKResults
    {
        get => _topKResults;
        set { if (SetProperty(ref _topKResults, value)) MarkAsChanged(); }
    }

    public double SimilarityThreshold
    {
        get => _similarityThreshold;
        set { if (SetProperty(ref _similarityThreshold, value)) MarkAsChanged(); }
    }

    public double RagSimilarityThreshold
    {
        get => _similarityThreshold;
        set { if (SetProperty(ref _similarityThreshold, value)) { SimilarityThreshold = value; MarkAsChanged(); } }
    }

    public bool EnableReranking
    {
        get => _enableReranking;
        set { if (SetProperty(ref _enableReranking, value)) MarkAsChanged(); }
    }

    public bool RagUseSQLiteVss
    {
        get => _useSQLiteVss;
        set { if (SetProperty(ref _useSQLiteVss, value)) MarkAsChanged(); }
    }

    // Data Sources Properties
    public string DocumentDirectory
    {
        get => _documentDirectory;
        set { if (SetProperty(ref _documentDirectory, value)) MarkAsChanged(); }
    }

    public bool SupportPdf
    {
        get => _supportPdf;
        set { if (SetProperty(ref _supportPdf, value)) MarkAsChanged(); }
    }

    public bool SupportDocx
    {
        get => _supportDocx;
        set { if (SetProperty(ref _supportDocx, value)) MarkAsChanged(); }
    }

    public bool SupportTxt
    {
        get => _supportTxt;
        set { if (SetProperty(ref _supportTxt, value)) MarkAsChanged(); }
    }

    public bool SupportMarkdown
    {
        get => _supportMarkdown;
        set { if (SetProperty(ref _supportMarkdown, value)) MarkAsChanged(); }
    }

    public bool SupportHtml
    {
        get => _supportHtml;
        set { if (SetProperty(ref _supportHtml, value)) MarkAsChanged(); }
    }

    public bool SupportCsv
    {
        get => _supportCsv;
        set { if (SetProperty(ref _supportCsv, value)) MarkAsChanged(); }
    }

    // Advanced Settings Properties
    public bool ExtractMetadata
    {
        get => _extractMetadata;
        set { if (SetProperty(ref _extractMetadata, value)) MarkAsChanged(); }
    }

    public bool UseMetadataFiltering
    {
        get => _useMetadataFiltering;
        set { if (SetProperty(ref _useMetadataFiltering, value)) MarkAsChanged(); }
    }

    public bool IndexMetadata
    {
        get => _indexMetadata;
        set { if (SetProperty(ref _indexMetadata, value)) MarkAsChanged(); }
    }

    public int IndexingBatchSize
    {
        get => _indexingBatchSize;
        set { if (SetProperty(ref _indexingBatchSize, value)) MarkAsChanged(); }
    }

    public bool CacheEmbeddings
    {
        get => _cacheEmbeddings;
        set { if (SetProperty(ref _cacheEmbeddings, value)) MarkAsChanged(); }
    }

    public bool CacheSearchResults
    {
        get => _cacheSearchResults;
        set { if (SetProperty(ref _cacheSearchResults, value)) MarkAsChanged(); }
    }

    // Commands
    public ICommand BrowseDatabaseCommand { get; }
    public ICommand BrowseRagDatabaseCommand => BrowseDatabaseCommand; // Alias for existing view
    public ICommand BrowseDocumentDirectoryCommand { get; }
    public ICommand IndexDocumentsCommand { get; }
    public ICommand ClearIndexCommand { get; }
    public ICommand TestRetrievalCommand { get; }
    public new ICommand ResetToDefaultCommand { get; }

    private void BrowseDatabase()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Database Directory",
            FileName = "Database Folder",
            Filter = "Directory|*.this.directory",
            CheckFileExists = false,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == true)
        {
            var path = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(path))
                RagDatabasePath = path;
        }
    }

    private void BrowseDocumentDirectory()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Document Directory",
            FileName = "Document Folder",
            Filter = "Directory|*.this.directory",
            CheckFileExists = false,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == true)
        {
            var path = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(path))
                DocumentDirectory = path;
        }
    }

    private void IndexDocuments()
    {
        // TODO: Implement document indexing
    }

    private void ClearIndex()
    {
        // TODO: Implement index clearing
    }

    private void TestRetrieval()
    {
        // TODO: Implement retrieval testing
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;

        // Load saved settings
        VectorDbType = settings.VectorDbType;
        ChunkSize = settings.ChunkSize;
        OverlapSize = settings.OverlapSize;
        SimilarityThreshold = settings.SimilarityThreshold;

        // Initialize all properties with defaults
        EmbeddingProvider = "OpenAI";
        EmbeddingModel = "text-embedding-3-large";
        EmbeddingDimensions = 1536;

        RagEnableVectorStore = true;
        RagDatabasePath = System.IO.Path.Combine(SettingsPaths.DatabaseDirectory, "vectors.db");
        RagUseSQLiteVss = VectorDbType == "SQLite-VSS";
        RagStorageEngine = VectorDbType;

        ChunkingMethod = "Fixed Size";
        ChunkOverlap = 100;

        SearchType = "Cosine Similarity";
        TopKResults = 5;
        EnableReranking = false;

        DocumentDirectory = System.IO.Path.Combine(SettingsPaths.AppDataRoot, "documents");
        SupportPdf = true;
        SupportDocx = true;
        SupportTxt = true;
        SupportMarkdown = true;
        SupportHtml = true;
        SupportCsv = false;

        ExtractMetadata = true;
        UseMetadataFiltering = false;
        IndexMetadata = false;
        IndexingBatchSize = 10;
        CacheEmbeddings = true;
        CacheSearchResults = false;

        // Fix empty values
        if (string.IsNullOrEmpty(VectorDbType)) VectorDbType = "SQLite";
        if (ChunkSize == 0) ChunkSize = 512;
        if (OverlapSize == 0) OverlapSize = 50;
        if (SimilarityThreshold == 0) SimilarityThreshold = 0.7;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.VectorDbType = VectorDbType;
        settings.ChunkSize = ChunkSize;
        settings.OverlapSize = OverlapSize;
        settings.SimilarityThreshold = SimilarityThreshold;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        // Embedding Model defaults
        EmbeddingProvider = "OpenAI";
        EmbeddingModel = "text-embedding-3-large";
        EmbeddingDimensions = 1536;

        // Vector Database defaults
        VectorDbType = "SQLite";
        RagEnableVectorStore = true;
        RagStorageEngine = "SQLite";
        try
        {
            RagDatabasePath = System.IO.Path.Combine(SettingsPaths.DatabaseDirectory, "vectors.db");
        }
        catch
        {
            RagDatabasePath = @"C:\Lazarus\Database\vectors.db";
        }

        // Document Processing defaults
        ChunkingMethod = "Fixed Size";
        ChunkSize = 512;
        ChunkOverlap = 100;
        OverlapSize = 50;

        // Retrieval Settings defaults
        SearchType = "Cosine Similarity";
        TopKResults = 5;
        SimilarityThreshold = 0.7;
        EnableReranking = false;
        RagUseSQLiteVss = false;

        // Data Sources defaults
        try
        {
            DocumentDirectory = System.IO.Path.Combine(SettingsPaths.AppDataRoot, "documents");
        }
        catch
        {
            DocumentDirectory = @"C:\Lazarus\Documents";
        }
        SupportPdf = true;
        SupportDocx = true;
        SupportTxt = true;
        SupportMarkdown = true;
        SupportHtml = true;
        SupportCsv = false;

        // Advanced Settings defaults
        ExtractMetadata = true;
        UseMetadataFiltering = false;
        IndexMetadata = false;
        IndexingBatchSize = 10;
        CacheEmbeddings = true;
        CacheSearchResults = false;
    }
}