using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels;

namespace Lazarus.Desktop.Views
{
    public partial class ModelsView : UserControl
    {
        public ModelsView()
        {
            InitializeComponent();

            // Default active tab
            SetActiveSubTab(BaseModelTab, BaseModelContent);
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabTag)
            {
                // Hide all sections
                BaseModelContent.Visibility = Visibility.Collapsed;
                LoRAsContent.Visibility = Visibility.Collapsed;
                ControlNetsContent.Visibility = Visibility.Collapsed;
                VAEsContent.Visibility = Visibility.Collapsed;
                EmbeddingsContent.Visibility = Visibility.Collapsed;
                HypernetworksContent.Visibility = Visibility.Collapsed;
                AdvancedContent.Visibility = Visibility.Collapsed;

                // Reset all button styles
                ResetSubTabButtonStyles();

                // Show + activate selected
                switch (tabTag)
                {
                    case "BaseModel":
                        SetActiveSubTab(BaseModelTab, BaseModelContent);
                        break;
                    case "LoRAs":
                        SetActiveSubTab(LoRAsTab, LoRAsContent);
                        break;
                    case "ControlNets":
                        SetActiveSubTab(ControlNetsTab, ControlNetsContent);
                        break;
                    case "VAEs":
                        SetActiveSubTab(VAEsTab, VAEsContent);
                        break;
                    case "Embeddings":
                        SetActiveSubTab(EmbeddingsTab, EmbeddingsContent);
                        break;
                    case "Hypernetworks":
                        SetActiveSubTab(HypernetworksTab, HypernetworksContent);
                        break;
                    case "Advanced":
                        SetActiveSubTab(AdvancedTab, AdvancedContent);
                        break;
                }
            }
        }

        private void ResetSubTabButtonStyles()
        {
            BaseModelTab.Style = (Style)FindResource("SubTabButtonStyle");
            LoRAsTab.Style = (Style)FindResource("SubTabButtonStyle");
            ControlNetsTab.Style = (Style)FindResource("SubTabButtonStyle");
            VAEsTab.Style = (Style)FindResource("SubTabButtonStyle");
            EmbeddingsTab.Style = (Style)FindResource("SubTabButtonStyle");
            HypernetworksTab.Style = (Style)FindResource("SubTabButtonStyle");
            AdvancedTab.Style = (Style)FindResource("SubTabButtonStyle");
        }

        private void SetActiveSubTab(Button tabButton, UIElement contentPanel)
        {
            tabButton.Style = (Style)FindResource("ActiveSubTabButtonStyle");
            contentPanel.Visibility = Visibility.Visible;
        }
    }
}