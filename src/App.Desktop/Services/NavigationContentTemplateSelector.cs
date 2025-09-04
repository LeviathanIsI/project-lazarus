using System.Windows;
using System.Windows.Controls;
using Lazarus.App.Desktop.ViewModels;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Template selector for navigation content based on view model type
/// </summary>
public class NavigationContentTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Selects the appropriate data template based on the item (view model)
    /// </summary>
    /// <param name="item">The view model instance</param>
    /// <param name="container">The dependency object container</param>
    /// <returns>The appropriate data template</returns>
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (container is FrameworkElement element && item != null)
        {
            var templateKey = item switch
            {
                DashboardViewModel => "DashboardTemplate",
                ConversationsViewModel => "ConversationsTemplate",
                ModelConfigurationViewModel => "ModelConfigurationTemplate",
                RunnerManagerViewModel => "RunnerManagerTemplate",
                JobsViewModel => "JobsTemplate",
                DatasetsViewModel => "DatasetsTemplate",
                ImagesViewModel => "ImagesTemplate",
                VideoViewModel => "VideoTemplate",
                VoiceViewModel => "VoiceTemplate",
                ThreeDModelsViewModel => "ThreeDModelsTemplate",
                EntitiesViewModel => "EntitiesTemplate",
                TrainingViewModel => "TrainingTemplate",
                _ => null
            };

            if (templateKey != null)
            {
                return element.FindResource(templateKey) as DataTemplate ?? base.SelectTemplate(item, container);
            }
        }

        return base.SelectTemplate(item, container);
    }
}