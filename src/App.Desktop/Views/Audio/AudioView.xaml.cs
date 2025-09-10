using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Audio;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Audio;

public partial class AudioView : UserControl, IDisposable
{
    private IServiceScope? _scope;
    private AudioViewModel? _viewModel;
    private bool _disposed;

    public AudioView()
    {
        InitializeComponent();
        
        try
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _scope = App.ServiceProvider?.CreateScope();
                if (_scope != null)
                {
                    _viewModel = _scope.ServiceProvider.GetRequiredService<AudioViewModel>();
                    DataContext = _viewModel;
                }
                
                Unloaded += (_, __) => Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioView initialization error: {ex}");
        }
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        
        if (_viewModel == null) return;
        
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // Filter for audio files
                var audioExtensions = new[] { ".wav", ".mp3", ".flac", ".m4a", ".ogg", ".wma", ".aac" };
                var audioFiles = files.Where(f => 
                    audioExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
                    .ToArray();
                
                if (audioFiles.Length > 0)
                {
                    // Import via view model
                    _ = ImportFilesAsync(audioFiles);
                }
            }
        }
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async System.Threading.Tasks.Task ImportFilesAsync(string[] files)
    {
        if (_viewModel == null) return;
        
        foreach (var file in files)
        {
            await _viewModel.ImportAsync(file);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _viewModel?.Dispose();
        _scope?.Dispose();
    }
}

// Value converters
public sealed class ZeroToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int count)
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public sealed class NonZeroToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int count)
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public sealed class NullToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value == null ? Visibility.Visible : Visibility.Collapsed;
    
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public sealed class NotNullToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;
    
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public sealed class NotNullToBooleanConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value != null;
    
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}