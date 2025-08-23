using System.Windows;
using System.Windows.Controls;
using Lazarus.Desktop.ViewModels.Entities;

namespace Lazarus.Desktop.Views.Entities
{
    public partial class EntitiesView : UserControl
    {
        public EntitiesView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;

            // Default active tab
            SetActiveSubTab(EntityCreationTab, EntityCreationContent);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is EntitiesViewModel mainViewModel)
            {
                // Wire up child controls to their respective ViewModels
                EntityCreationContent.DataContext = mainViewModel.EntityCreation;
                BehavioralPatternsContent.DataContext = mainViewModel.BehavioralPatterns;
                InteractionTestingContent.DataContext = mainViewModel.InteractionTesting;
                EntityManagementContent.DataContext = mainViewModel.EntityManagement;
            }
        }

        private void SubTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabTag)
            {
                // Hide all sections
                EntityCreationContent.Visibility = Visibility.Collapsed;
                BehavioralPatternsContent.Visibility = Visibility.Collapsed;
                InteractionTestingContent.Visibility = Visibility.Collapsed;
                EntityManagementContent.Visibility = Visibility.Collapsed;

                // Reset all button styles
                ResetSubTabButtonStyles();

                // Show + activate selected
                switch (tabTag)
                {
                    case "EntityCreation":
                        SetActiveSubTab(EntityCreationTab, EntityCreationContent);
                        break;
                    case "BehavioralPatterns":
                        SetActiveSubTab(BehavioralPatternsTab, BehavioralPatternsContent);
                        break;
                    case "InteractionTesting":
                        SetActiveSubTab(InteractionTestingTab, InteractionTestingContent);
                        break;
                    case "EntityManagement":
                        SetActiveSubTab(EntityManagementTab, EntityManagementContent);
                        break;
                }
            }
        }

        private void ResetSubTabButtonStyles()
        {
            EntityCreationTab.Style = (Style)FindResource("SubTabButtonStyle");
            BehavioralPatternsTab.Style = (Style)FindResource("SubTabButtonStyle");
            InteractionTestingTab.Style = (Style)FindResource("SubTabButtonStyle");
            EntityManagementTab.Style = (Style)FindResource("SubTabButtonStyle");
        }

        private void SetActiveSubTab(Button tabButton, UIElement contentPanel)
        {
            tabButton.Style = (Style)FindResource("ActiveSubTabButtonStyle");
            contentPanel.Visibility = Visibility.Visible;
        }
    }
}