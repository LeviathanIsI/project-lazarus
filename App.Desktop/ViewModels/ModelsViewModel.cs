using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.OpenAI;

namespace Lazarus.Desktop.ViewModels;

public class ModelsViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _statusText = "Ready";
    private SystemInfo? _systemInfo;

    public ModelsViewModel()
    {
        RefreshSystemCommand = new RelayCommand(async _ => await RefreshSystemInfoAsync(), _ => !IsLoading);

        // Load system info on startup
        _ = Task.Run(async () => await RefreshSystemInfoAsync());
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public SystemInfo? SystemInfo
    {
        get => _systemInfo;
        set => SetProperty(ref _systemInfo, value);
    }

    public ICommand RefreshSystemCommand { get; }

    private async Task RefreshSystemInfoAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Getting system info...";

            var info = await ApiClient.GetSystemInfoAsync();
            if (info != null)
            {
                SystemInfo = info;
                StatusText = "System info updated";
            }
            else
            {
                StatusText = "Failed to get system info";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"System info error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}