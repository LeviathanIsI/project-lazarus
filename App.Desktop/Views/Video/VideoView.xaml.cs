using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Video;

namespace Lazarus.Desktop.Views.Video
{
    public partial class VideoView : UserControl
    {
        public VideoView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;

            // Default active tab
            SetActiveSubTab(Text2VideoTab, Text2VideoContent);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is VideoViewModel mainViewModel)
            {
                // Wire up child controls to their respective ViewModels
                Text2VideoContent.DataContext = mainViewModel.Text2Video;
                Video2VideoContent.DataContext = mainViewModel.Video2Video;
                MotionControlContent.DataContext = mainViewModel.MotionControl;
                TemporalEffectsContent.DataContext = mainViewModel.TemporalEffects;
            }
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabTag)
            {
                // Hide all sections
                Text2VideoContent.Visibility = Visibility.Collapsed;
                Video2VideoContent.Visibility = Visibility.Collapsed;
                MotionControlContent.Visibility = Visibility.Collapsed;
                TemporalEffectsContent.Visibility = Visibility.Collapsed;

                // Reset all button styles
                ResetSubTabButtonStyles();

                // Show + activate selected
                switch (tabTag)
                {
                    case "Text2Video":
                        SetActiveSubTab(Text2VideoTab, Text2VideoContent);
                        break;
                    case "Video2Video":
                        SetActiveSubTab(Video2VideoTab, Video2VideoContent);
                        break;
                    case "MotionControl":
                        SetActiveSubTab(MotionControlTab, MotionControlContent);
                        break;
                    case "TemporalEffects":
                        SetActiveSubTab(TemporalEffectsTab, TemporalEffectsContent);
                        break;
                }
            }
        }

        private void ResetSubTabButtonStyles()
        {
            Text2VideoTab.Style = (Style)FindResource("SubTabButtonStyle");
            Video2VideoTab.Style = (Style)FindResource("SubTabButtonStyle");
            MotionControlTab.Style = (Style)FindResource("SubTabButtonStyle");
            TemporalEffectsTab.Style = (Style)FindResource("SubTabButtonStyle");
        }

        private void SetActiveSubTab(Button tabButton, UIElement contentPanel)
        {
            tabButton.Style = (Style)FindResource("ActiveSubTabButtonStyle");
            contentPanel.Visibility = Visibility.Visible;
        }
    }
}