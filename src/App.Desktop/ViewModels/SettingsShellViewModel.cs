using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.ViewModels;

public sealed class SettingsShellViewModel
{
    public ObservableCollection<SettingsSection> Sections { get; } = new();
    public SettingsSection? SelectedSection { get; set; }

    public SettingsShellViewModel()
    {
        // Resolve one SettingsViewModel instance for all views to bind against
        var svm = App.ServiceProvider.GetRequiredService<SettingsViewModel>();

        SettingsSection Make<TView>(string title) where TView : UserControl, new()
        {
            var view = new TView();
            view.DataContext = svm;
            return new SettingsSection(title, view);
        }

        Sections.Add(Make<Views.GeneralSettingsView>("General"));
        Sections.Add(Make<Views.PathsSettingsView>("Paths"));
        Sections.Add(Make<Views.OrchestratorSettingsView>("Orchestrator"));
        Sections.Add(Make<Views.RunnersSettingsView>("Runners"));
        Sections.Add(Make<Views.ModelsSettingsView>("Models"));
        Sections.Add(Make<Views.AudioSettingsView>("Audio"));
        Sections.Add(Make<Views.RagSettingsView>("Embeddings / RAG"));
        Sections.Add(Make<Views.TrainingSettingsView>("Training"));
        Sections.Add(Make<Views.LoggingSettingsView>("Logging"));
        Sections.Add(Make<Views.AdvancedSettingsView>("Advanced"));
        Sections.Add(Make<Views.AvatarSettingsView>("Avatars (future)"));

        SelectedSection = Sections.FirstOrDefault();
    }
}

public sealed record SettingsSection(string Title, UserControl View);

