using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Audio;

public partial class AudioView : UserControl
{
    public AudioView()
    {
        InitializeComponent();
        try { Lazarus.Shared.LazarusPaths.Audio.EnsureDirectories(); } catch { }

        if (!DesignerProperties.GetIsInDesignMode(this) && DataContext == null)
        {
            try
            {
                if (App.ServiceProvider != null)
                {
                    var vm = App.ServiceProvider.GetRequiredService<Lazarus.Desktop.ViewModels.Audio.AudioViewModel>();
                    DataContext = vm;
                }
            }
            catch { }
        }
    }
}

