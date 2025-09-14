using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class DesignProgressViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;
        private readonly ISystemMetricsService? _metrics;

        public string Title => "Design Progress";

        // Mission Control Properties
        public int ActiveJobsCount => 3;
        private double _cpuPercent;
        public double CpuPercent { get => _cpuPercent; private set => SetProperty(ref _cpuPercent, value); }

        private double _gpuPercent;
        public double GpuPercent { get => _gpuPercent; private set => SetProperty(ref _gpuPercent, value); }

        private double _ramUsedGb;
        public double RamUsedGb { get => _ramUsedGb; private set => SetProperty(ref _ramUsedGb, value); }

        private double _ramTotalGb;
        public double RamTotalGb { get => _ramTotalGb; private set => SetProperty(ref _ramTotalGb, value); }

        public string CpuUsageText => $"{CpuPercent:0}%";
        public string GpuUsageText => GpuPercent <= 0 ? "n/a" : $"{GpuPercent:0}%";
        public string RamUsageText => RamTotalGb > 0 ? $"{RamUsedGb:0.0}/{RamTotalGb:0.0} GB" : "n/a";

        public string TotalGpuUsage => GpuUsageText;
        public string AverageThroughput => "1.2K";
        public string EtaAllJobs => "2h 15m";

        // Sample job data for visualization
        public ObservableCollection<TrainingJobSummary> ActiveJobs { get; }

        // Commands
        public ICommand PauseAllCommand { get; }
        public ICommand StopAllCommand { get; }
        public ICommand ExportReportsCommand { get; }
        public ICommand PlaySampleCommand { get; }

        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }

        public bool HasJob => CurrentJob != null;

        public DesignProgressViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            // Try resolve optional metrics service from global provider
            try
            {
                _metrics = App.ServiceProvider.GetService(typeof(ISystemMetricsService)) as ISystemMetricsService;
                if (_metrics != null)
                {
                    _metrics.MetricsUpdated += OnMetricsUpdated;
                    _metrics.Start();
                }
            }
            catch { }

            // Initialize sample active jobs data
            ActiveJobs = new ObservableCollection<TrainingJobSummary>
            {
                new TrainingJobSummary
                {
                    Name = "Customer Support Chat",
                    Modality = "💬",
                    Status = "Running",
                    Progress = 0.67,
                    CurrentStep = 1340,
                    TotalSteps = 2000,
                    Eta = "45m"
                },
                new TrainingJobSummary
                {
                    Name = "Expressive Female Voice",
                    Modality = "🎵",
                    Status = "Queued",
                    Progress = 0.0,
                    CurrentStep = 0,
                    TotalSteps = 5000,
                    Eta = "Position: #2"
                },
                new TrainingJobSummary
                {
                    Name = "Character LoRA Training",
                    Modality = "🖼️",
                    Status = "Failed",
                    Progress = 0.23,
                    CurrentStep = 460,
                    TotalSteps = 2000,
                    Eta = "OOM Error"
                }
            };

            // Initialize commands
            PauseAllCommand = new RelayCommand(_ => PauseAllJobs());
            StopAllCommand = new RelayCommand(_ => StopAllJobs());
            ExportReportsCommand = new RelayCommand(_ => ExportReports());
            PlaySampleCommand = new RelayCommand<string>(PlaySample);
        }

        private void OnMetricsUpdated(object? sender, SystemMetrics e)
        {
            CpuPercent = e.CpuUsagePercent;
            GpuPercent = e.GpuUsagePercent;
            RamUsedGb = e.RamUsedGb;
            RamTotalGb = e.RamTotalGb;
            OnPropertyChanged(nameof(CpuUsageText));
            OnPropertyChanged(nameof(GpuUsageText));
            OnPropertyChanged(nameof(RamUsageText));
            OnPropertyChanged(nameof(TotalGpuUsage));
        }

        public void SetCurrentJob(TrainingJob? job)
        {
            CurrentJob = job;
            OnPropertyChanged(nameof(HasJob));
        }

        private void PauseAllJobs()
        {
            System.Diagnostics.Debug.WriteLine("Pausing all active training jobs");
        }

        private void StopAllJobs()
        {
            System.Diagnostics.Debug.WriteLine("Stopping all active training jobs");
        }

        private void ExportReports()
        {
            System.Diagnostics.Debug.WriteLine("Exporting training reports to Markdown/JSON");
        }

        private void PlaySample(string? samplePath)
        {
            System.Diagnostics.Debug.WriteLine($"Playing sample: {samplePath}");
        }
    }

    // Supporting data model for job summaries
    public class TrainingJobSummary
    {
        public string Name { get; set; } = "";
        public string Modality { get; set; } = "";
        public string Status { get; set; } = "";
        public double Progress { get; set; }
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string Eta { get; set; } = "";
    }
}
