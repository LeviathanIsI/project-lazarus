using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lazarus.Desktop.Views
{
    public partial class ModelsView : UserControl
    {
        public ModelsView()
        {
            InitializeComponent();
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabTag)
            {
                // Hide all content sections
                BaseModelContent.Visibility = Visibility.Collapsed;
                LoRAsContent.Visibility = Visibility.Collapsed;
                ControlNetsContent.Visibility = Visibility.Collapsed;
                VAEsContent.Visibility = Visibility.Collapsed;
                EmbeddingsContent.Visibility = Visibility.Collapsed;
                HypernetworksContent.Visibility = Visibility.Collapsed;
                AdvancedContent.Visibility = Visibility.Collapsed;

                // Reset all tab button styles
                ResetSubTabButtonStyles();

                // Show the selected section + highlight button
                switch (tabTag)
                {
                    case "BaseModel":
                        BaseModelContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(BaseModelTab);
                        break;
                    case "LoRAs":
                        LoRAsContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(LoRAsTab);
                        break;
                    case "ControlNets":
                        ControlNetsContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(ControlNetsTab);
                        break;
                    case "VAEs":
                        VAEsContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(VAEsTab);
                        break;
                    case "Embeddings":
                        EmbeddingsContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(EmbeddingsTab);
                        break;
                    case "Hypernetworks":
                        HypernetworksContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(HypernetworksTab);
                        break;
                    case "Advanced":
                        AdvancedContent.Visibility = Visibility.Visible;
                        SetActiveSubTabStyle(AdvancedTab);
                        break;
                }
            }
        }

        private void ResetSubTabButtonStyles()
        {
            var inactiveBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81)); // #374151
            var inactiveTextBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9ca3af

            BaseModelTab.Background = inactiveBrush;
            BaseModelTab.Foreground = inactiveTextBrush;

            LoRAsTab.Background = inactiveBrush;
            LoRAsTab.Foreground = inactiveTextBrush;

            ControlNetsTab.Background = inactiveBrush;
            ControlNetsTab.Foreground = inactiveTextBrush;

            VAEsTab.Background = inactiveBrush;
            VAEsTab.Foreground = inactiveTextBrush;

            EmbeddingsTab.Background = inactiveBrush;
            EmbeddingsTab.Foreground = inactiveTextBrush;

            HypernetworksTab.Background = inactiveBrush;
            HypernetworksTab.Foreground = inactiveTextBrush;

            AdvancedTab.Background = inactiveBrush;
            AdvancedTab.Foreground = inactiveTextBrush;
        }

        private void SetActiveSubTabStyle(Button button)
        {
            var activeBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8b5cf6
            var activeTextBrush = new SolidColorBrush(Colors.White);

            button.Background = activeBrush;
            button.Foreground = activeTextBrush;
        }
    }
}
