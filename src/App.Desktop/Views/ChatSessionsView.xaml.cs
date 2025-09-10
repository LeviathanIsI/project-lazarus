using System;
using System.ComponentModel;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views
{
    public partial class ChatSessionsView : UserControl, IDisposable
    {
        private IServiceScope? _scope;
        public ChatSessionsView()
        {
            InitializeComponent();
            try
            {
                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    _scope = Lazarus.Desktop.App.ServiceProvider.CreateScope();
                    DataContext = _scope.ServiceProvider.GetRequiredService<ChatSessionsViewModel>();
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
