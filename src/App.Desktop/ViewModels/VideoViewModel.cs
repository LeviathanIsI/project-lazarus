using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Video section
/// </summary>
public partial class VideoViewModel : BaseViewModel
{
    private readonly ILogger<VideoViewModel> _logger;
    private VideoItem? _selectedVideo;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public VideoViewModel(ILogger<VideoViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Video";
        StatusMessage = "Video processing tools coming soon";
        
        Videos = new ObservableCollection<VideoItem>();
        
        ImportVideoCommand = new AsyncRelayCommand(ImportVideoAsync);
        ProcessVideoCommand = new AsyncRelayCommand<VideoItem>(ProcessVideoAsync);
        
        LoadSampleVideos();
        
        _logger.LogInformation("Video view model initialized");
    }

    public string Title { get; }
    
    public VideoItem? SelectedVideo
    {
        get => _selectedVideo;
        set => SetProperty(ref _selectedVideo, value);
    }

    public ObservableCollection<VideoItem> Videos { get; }
    public IAsyncRelayCommand ImportVideoCommand { get; }
    public IAsyncRelayCommand<VideoItem> ProcessVideoCommand { get; }

    private async Task ImportVideoAsync()
    {
        try
        {
            SetBusyState(true, "Importing video...");
            await Task.Delay(2000);

            var newVideo = new VideoItem($"Video_{DateTime.Now:HHmmss}.mp4", "MP4", "1920x1080", "00:02:30", "45.2 MB", DateTime.Now);
            Videos.Insert(0, newVideo);
            
            SetBusyState(false, "Video imported successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing video");
            SetBusyState(false, "Failed to import video");
        }
    }

    private async Task ProcessVideoAsync(VideoItem? video)
    {
        if (video == null) return;
        
        try
        {
            SetBusyState(true, $"Processing video '{video.Name}'...");
            await Task.Delay(3000);
            SetBusyState(false, $"Video '{video.Name}' processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            SetBusyState(false, "Failed to process video");
        }
    }

    private void LoadSampleVideos()
    {
        Videos.Clear();
        Videos.Add(new VideoItem("sample_001.mp4", "MP4", "1920x1080", "00:05:30", "125.5 MB", DateTime.Now.AddHours(-2)));
        Videos.Add(new VideoItem("training_data.avi", "AVI", "1280x720", "00:10:15", "235.8 MB", DateTime.Now.AddHours(-4)));
        Videos.Add(new VideoItem("demo_video.mov", "MOV", "2560x1440", "00:03:45", "180.2 MB", DateTime.Now.AddHours(-1)));
    }
}

public class VideoItem
{
    public VideoItem(string name, string format, string resolution, string duration, string size, DateTime createdDate)
    {
        Name = name;
        Format = format;
        Resolution = resolution;
        Duration = duration;
        Size = size;
        CreatedDate = createdDate;
    }

    public string Name { get; }
    public string Format { get; }
    public string Resolution { get; }
    public string Duration { get; }
    public string Size { get; }
    public DateTime CreatedDate { get; }
}