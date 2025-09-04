using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Images section
/// </summary>
public partial class ImagesViewModel : BaseViewModel
{
    private readonly ILogger<ImagesViewModel> _logger;
    private ImageItem? _selectedImage;
    private string _searchFilter = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImagesViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ImagesViewModel(ILogger<ImagesViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        Title = "Images";
        StatusMessage = "Image management tools coming soon";
        
        // Initialize collections
        Images = new ObservableCollection<ImageItem>();
        
        // Initialize commands
        ImportImagesCommand = new AsyncRelayCommand(ImportImagesAsync);
        GenerateImageCommand = new AsyncRelayCommand(GenerateImageAsync);
        DeleteImageCommand = new RelayCommand<ImageItem>(ExecuteDeleteImage);
        
        // Load sample data
        LoadSampleImages();
        
        _logger.LogInformation("Images view model initialized");
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the selected image
    /// </summary>
    public ImageItem? SelectedImage
    {
        get => _selectedImage;
        set => SetProperty(ref _selectedImage, value);
    }

    /// <summary>
    /// Gets or sets the search filter
    /// </summary>
    public string SearchFilter
    {
        get => _searchFilter;
        set => SetProperty(ref _searchFilter, value);
    }

    /// <summary>
    /// Gets the collection of images
    /// </summary>
    public ObservableCollection<ImageItem> Images { get; }

    /// <summary>
    /// Gets the import images command
    /// </summary>
    public IAsyncRelayCommand ImportImagesCommand { get; }

    /// <summary>
    /// Gets the generate image command
    /// </summary>
    public IAsyncRelayCommand GenerateImageCommand { get; }

    /// <summary>
    /// Gets the delete image command
    /// </summary>
    public IRelayCommand<ImageItem> DeleteImageCommand { get; }

    /// <summary>
    /// Imports images from file system
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task ImportImagesAsync()
    {
        try
        {
            SetBusyState(true, "Importing images...");
            _logger.LogInformation("Importing images from file system");

            // Simulate import process
            await Task.Delay(1500);

            var importedImages = new[]
            {
                new ImageItem($"Imported_{DateTime.Now:HHmmss}_1.jpg", "JPEG", "1920x1080", "2.3 MB", DateTime.Now),
                new ImageItem($"Imported_{DateTime.Now:HHmmss}_2.png", "PNG", "1024x768", "1.8 MB", DateTime.Now),
                new ImageItem($"Imported_{DateTime.Now:HHmmss}_3.jpg", "JPEG", "2048x1536", "3.1 MB", DateTime.Now)
            };

            foreach (var image in importedImages)
            {
                Images.Insert(0, image);
            }
            
            SetBusyState(false, $"Imported {importedImages.Length} images successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing images");
            SetBusyState(false, "Failed to import images");
        }
    }

    /// <summary>
    /// Generates an AI image
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task GenerateImageAsync()
    {
        try
        {
            SetBusyState(true, "Generating AI image...");
            _logger.LogInformation("Generating AI image");

            // Simulate generation process
            await Task.Delay(3000);

            var generatedImage = new ImageItem(
                $"Generated_{DateTime.Now:HHmmss}.png",
                "PNG",
                "1024x1024",
                "4.2 MB",
                DateTime.Now
            );

            Images.Insert(0, generatedImage);
            SelectedImage = generatedImage;
            
            SetBusyState(false, "AI image generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI image");
            SetBusyState(false, "Failed to generate AI image");
        }
    }

    /// <summary>
    /// Executes the delete image command
    /// </summary>
    /// <param name="image">The image to delete</param>
    private void ExecuteDeleteImage(ImageItem? image)
    {
        if (image == null) return;
        
        _logger.LogInformation("Deleting image: {ImageName}", image.Name);
        Images.Remove(image);
        
        if (SelectedImage == image)
        {
            SelectedImage = null;
        }
        
        StatusMessage = $"Image '{image.Name}' deleted";
    }

    /// <summary>
    /// Loads sample image data
    /// </summary>
    private void LoadSampleImages()
    {
        Images.Clear();
        
        Images.Add(new ImageItem("portrait_001.jpg", "JPEG", "1920x1080", "2.5 MB", DateTime.Now.AddHours(-2)));
        Images.Add(new ImageItem("landscape_002.png", "PNG", "2560x1440", "5.1 MB", DateTime.Now.AddHours(-4)));
        Images.Add(new ImageItem("abstract_003.jpg", "JPEG", "1024x1024", "1.8 MB", DateTime.Now.AddHours(-1)));
        Images.Add(new ImageItem("ai_generated_004.png", "PNG", "1024x1024", "3.2 MB", DateTime.Now.AddMinutes(-30)));
        Images.Add(new ImageItem("photo_005.jpg", "JPEG", "4000x3000", "8.7 MB", DateTime.Now.AddHours(-6)));
    }
}

/// <summary>
/// Represents an image item
/// </summary>
public class ImageItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageItem"/> class
    /// </summary>
    /// <param name="name">The image name</param>
    /// <param name="format">The image format</param>
    /// <param name="dimensions">The image dimensions</param>
    /// <param name="size">The file size</param>
    /// <param name="createdDate">When the image was created</param>
    public ImageItem(string name, string format, string dimensions, string size, DateTime createdDate)
    {
        Name = name;
        Format = format;
        Dimensions = dimensions;
        Size = size;
        CreatedDate = createdDate;
    }

    /// <summary>
    /// Gets the image name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the image format
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets the image dimensions
    /// </summary>
    public string Dimensions { get; }

    /// <summary>
    /// Gets the file size
    /// </summary>
    public string Size { get; }

    /// <summary>
    /// Gets when the image was created
    /// </summary>
    public DateTime CreatedDate { get; }
}