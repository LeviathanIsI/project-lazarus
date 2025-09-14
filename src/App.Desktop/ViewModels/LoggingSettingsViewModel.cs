using System.Windows.Input;
using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for Logging settings section
/// </summary>
public class LoggingSettingsViewModel : SettingsSectionBase
{
    // General Settings
    private bool _enableLogging = true;
    private string _logLevel = "Information";
    private string _logFormat = "PlainText";

    // File Logging
    private bool _logToFile = true;
    private string _logDirectory = "";
    private int _maxFileSize = 10;
    private string _fileRotation = "Daily";
    private int _retentionDays = 30;

    // Performance Logging
    private bool _logPerformanceMetrics;
    private bool _logModelInference;
    private bool _logApiCalls;

    // Debug & Diagnostics
    private bool _enableVerboseLogging;
    private bool _logStackTraces = true;
    private bool _logSensitiveData;
    private bool _logToConsole = true;

    // Crash Reporting
    private bool _sendCrashReports;
    private bool _includeSystemInfo = true;
    private bool _createLocalDumps = true;

    // Legacy properties for compatibility
    private int _maxLogSizeMB;
    private int _logRetentionDays;
    private bool _consoleOutput;
    private bool _enableStructured;

    public LoggingSettingsViewModel(SettingsViewModel settings) : base(settings, "Logging")
    {
        SectionDescription = "Configure application logging and diagnostics";
        BrowseLogDirectoryCommand = new RelayCommand(BrowseLogDirectory);
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        ClearLogsCommand = new RelayCommand(ClearLogs);
        ResetToDefaultCommand = new RelayCommand(() => { ResetToDefault(); OnPropertyChanged(""); });

        // Initialize with default values immediately
        ResetToDefault();
        CompleteInitialization();
    }

    // General Settings Properties
    public bool EnableLogging
    {
        get => _enableLogging;
        set { if (SetProperty(ref _enableLogging, value)) MarkAsChanged(); }
    }

    public string LogLevel
    {
        get => _logLevel;
        set { if (SetProperty(ref _logLevel, value)) MarkAsChanged(); }
    }

    public string LogFormat
    {
        get => _logFormat;
        set { if (SetProperty(ref _logFormat, value)) MarkAsChanged(); }
    }

    // File Logging Properties
    public bool LogToFile
    {
        get => _logToFile;
        set { if (SetProperty(ref _logToFile, value)) MarkAsChanged(); }
    }

    public string LogDirectory
    {
        get => _logDirectory;
        set { if (SetProperty(ref _logDirectory, value)) MarkAsChanged(); }
    }

    public int MaxFileSize
    {
        get => _maxFileSize;
        set { if (SetProperty(ref _maxFileSize, value)) MarkAsChanged(); }
    }

    public string FileRotation
    {
        get => _fileRotation;
        set { if (SetProperty(ref _fileRotation, value)) MarkAsChanged(); }
    }

    public int RetentionDays
    {
        get => _retentionDays;
        set { if (SetProperty(ref _retentionDays, value)) MarkAsChanged(); }
    }

    // Performance Logging Properties
    public bool LogPerformanceMetrics
    {
        get => _logPerformanceMetrics;
        set { if (SetProperty(ref _logPerformanceMetrics, value)) MarkAsChanged(); }
    }

    public bool LogModelInference
    {
        get => _logModelInference;
        set { if (SetProperty(ref _logModelInference, value)) MarkAsChanged(); }
    }

    public bool LogApiCalls
    {
        get => _logApiCalls;
        set { if (SetProperty(ref _logApiCalls, value)) MarkAsChanged(); }
    }

    // Debug & Diagnostics Properties
    public bool EnableVerboseLogging
    {
        get => _enableVerboseLogging;
        set { if (SetProperty(ref _enableVerboseLogging, value)) MarkAsChanged(); }
    }

    public bool LogStackTraces
    {
        get => _logStackTraces;
        set { if (SetProperty(ref _logStackTraces, value)) MarkAsChanged(); }
    }

    public bool LogSensitiveData
    {
        get => _logSensitiveData;
        set { if (SetProperty(ref _logSensitiveData, value)) MarkAsChanged(); }
    }

    public bool LogToConsole
    {
        get => _logToConsole;
        set { if (SetProperty(ref _logToConsole, value)) MarkAsChanged(); }
    }

    // Crash Reporting Properties
    public bool SendCrashReports
    {
        get => _sendCrashReports;
        set { if (SetProperty(ref _sendCrashReports, value)) MarkAsChanged(); }
    }

    public bool IncludeSystemInfo
    {
        get => _includeSystemInfo;
        set { if (SetProperty(ref _includeSystemInfo, value)) MarkAsChanged(); }
    }

    public bool CreateLocalDumps
    {
        get => _createLocalDumps;
        set { if (SetProperty(ref _createLocalDumps, value)) MarkAsChanged(); }
    }

