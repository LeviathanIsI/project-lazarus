using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Shared.Contracts;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class MonitorDockViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ITrainingService _trainingService;
        private readonly List<IDisposable> _disposables = new();
        private TrainingJob? _currentJob;

        // Metrics tab
        public ObservableCollection<MetricSeries> MetricSeries { get; } = new();

        // Logs tab
        public ObservableCollection<TrainingLogEvent> Logs { get; } = new();

        private string _logsFilter = "";
        public string LogsFilter { get => _logsFilter; set => SetProperty(ref _logsFilter, value); }

        private bool _autoscroll = true;
        public bool Autoscroll { get => _autoscroll; set => SetProperty(ref _autoscroll, value); }

        // System tab - gauges
        private double _vramUsagePercent;
        public double VramUsagePercent { get => _vramUsagePercent; set => SetProperty(ref _vramUsagePercent, value); }

        private double _cpuUsagePercent;
        public double CpuUsagePercent { get => _cpuUsagePercent; set => SetProperty(ref _cpuUsagePercent, value); }

        private double _diskUsagePercent;
        public double DiskUsagePercent { get => _diskUsagePercent; set => SetProperty(ref _diskUsagePercent, value); }

        private double _ioUsagePercent;
        public double IoUsagePercent { get => _ioUsagePercent; set => SetProperty(ref _ioUsagePercent, value); }

        // Auto-expand triggers
        private bool _autoExpandOnError;
        public bool AutoExpandOnError { get => _autoExpandOnError; set => SetProperty(ref _autoExpandOnError, value); }

        private bool _autoExpandOnPlateau;
        public bool AutoExpandOnPlateau { get => _autoExpandOnPlateau; set => SetProperty(ref _autoExpandOnPlateau, value); }

        private bool _autoExpandOnOOM;
        public bool AutoExpandOnOOM { get => _autoExpandOnOOM; set => SetProperty(ref _autoExpandOnOOM, value); }

        // Commands
        public ICommand ClearLogsCommand { get; }
        public ICommand CopyLogsCommand { get; }
        public ICommand SaveLogsCommand { get; }
        public ICommand ToggleMetricCommand { get; }

        public MonitorDockViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));

            ClearLogsCommand = new RelayCommand(_ => ClearLogs());
            CopyLogsCommand = new RelayCommand(_ => CopyLogs());
            SaveLogsCommand = new RelayCommand(_ => SaveLogs());
            ToggleMetricCommand = new RelayCommand<string>(metricName => ToggleMetric(metricName));

            // TODO(training): Initialize default metric series
            InitializeMetricSeries();
        }

        public void SetSelectedJob(TrainingJob? job)
        {
            if (_currentJob == job) return;

            // Unsubscribe from previous job
            if (_currentJob != null)
            {
                // TODO(training): Unsubscribe from streams
            }

            _currentJob = job;

            if (job != null)
            {
                // TODO(training): Subscribe to new job streams
                SubscribeToJobStreams(job);
            }
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }

        private void InitializeMetricSeries()
        {
            MetricSeries.Add(new MetricSeries { Name = "Training Loss", Color = "#FF6B46C1", Visible = true });
            MetricSeries.Add(new MetricSeries { Name = "Validation Loss", Color = "#FF3B82F6", Visible = true });
            MetricSeries.Add(new MetricSeries { Name = "Learning Rate", Color = "#FF10B981", Visible = true });
            MetricSeries.Add(new MetricSeries { Name = "VRAM Usage", Color = "#FFF59E0B", Visible = true });
        }

        private void SubscribeToJobStreams(TrainingJob job)
        {
            try
            {
                // TODO(training): Subscribe to metrics stream
                // var metricsSubscription = _trainingService.GetMetricsStream(job.Id)
                //     .Subscribe(OnMetricsReceived);
                // _disposables.Add(metricsSubscription);

                // TODO(training): Subscribe to logs stream  
                // var logsSubscription = _trainingService.GetLogsStream(job.Id)
                //     .Subscribe(OnLogReceived);
                // _disposables.Add(logsSubscription);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to subscribe to job streams: {ex.Message}");
            }
        }

        private void OnMetricsReceived(TrainingMetricsSnapshot metrics)
        {
            // Update system gauges
            VramUsagePercent = (double)metrics.VramUsedBytes / (1024 * 1024 * 1024); // Convert to GB for percentage
            CpuUsagePercent = metrics.CpuUsagePercent;

            // Update metric series
            foreach (var series in MetricSeries)
            {
                var point = new MetricPoint
                {
                    Timestamp = metrics.Timestamp,
                    Step = metrics.GlobalStep,
                    Value = series.Name switch
                    {
                        "Training Loss" => metrics.Loss,
                        "Learning Rate" => metrics.LearningRate,
                        "VRAM Usage" => VramUsagePercent,
                        _ => 0
                    }
                };

                series.Points.Add(point);

                // Keep only last 1000 points for performance
                while (series.Points.Count > 1000)
                {
                    series.Points.RemoveAt(0);
                }
            }

            // Check auto-expand triggers
            CheckAutoExpandTriggers(metrics);
        }

        private void OnLogReceived(TrainingLogEvent logEvent)
        {
            Logs.Add(logEvent);

            // Keep only last 10000 logs for performance
            while (Logs.Count > 10000)
            {
                Logs.RemoveAt(0);
            }

            // Check for error triggers
            if (logEvent.Level == LogLevel.Error)
            {
                AutoExpandOnError = true;
            }
            else if (logEvent.Message.Contains("OOM") || logEvent.Message.Contains("out of memory"))
            {
                AutoExpandOnOOM = true;
            }
        }

        private void CheckAutoExpandTriggers(TrainingMetricsSnapshot metrics)
        {
            // TODO(training): Implement plateau detection
            // TODO(training): Implement NaN detection
            // TODO(training): Implement OOM detection from metrics
        }

        private void ClearLogs()
        {
            Logs.Clear();
        }

        private void CopyLogs()
        {
            // TODO(training): Copy logs to clipboard
            var logsText = string.Join(Environment.NewLine, Logs.Select(l => $"{l.Timestamp:HH:mm:ss} [{l.Level}] {l.Message}"));
            System.Diagnostics.Debug.WriteLine($"Would copy {logsText.Length} characters to clipboard");
        }

        private void SaveLogs()
        {
            // TODO(training): Show save file dialog and save logs
            System.Diagnostics.Debug.WriteLine("Would show save logs dialog");
        }

        private void ToggleMetric(string? metricName)
        {
            var metric = MetricSeries.FirstOrDefault(m => m.Name == metricName);
            if (metric != null)
            {
                metric.Visible = !metric.Visible;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}