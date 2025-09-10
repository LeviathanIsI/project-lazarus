using System.ComponentModel;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views
{
    public partial class AudioView : UserControl
    {
        public AudioView()
        {
            InitializeComponent();
            try
            {
                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    DataContext = Lazarus.Desktop.App.ServiceProvider.GetRequiredService<AudioViewModel>();
                }
            }
            catch { }
        }
    }
}
