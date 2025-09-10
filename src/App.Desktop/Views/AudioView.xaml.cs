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
                DataContext = Lazarus.Desktop.App.ServiceProvider.GetRequiredService<AudioViewModel>();
            }
            catch { }
        }
    }
}
