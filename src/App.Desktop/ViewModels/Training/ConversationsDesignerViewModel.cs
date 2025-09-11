using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class ConversationsDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;
        
        public string Title => "Conversations Training";
        
        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }
        
        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Conversations };
        public ParameterBagProxy Params { get; }
        
        // Commands
        public ICommand ImportCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand ClearCommand { get; }
        
        public ConversationsDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);
            
            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            
            // Set default parameters
            Params["BaseModel"] = "llama-2-7b-chat";
            Params["TrainingType"] = "LoRA";
            Params["LearningRate"] = "2e-4";
            Params["ChatTemplate"] = "ChatML";
        }
        
        public void SetCurrentJob(TrainingJob? job)
        {
            CurrentJob = job;
            Params.SetCurrentJob(job);
            OnPropertyChanged(nameof(HasJob));
        }
        
        private async Task ImportAsync()
        {
            // TODO: Show file dialog for JSONL files
            System.Diagnostics.Debug.WriteLine("Import conversation dataset");
            await Task.CompletedTask;
        }
        
        private async Task CreateJobAsync()
        {
            if (HasJob) return;
            
            try
            {
                var job = await _trainingService.CreateJobAsync(Draft.Name, Draft.Modality);
                job.OutputPath = Params["OutputPath"];
                await _trainingService.UpdateJobAsync(job);
                
                System.Diagnostics.Debug.WriteLine($"Created conversation job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }
        
        private void Clear()
        {
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            // Reset defaults
            Params["BaseModel"] = "llama-2-7b-chat";
            Params["TrainingType"] = "LoRA";
            Params["LearningRate"] = "2e-4";
            Params["ChatTemplate"] = "ChatML";
        }
    }
}