    // Legacy properties for backward compatibility
    public int MaxLogSizeMB
    {
        get => _maxLogSizeMB;
        set { if (SetProperty(ref _maxLogSizeMB, value)) { MaxFileSize = value; MarkAsChanged(); } }
    }

    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set { if (SetProperty(ref _logRetentionDays, value)) { RetentionDays = value; MarkAsChanged(); } }
    }

    public bool ConsoleOutput
    {
        get => _consoleOutput;
        set { if (SetProperty(ref _consoleOutput, value)) { LogToConsole = value; MarkAsChanged(); } }
    }

    public string LoggingLevel
    {
        get => _logLevel switch
        {
            "Debug" => "Verbose",
            "Information" => "Information",
            "Warning" => "Minimal",
            "Error" => "Minimal",
            _ => "Information"
        };
        set
        {
            LogLevel = value switch
            {
                "Verbose" => "Debug",
                "Information" => "Information",
                "Minimal" => "Warning",
                _ => "Information"
            };
            MarkAsChanged();
        }
    }

    public bool LoggingEnableStructured
    {
        get => _enableStructured;
        set { if (SetProperty(ref _enableStructured, value)) MarkAsChanged(); }
    }

    // Commands
    public ICommand BrowseLogDirectoryCommand { get; }
    public ICommand OpenLogFolderCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public new ICommand ResetToDefaultCommand { get; }

    private void BrowseLogDirectory()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Log Directory",
            FileName = "Log Folder",
            Filter = "Directory|*.this.directory",
            CheckFileExists = false,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == true)
        {
            var path = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(path))
                LogDirectory = path;
        }
    }

    private void OpenLogFolder()
    {
        if (!string.IsNullOrEmpty(LogDirectory) && System.IO.Directory.Exists(LogDirectory))
        {
            System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
        }
    }

    private void ClearLogs()
    {
        // TODO: Implement log clearing
        if (!string.IsNullOrEmpty(LogDirectory) && System.IO.Directory.Exists(LogDirectory))
        {
            var files = System.IO.Directory.GetFiles(LogDirectory, "*.log");
            foreach (var file in files)
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch
                {
                    // Ignore files that can't be deleted (in use)
                }
            }
        }
    }

    public override void RefreshFromSettings()
    {
        var settings = ParentSettings.Settings;

        // Load saved settings
        LogLevel = settings.LogLevel;
        MaxLogSizeMB = settings.MaxLogSizeMB;
        LogRetentionDays = settings.LogRetentionDays;
        ConsoleOutput = settings.ConsoleOutput;

        // Initialize all properties with defaults
        EnableLogging = true;
        LogToFile = true;
        LogDirectory = SettingsPaths.LogsDirectory;
        LogFormat = "PlainText";

        MaxFileSize = MaxLogSizeMB > 0 ? MaxLogSizeMB : 10;
        FileRotation = "Daily";
        RetentionDays = LogRetentionDays > 0 ? LogRetentionDays : 30;

        LogPerformanceMetrics = false;
        LogModelInference = false;
        LogApiCalls = false;

        EnableVerboseLogging = false;
        LogStackTraces = true;
        LogSensitiveData = false;
        LogToConsole = ConsoleOutput;

        SendCrashReports = false;
        IncludeSystemInfo = true;
        CreateLocalDumps = true;

        LoggingEnableStructured = true;

        // Fix empty values
        if (string.IsNullOrEmpty(LogLevel)) LogLevel = "Information";
        if (MaxFileSize == 0) MaxFileSize = 10;
        if (RetentionDays == 0) RetentionDays = 30;
    }

    public override async Task ApplySettingsAsync()
    {
        var settings = ParentSettings.Settings;
        settings.LogLevel = LogLevel;
        settings.MaxLogSizeMB = MaxFileSize;
        settings.LogRetentionDays = RetentionDays;
        settings.ConsoleOutput = LogToConsole;
        await Task.CompletedTask;
    }

    protected override void ResetToDefault()
    {
        // General Settings
        EnableLogging = true;
        LogLevel = "Information";
        LogFormat = "PlainText";

        // File Logging
        LogToFile = true;
        try
        {
            LogDirectory = SettingsPaths.LogsDirectory;
        }
        catch
        {
            LogDirectory = @"C:\Lazarus\Logs";
        }
        MaxFileSize = 10;
        FileRotation = "Daily";
        RetentionDays = 30;

        // Performance Logging
        LogPerformanceMetrics = false;
        LogModelInference = false;
        LogApiCalls = false;

        // Debug & Diagnostics
        EnableVerboseLogging = false;
        LogStackTraces = true;
        LogSensitiveData = false;
        LogToConsole = true;

        // Crash Reporting
        SendCrashReports = false;
        IncludeSystemInfo = true;
        CreateLocalDumps = true;

        // Legacy properties
        MaxLogSizeMB = 10;
        LogRetentionDays = 30;
        ConsoleOutput = true;
        LoggingEnableStructured = true;
    }
}