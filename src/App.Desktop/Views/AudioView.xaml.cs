using System;
using System.ComponentModel;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views;

public partial class AudioView : UserControl, IDisposable
{
    private IServiceScope? _scope;
    private AudioViewModel? _viewModel;
    
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
    
    public void Dispose()
    {
        _viewModel?.Dispose();
        _scope?.Dispose();
        _scope = null;
    }
}