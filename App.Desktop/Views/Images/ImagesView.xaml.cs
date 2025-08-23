using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Images;

namespace Lazarus.Desktop.Views.Images
{
    public partial class ImagesView : UserControl
    {
        public ImagesView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;

            // Default active tab
            SetActiveSubTab(Text2ImageTab, Text2ImageContent);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ImagesViewModel mainViewModel)
            {
                // Wire up child controls to their respective ViewModels
                Text2ImageContent.DataContext = mainViewModel.Text2Image;
                Image2ImageContent.DataContext = mainViewModel.Image2Image;
                InpaintingContent.DataContext = mainViewModel.Inpainting;
                UpscalingContent.DataContext = mainViewModel.Upscaling;
            }
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabTag)
            {
                // Hide all sections
                Text2ImageContent.Visibility = Visibility.Collapsed;
                Image2ImageContent.Visibility = Visibility.Collapsed;
                InpaintingContent.Visibility = Visibility.Collapsed;
                UpscalingContent.Visibility = Visibility.Collapsed;

                // Reset all button styles
                ResetSubTabButtonStyles();

                // Show + activate selected
                switch (tabTag)
                {
                    case "Text2Image":
                        SetActiveSubTab(Text2ImageTab, Text2ImageContent);
                        break;
                    case "Image2Image":
                        SetActiveSubTab(Image2ImageTab, Image2ImageContent);
                        break;
                    case "Inpainting":
                        SetActiveSubTab(InpaintingTab, InpaintingContent);
                        break;
                    case "Upscaling":
                        SetActiveSubTab(UpscalingTab, UpscalingContent);
                        break;
                }
            }
        }

        private void ResetSubTabButtonStyles()
        {
            Text2ImageTab.Style = (Style)FindResource("SubTabButtonStyle");
            Image2ImageTab.Style = (Style)FindResource("SubTabButtonStyle");
            InpaintingTab.Style = (Style)FindResource("SubTabButtonStyle");
            UpscalingTab.Style = (Style)FindResource("SubTabButtonStyle");
        }

        private void SetActiveSubTab(Button tabButton, UIElement contentPanel)
        {
            tabButton.Style = (Style)FindResource("ActiveSubTabButtonStyle");
            contentPanel.Visibility = Visibility.Visible;
        }
    }
}