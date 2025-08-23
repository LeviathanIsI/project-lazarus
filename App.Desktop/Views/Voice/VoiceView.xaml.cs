using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Voice;

namespace Lazarus.Desktop.Views.Voice
{
    public partial class VoiceView : UserControl
    {
        public VoiceView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;

            // Default active tab
            SetActiveSubTab(TTSConfigurationTab, TTSConfigurationContent);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is VoiceViewModel mainViewModel)
            {
                // Wire up child controls to their respective ViewModels
                TTSConfigurationContent.DataContext = mainViewModel.TTSConfiguration;
                VoiceCloningContent.DataContext = mainViewModel.VoiceCloning;
                RealTimeSynthesisContent.DataContext = mainViewModel.RealTimeSynthesis;
                AudioProcessingContent.DataContext = mainViewModel.AudioProcessing;
            }
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabTag)
            {
                // Hide all sections
                TTSConfigurationContent.Visibility = Visibility.Collapsed;
                VoiceCloningContent.Visibility = Visibility.Collapsed;
                RealTimeSynthesisContent.Visibility = Visibility.Collapsed;
                AudioProcessingContent.Visibility = Visibility.Collapsed;

                // Reset all button styles
                ResetSubTabButtonStyles();

                // Show + activate selected
                switch (tabTag)
                {
                    case "TTSConfiguration":
                        SetActiveSubTab(TTSConfigurationTab, TTSConfigurationContent);
                        break;
                    case "VoiceCloning":
                        SetActiveSubTab(VoiceCloningTab, VoiceCloningContent);
                        break;
                    case "RealTimeSynthesis":
                        SetActiveSubTab(RealTimeSynthesisTab, RealTimeSynthesisContent);
                        break;
                    case "AudioProcessing":
                        SetActiveSubTab(AudioProcessingTab, AudioProcessingContent);
                        break;
                }
            }
        }

        private void ResetSubTabButtonStyles()
        {
            TTSConfigurationTab.Style = (Style)FindResource("SubTabButtonStyle");
            VoiceCloningTab.Style = (Style)FindResource("SubTabButtonStyle");
            RealTimeSynthesisTab.Style = (Style)FindResource("SubTabButtonStyle");
            AudioProcessingTab.Style = (Style)FindResource("SubTabButtonStyle");
        }

        private void SetActiveSubTab(Button tabButton, UIElement contentPanel)
        {
            tabButton.Style = (Style)FindResource("ActiveSubTabButtonStyle");
            contentPanel.Visibility = Visibility.Visible;
        }
    }
}