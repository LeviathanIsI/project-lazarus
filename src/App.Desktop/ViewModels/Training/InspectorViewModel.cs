using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lazarus.Shared.Contracts;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class InspectorViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ITrainingService _trainingService;
        private TrainingJob? _currentJob;
        
        private string _selectionSummary = "No selection";
        public string SelectionSummary { get => _selectionSummary; set => SetProperty(ref _selectionSummary, value); }
        
        private object? _selectedItem;
        public object? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        
        // Job details
        private string? _jobName;
        public string? JobName { get => _jobName; set => SetProperty(ref _jobName, value); }
        
        private TrainingModality _jobModality;
        public TrainingModality JobModality { get => _jobModality; set => SetProperty(ref _jobModality, value); }
        
        private TrainingStatus _jobStatus;
        public TrainingStatus JobStatus { get => _jobStatus; set => SetProperty(ref _jobStatus, value); }
        
        private DateTime _jobCreated;
        public DateTime JobCreated { get => _jobCreated; set => SetProperty(ref _jobCreated, value); }
        
        private DateTime _jobModified;
        public DateTime JobModified { get => _jobModified; set => SetProperty(ref _jobModified, value); }
        
        private int _currentEpoch;
        public int CurrentEpoch { get => _currentEpoch; set => SetProperty(ref _currentEpoch, value); }
        
        private long _currentStep;
        public long CurrentStep { get => _currentStep; set => SetProperty(ref _currentStep, value); }
        
        private string? _estimatedTimeRemaining;
        public string? EstimatedTimeRemaining { get => _estimatedTimeRemaining; set => SetProperty(ref _estimatedTimeRemaining, value); }
        
        private string? _lastError;
        public string? LastError { get => _lastError; set => SetProperty(ref _lastError, value); }
        
        // Warnings and tips
        private string? _warnings;
        public string? Warnings { get => _warnings; set => SetProperty(ref _warnings, value); }
        
        private string? _recommendations;
        public string? Recommendations { get => _recommendations; set => SetProperty(ref _recommendations, value); }
        
        public InspectorViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
        }
        
        public void SetSelectedJob(TrainingJob? job)
        {
            _currentJob = job;
            UpdateJobDetails();
        }
        
        public void SetSelectedItem(object? item)
        {
            SelectedItem = item;
            UpdateSelectionSummary(item);
        }
        
        public void Dispose()
        {
            // TODO(training): Clean up any subscriptions
        }
        
        private void UpdateJobDetails()
        {
            if (_currentJob == null)
            {
                JobName = null;
                SelectionSummary = "No job selected";
                return;
            }
            
            JobName = _currentJob.Name;
            JobModality = _currentJob.Modality;
            JobStatus = _currentJob.Status;
            JobCreated = _currentJob.Created;
            JobModified = _currentJob.Modified;
            CurrentEpoch = _currentJob.CurrentEpoch;
            CurrentStep = _currentJob.CurrentStep;
            EstimatedTimeRemaining = _currentJob.EstimatedTimeRemaining?.ToString(@"hh\:mm\:ss");
            LastError = _currentJob.LastError;
            
            SelectionSummary = $"{_currentJob.Name} ({_currentJob.Modality})";
            
            // TODO(training): Generate warnings and recommendations based on job state
            GenerateWarningsAndRecommendations();
        }
        
        private void UpdateSelectionSummary(object? item)
        {
            if (_currentJob == null)
            {
                SelectionSummary = "No job selected";
                return;
            }
            
            SelectionSummary = item switch
            {
                TrainingDatasetRef dataset => $"Dataset: {dataset.Name} ({dataset.Stats.TotalItems:N0} items)",
                TrainingConfig config => $"Config: {config.Recipe} on {config.ModelId}",
                TrainingResources resources => $"Resources: {resources.GpuIds.Count} GPUs, batch size {resources.BatchSize}",
                TrainingJob job => $"Job: {job.Name} ({job.Status})",
                _ => $"Job: {_currentJob.Name} ({_currentJob.Modality})"
            };
        }
        
        private void GenerateWarningsAndRecommendations()
        {
            if (_currentJob == null) return;
            
            var warnings = new System.Text.StringBuilder();
            var recommendations = new System.Text.StringBuilder();
            
            // Check for common issues
            if (_currentJob.Status == TrainingStatus.Failed && !string.IsNullOrEmpty(_currentJob.LastError))
            {
                warnings.AppendLine($"Job failed: {_currentJob.LastError}");
                
                if (_currentJob.LastError.Contains("OOM") || _currentJob.LastError.Contains("out of memory"))
                {
                    recommendations.AppendLine("• Reduce batch size or enable gradient accumulation");
                    recommendations.AppendLine("• Use gradient checkpointing to save memory");
                    recommendations.AppendLine("• Consider using a lower precision (FP16/INT8)");
                }
                else if (_currentJob.LastError.Contains("NaN") || _currentJob.LastError.Contains("loss"))
                {
                    recommendations.AppendLine("• Reduce learning rate");
                    recommendations.AppendLine("• Add gradient clipping");
                    recommendations.AppendLine("• Check dataset for corrupted samples");
                }
            }
            
            if (_currentJob.Status == TrainingStatus.Running && _currentJob.CurrentStep > 1000)
            {
                // TODO(training): Check for plateau detection
                recommendations.AppendLine("• Monitor validation loss for overfitting");
                recommendations.AppendLine("• Consider early stopping if no improvement");
            }
            
            Warnings = warnings.Length > 0 ? warnings.ToString().Trim() : null;
            Recommendations = recommendations.Length > 0 ? recommendations.ToString().Trim() : null;
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