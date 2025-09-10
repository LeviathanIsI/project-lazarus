using System;
using System.ComponentModel;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views
{
    public partial class AudioView : UserControl, IDisposable
    {
        private IServiceScope? _scope;
        public AudioView()
        {
            InitializeComponent();
            try
            {
                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    _scope = Lazarus.Desktop.App.ServiceProvider.CreateScope();
                    DataContext = _scope.ServiceProvider.GetRequiredService<AudioViewModel>();
                    Unloaded += (_, __) => Dispose();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}
