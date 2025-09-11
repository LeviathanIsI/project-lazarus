using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class JobDesignerViewModel : INotifyPropertyChanged
    {
        // Datasets presenter placeholder (could be a view-model or collection)
        public object? DatasetsPresenter { get; set; }

        // Configuration
        public ObservableCollection<string> Models { get; } = new() { "Select a model" };
        public ObservableCollection<string> Recipes { get; } = new() { "Full fine-tuning", "LoRA" };
        public string SelectedModel { get => _selectedModel; set { _selectedModel = value; OnPropertyChanged(); } }
        public string SelectedRecipe { get => _selectedRecipe; set { _selectedRecipe = value; OnPropertyChanged(); } }
        public string OutputPath { get => _outputPath; set { _outputPath = value; OnPropertyChanged(); } }
        private string _selectedModel = "Select a model";
        private string _selectedRecipe = "Full fine-tuning";
        private string _outputPath = string.Empty;

        // Resources
        public ObservableCollection<string> Gpus { get; } = new() { "GPU 0", "GPU 1" };
        public string? SelectedGpu { get => _selectedGpu; set { _selectedGpu = value; OnPropertyChanged(); } }
        private string? _selectedGpu = "GPU 0";
        public int BatchSize { get => _batchSize; set { _batchSize = value; OnPropertyChanged(); } }
        private int _batchSize = 1;
        public ObservableCollection<string> Precisions { get; } = new() { "fp16", "bf16", "fp32" };
        public string SelectedPrecision { get => _selectedPrecision; set { _selectedPrecision = value; OnPropertyChanged(); } }
        private string _selectedPrecision = "fp16";

        // Overview strip
        public string OverviewStatus { get => _overviewStatus; set { _overviewStatus = value; OnPropertyChanged(); } }
        public double OverviewProgress { get => _overviewProgress; set { _overviewProgress = value; OnPropertyChanged(); } }
        public string OverviewEta { get => _overviewEta; set { _overviewEta = value; OnPropertyChanged(); } }
        private string _overviewStatus = "Idle";
        private double _overviewProgress = 0.0;
        private string _overviewEta = "--:--";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

