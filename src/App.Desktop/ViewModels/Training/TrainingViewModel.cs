using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class TrainingViewModel : INotifyPropertyChanged, IDisposable
    {
        public JobsSidebarViewModel JobsSidebar { get; } = new();
        public JobDesignerViewModel JobDesigner { get; } = new();
        public MonitorDockViewModel MonitorDock { get; } = new();
        public InspectorViewModel Inspector { get; } = new();

        private string _selectedModality = "Conversations";
        public string SelectedModality
        {
            get => _selectedModality;
            set { _selectedModality = value; OnPropertyChanged(); }
        }

        private bool _isProgressMode;
        public bool IsProgressMode { get => _isProgressMode; set { _isProgressMode = value; OnPropertyChanged(); } }

        private bool _isMonitorOpen;
        public bool IsMonitorOpen { get => _isMonitorOpen; set { _isMonitorOpen = value; OnPropertyChanged(); } }

        // Actions
        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ExportCommand { get; }

        public bool CanStart { get; private set; } = true;
        public bool CanPause { get; private set; } = false;
        public bool CanStop  { get; private set; } = false;
        public bool CanExport{ get; private set; } = false;

        public TrainingViewModel()
        {
            StartCommand = new RelayCommand(_ => { /* TODO(training): queue/start */ CanStart=false; CanPause=true; CanStop=true; OnPropertyChanged(nameof(CanStart)); OnPropertyChanged(nameof(CanPause)); OnPropertyChanged(nameof(CanStop)); }, _ => CanStart);
            PauseCommand = new RelayCommand(_ => { /* TODO(training): pause */ }, _ => CanPause);
            StopCommand  = new RelayCommand(_ => { /* TODO(training): stop */ CanExport=true; OnPropertyChanged(nameof(CanExport)); }, _ => CanStop);
            ExportCommand= new RelayCommand(_ => { /* TODO(training): export */ }, _ => CanExport);

            // TODO(training): wire keyboard shortcuts in view if needed
        }

        public void Dispose()
        {
            // TODO(training): cleanup stream subscriptions when added
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

