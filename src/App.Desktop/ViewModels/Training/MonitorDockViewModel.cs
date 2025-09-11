using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class MonitorDockViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> Logs { get; } = new();
        public string LogsFilter { get => _logsFilter; set { _logsFilter = value; OnPropertyChanged(); } }
        private string _logsFilter = string.Empty;
        public bool Autoscroll { get => _autoscroll; set { _autoscroll = value; OnPropertyChanged(); } }
        private bool _autoscroll = true;

        // Auto-expand triggers
        public bool AutoExpandOnError { get; set; }
        public bool AutoExpandOnPlateau { get; set; }
        public bool AutoExpandOnOOM { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

