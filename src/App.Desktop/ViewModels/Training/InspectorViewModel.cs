using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class InspectorViewModel : INotifyPropertyChanged
    {
        public string? SelectionSummary { get => _selectionSummary; set { _selectionSummary = value; OnPropertyChanged(); } }
        private string? _selectionSummary = "// TODO(training): inspector selection metadata";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

