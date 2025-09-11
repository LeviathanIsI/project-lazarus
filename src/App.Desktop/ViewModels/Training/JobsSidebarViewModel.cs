using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class JobsSidebarViewModel : INotifyPropertyChanged
    {
        public sealed record JobItem(Guid Id, string Name, string Status);

        public ObservableCollection<JobItem> Jobs { get; } = new();

        private JobItem? _selectedJob;
        public JobItem? SelectedJob { get => _selectedJob; set { _selectedJob = value; OnPropertyChanged(); } }

        // Filters
        public bool FilterAll { get => _filterAll; set { _filterAll = value; OnPropertyChanged(); } }
        public bool FilterActive { get => _filterActive; set { _filterActive = value; OnPropertyChanged(); } }
        public bool FilterQueued { get => _filterQueued; set { _filterQueued = value; OnPropertyChanged(); } }
        public bool FilterCompleted { get => _filterCompleted; set { _filterCompleted = value; OnPropertyChanged(); } }
        public bool FilterFailed { get => _filterFailed; set { _filterFailed = value; OnPropertyChanged(); } }
        private bool _filterAll = true, _filterActive, _filterQueued, _filterCompleted, _filterFailed;

        // Commands
        public ICommand NewJobCommand { get; }
        public ICommand ImportDatasetCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ResumeCommand { get; }

        public JobsSidebarViewModel()
        {
            // TODO(training): seed placeholder jobs
            Jobs.Add(new JobItem(Guid.NewGuid(), "Training Job 1", "Queued"));
            Jobs.Add(new JobItem(Guid.NewGuid(), "Training Job 2", "Running"));

            NewJobCommand = new RelayCommand(_ => { /* TODO */ });
            ImportDatasetCommand = new RelayCommand(_ => { /* TODO */ });
            DuplicateCommand = new RelayCommand(_ => { /* TODO */ }, _ => SelectedJob != null);
            DeleteCommand = new RelayCommand(_ => { /* TODO */ }, _ => SelectedJob != null);
            ResumeCommand = new RelayCommand(_ => { /* TODO */ }, _ => SelectedJob != null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

