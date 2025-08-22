using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lazarus.Desktop.Views
{
    public partial class ModelsView : UserControl
    {
        private static readonly Brush ActiveBg = (Brush)new BrushConverter().ConvertFromString("#8b5cf6")!;
        private static readonly Brush InactiveBg = (Brush)new BrushConverter().ConvertFromString("#374151")!;
        private static readonly Brush ActiveFg = Brushes.White;
        private static readonly Brush InactiveFg = (Brush)new BrushConverter().ConvertFromString("#9ca3af")!;

        public ModelsView()
        {
            InitializeComponent();
            ShowTab("BaseModel");
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag as string ?? "BaseModel";
            ShowTab(tag);
        }

        private void ShowTab(string tag)
        {
            // Content visibility
            BaseModelContent.Visibility = tag == "BaseModel" ? Visibility.Visible : Visibility.Collapsed;
            LoRAsContent.Visibility = tag == "LoRAs" ? Visibility.Visible : Visibility.Collapsed;
            ControlNetsContent.Visibility = tag == "ControlNets" ? Visibility.Visible : Visibility.Collapsed;
            VAEsContent.Visibility = tag == "VAEs" ? Visibility.Visible : Visibility.Collapsed;
            EmbeddingsContent.Visibility = tag == "Embeddings" ? Visibility.Visible : Visibility.Collapsed;
            HypernetworksContent.Visibility = tag == "Hypernetworks" ? Visibility.Visible : Visibility.Collapsed;
            AdvancedContent.Visibility = tag == "Advanced" ? Visibility.Visible : Visibility.Collapsed;

            // Tab button styling
            SetActive(BaseModelTab, tag == "BaseModel");
            SetActive(LoRAsTab, tag == "LoRAs");
            SetActive(ControlNetsTab, tag == "ControlNets");
            SetActive(VAEsTab, tag == "VAEs");
            SetActive(EmbeddingsTab, tag == "Embeddings");
            SetActive(HypernetworksTab, tag == "Hypernetworks");
            SetActive(AdvancedTab, tag == "Advanced");
        }

        private void SetActive(Button btn, bool active)
        {
            if (btn is null) return;
            btn.Background = active ? ActiveBg : InactiveBg;
            btn.Foreground = active ? ActiveFg : InactiveFg;
        }
    }
}
