using CommunityToolkit.Mvvm.ComponentModel;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// Base view model class providing common MVVM functionality
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the view model is busy
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Gets or sets the current status message
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Sets the busy state and status message
    /// </summary>
    /// <param name="isBusy">Whether the view model is busy</param>
    /// <param name="message">The status message</param>
    protected void SetBusyState(bool isBusy, string message = "")
    {
        IsBusy = isBusy;
        StatusMessage = message;
    }
}