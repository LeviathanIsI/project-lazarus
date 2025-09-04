using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Voice section
/// </summary>
public partial class VoiceViewModel : BaseViewModel
{
    private readonly ILogger<VoiceViewModel> _logger;
    private VoiceItem? _selectedVoice;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public VoiceViewModel(ILogger<VoiceViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Voice";
        StatusMessage = "Voice processing tools coming soon";
        
        VoiceRecordings = new ObservableCollection<VoiceItem>();
        
        RecordVoiceCommand = new AsyncRelayCommand(RecordVoiceAsync);
        PlayVoiceCommand = new RelayCommand<VoiceItem>(ExecutePlayVoice);
        GenerateVoiceCommand = new AsyncRelayCommand(GenerateVoiceAsync);
        
        LoadSampleVoiceData();
        
        _logger.LogInformation("Voice view model initialized");
    }

    public string Title { get; }
    
    public VoiceItem? SelectedVoice
    {
        get => _selectedVoice;
        set => SetProperty(ref _selectedVoice, value);
    }

    public ObservableCollection<VoiceItem> VoiceRecordings { get; }
    public IAsyncRelayCommand RecordVoiceCommand { get; }
    public IRelayCommand<VoiceItem> PlayVoiceCommand { get; }
    public IAsyncRelayCommand GenerateVoiceCommand { get; }

    private async Task RecordVoiceAsync()
    {
        try
        {
            SetBusyState(true, "Recording voice...");
            await Task.Delay(2000);

            var newRecording = new VoiceItem($"Recording_{DateTime.Now:HHmmss}.wav", "WAV", "00:00:15", "2.1 MB", DateTime.Now, "User");
            VoiceRecordings.Insert(0, newRecording);
            
            SetBusyState(false, "Voice recorded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording voice");
            SetBusyState(false, "Failed to record voice");
        }
    }

    private void ExecutePlayVoice(VoiceItem? voice)
    {
        if (voice == null) return;
        
        _logger.LogInformation("Playing voice: {VoiceName}", voice.Name);
        StatusMessage = $"Playing '{voice.Name}'";
    }

    private async Task GenerateVoiceAsync()
    {
        try
        {
            SetBusyState(true, "Generating AI voice...");
            await Task.Delay(3000);

            var generatedVoice = new VoiceItem($"Generated_{DateTime.Now:HHmmss}.mp3", "MP3", "00:00:30", "1.8 MB", DateTime.Now, "AI");
            VoiceRecordings.Insert(0, generatedVoice);
            
            SetBusyState(false, "AI voice generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI voice");
            SetBusyState(false, "Failed to generate AI voice");
        }
    }

    private void LoadSampleVoiceData()
    {
        VoiceRecordings.Clear();
        VoiceRecordings.Add(new VoiceItem("sample_speech.wav", "WAV", "00:01:30", "5.2 MB", DateTime.Now.AddHours(-2), "User"));
        VoiceRecordings.Add(new VoiceItem("ai_narrator.mp3", "MP3", "00:02:15", "3.8 MB", DateTime.Now.AddHours(-1), "AI"));
        VoiceRecordings.Add(new VoiceItem("training_audio.flac", "FLAC", "00:05:00", "12.1 MB", DateTime.Now.AddHours(-4), "Dataset"));
    }
}

public class VoiceItem
{
    public VoiceItem(string name, string format, string duration, string size, DateTime createdDate, string source)
    {
        Name = name;
        Format = format;
        Duration = duration;
        Size = size;
        CreatedDate = createdDate;
        Source = source;
    }

    public string Name { get; }
    public string Format { get; }
    public string Duration { get; }
    public string Size { get; }
    public DateTime CreatedDate { get; }
    public string Source { get; }
}