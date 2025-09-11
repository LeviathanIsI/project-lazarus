using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    public partial class AudioView : UserControl, IDisposable
    {
        private readonly IServiceScope _scope;

        public AudioView()
        {
            InitializeComponent();

            // Per-view scope so tabs don’t bleed into each other
            _scope = App.ServiceProvider.CreateScope();
            DataContext = _scope.ServiceProvider.GetRequiredService<AudioViewModel>();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
