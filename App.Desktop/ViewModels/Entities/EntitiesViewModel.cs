using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Entities
{
    public class EntitiesViewModel : INotifyPropertyChanged
    {
        public EntityCreationViewModel EntityCreation { get; }
        public BehavioralPatternsViewModel BehavioralPatterns { get; }
        public InteractionTestingViewModel InteractionTesting { get; }
        public EntityManagementViewModel EntityManagement { get; }

        public EntitiesViewModel()
        {
            EntityCreation = new EntityCreationViewModel();
            BehavioralPatterns = new BehavioralPatternsViewModel();
            InteractionTesting = new InteractionTestingViewModel();
            EntityManagement = new EntityManagementViewModel();

            // Wire up communication between ViewModels if needed
            EntityCreation.PropertyChanged += OnSubViewModelPropertyChanged;
            BehavioralPatterns.PropertyChanged += OnSubViewModelPropertyChanged;
            InteractionTesting.PropertyChanged += OnSubViewModelPropertyChanged;
            EntityManagement.PropertyChanged += OnSubViewModelPropertyChanged;
        }

        private void OnSubViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Handle cross-communication between entity sub-tabs if needed
            // For example, sharing entity definitions between creation and testing
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}