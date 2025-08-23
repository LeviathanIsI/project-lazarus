using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels
{
    public class ControlNetsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _statusText = "Ready to configure ControlNets...";
        private bool _isLoading;
        private ControlNetModel? _selectedControlNet;
        private string _preprocessorType = "None";
        private double _weight = 1.0;
        private double _guidanceStart = 0.0;
        private double _guidanceEnd = 1.0;
        private string _inputImagePath = string.Empty;
        private bool _isEnabled = true;

        public ObservableCollection<ControlNetModel> AvailableControlNets { get; set; }
        public ObservableCollection<ControlNetModel> AppliedControlNets { get; set; }
        public ObservableCollection<string> PreprocessorTypes { get; set; }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ControlNetModel? SelectedControlNet
        {
            get => _selectedControlNet;
            set
            {
                _selectedControlNet = value;
                OnPropertyChanged();
            }
        }

        public string PreprocessorType
        {
            get => _preprocessorType;
            set
            {
                _preprocessorType = value;
                OnPropertyChanged();
            }
        }

        public double Weight
        {
            get => _weight;
            set
            {
                _weight = value;
                OnPropertyChanged();
            }
        }

        public double GuidanceStart
        {
            get => _guidanceStart;
            set
            {
                _guidanceStart = value;
                OnPropertyChanged();
            }
        }

        public double GuidanceEnd
        {
            get => _guidanceEnd;
            set
            {
                _guidanceEnd = value;
                OnPropertyChanged();
            }
        }

        public string InputImagePath
        {
            get => _inputImagePath;
            set
            {
                _inputImagePath = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public int AppliedControlNetsCount => AppliedControlNets.Count;

        public ICommand LoadControlNetsCommand { get; }
        public ICommand ApplyControlNetCommand { get; }
        public ICommand RemoveControlNetCommand { get; }
        public ICommand ClearAllControlNetsCommand { get; }
        public ICommand SelectInputImageCommand { get; }
        public ICommand RefreshCommand { get; }

        public ControlNetsViewModel()
        {
            AvailableControlNets = new ObservableCollection<ControlNetModel>();
            AppliedControlNets = new ObservableCollection<ControlNetModel>();
            PreprocessorTypes = new ObservableCollection<string>
            {
                "None",
                "Canny",
                "OpenPose",
                "Depth",
                "Normal Map",
                "Scribble",
                "Seg",
                "Lineart",
                "SoftEdge",
                "MLSD"
            };

            LoadControlNetsCommand = new ActionCommand(LoadControlNets);
            ApplyControlNetCommand = new ActionCommand<ControlNetModel>(ApplyControlNet);
            RemoveControlNetCommand = new ActionCommand<ControlNetModel>(RemoveControlNet);
            ClearAllControlNetsCommand = new ActionCommand(ClearAllControlNets);
            SelectInputImageCommand = new ActionCommand(SelectInputImage);
            RefreshCommand = new ActionCommand(LoadControlNets);

            // Load sample data
            LoadSampleControlNets();
        }

        private async void LoadControlNets()
        {
            IsLoading = true;
            StatusText = "Scanning for ControlNet models...";

            // Simulate loading ControlNets from filesystem
            await Task.Run(async () =>
            {
                await Task.Delay(1500); // Simulate scan time

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableControlNets.Clear();
                    LoadSampleControlNets();

                    IsLoading = false;
                    StatusText = $"Found {AvailableControlNets.Count} ControlNet models";
                });
            });
        }

        private void LoadSampleControlNets()
        {
            var sampleControlNets = new[]
            {
                new ControlNetModel
                {
                    Name = "control_v11p_sd15_canny",
                    Type = "Canny",
                    Description = "Edge detection using Canny algorithm for precise line control",
                    Version = "v1.1",
                    FileSize = "723 MB",
                    IsLoaded = false,
                    DefaultWeight = 1.0,
                    SupportedPreprocessors = new[] { "Canny" }
                },
                new ControlNetModel
                {
                    Name = "control_v11p_sd15_openpose",
                    Type = "OpenPose",
                    Description = "Human pose detection and control for character positioning",
                    Version = "v1.1",
                    FileSize = "723 MB",
                    IsLoaded = false,
                    DefaultWeight = 1.0,
                    SupportedPreprocessors = new[] { "OpenPose" }
                },
                new ControlNetModel
                {
                    Name = "control_v11f1p_sd15_depth",
                    Type = "Depth",
                    Description = "Depth map control for 3D spatial awareness and composition",
                    Version = "v1.1",
                    FileSize = "723 MB",
                    IsLoaded = true,
                    DefaultWeight = 0.8,
                    SupportedPreprocessors = new[] { "Depth", "Normal Map" }
                },
                new ControlNetModel
                {
                    Name = "control_v11p_sd15_scribble",
                    Type = "Scribble",
                    Description = "Rough sketch and scribble-based generation control",
                    Version = "v1.1",
                    FileSize = "723 MB",
                    IsLoaded = false,
                    DefaultWeight = 0.9,
                    SupportedPreprocessors = new[] { "Scribble", "SoftEdge" }
                },
                new ControlNetModel
                {
                    Name = "control_v11p_sd15_lineart",
                    Type = "Lineart",
                    Description = "Clean line art extraction and control for detailed drawings",
                    Version = "v1.1",
                    FileSize = "723 MB",
                    IsLoaded = false,
                    DefaultWeight = 1.0,
                    SupportedPreprocessors = new[] { "Lineart", "SoftEdge" }
                },
                new ControlNetModel
                {
                    Name = "control_v11p_sd15_seg",
                    Type = "Segmentation",
                    Description = "Semantic segmentation for precise object and region control",
                    Version = "v1.1",
                    FileSize = "723 MB",
                    IsLoaded = false,
                    DefaultWeight = 0.7,
                    SupportedPreprocessors = new[] { "Seg" }
                }
            };

            foreach (var controlNet in sampleControlNets)
            {
                AvailableControlNets.Add(controlNet);
            }
        }

        private void ApplyControlNet(ControlNetModel? controlNet)
        {
            if (controlNet == null) return;

            // Create a copy for the applied stack
            var appliedControlNet = new ControlNetModel
            {
                Name = controlNet.Name,
                Type = controlNet.Type,
                Description = controlNet.Description,
                Version = controlNet.Version,
                FileSize = controlNet.FileSize,
                IsLoaded = true,
                DefaultWeight = Weight,
                SupportedPreprocessors = controlNet.SupportedPreprocessors,
                PreprocessorType = PreprocessorType,
                InputImagePath = InputImagePath,
                Weight = Weight,
                GuidanceStart = GuidanceStart,
                GuidanceEnd = GuidanceEnd,
                IsEnabled = IsEnabled,
                Order = AppliedControlNets.Count + 1
            };

            AppliedControlNets.Add(appliedControlNet);
            controlNet.IsLoaded = true;

            StatusText = $"Applied {controlNet.Name} with {PreprocessorType} preprocessing";
            OnPropertyChanged(nameof(AppliedControlNetsCount));
        }

        private void RemoveControlNet(ControlNetModel? controlNet)
        {
            if (controlNet == null) return;

            AppliedControlNets.Remove(controlNet);

            // Update order numbers
            for (int i = 0; i < AppliedControlNets.Count; i++)
            {
                AppliedControlNets[i].Order = i + 1;
            }

            // Mark original as not loaded if no other instances
            var originalControlNet = AvailableControlNets.FirstOrDefault(x => x.Name == controlNet.Name);
            if (originalControlNet != null && !AppliedControlNets.Any(x => x.Name == controlNet.Name))
            {
                originalControlNet.IsLoaded = false;
            }

            StatusText = $"Removed {controlNet.Name} from stack";
            OnPropertyChanged(nameof(AppliedControlNetsCount));
        }

        private void ClearAllControlNets()
        {
            AppliedControlNets.Clear();

            // Mark all as not loaded
            foreach (var controlNet in AvailableControlNets)
            {
                controlNet.IsLoaded = false;
            }

            StatusText = "Cleared all applied ControlNets";
            OnPropertyChanged(nameof(AppliedControlNetsCount));
        }

        private void SelectInputImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InputImagePath = openFileDialog.FileName;
                StatusText = $"Selected input image: {System.IO.Path.GetFileName(InputImagePath)}";
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ControlNetModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isLoaded;
        private bool _isEnabled = true;
        private double _weight = 1.0;
        private double _guidanceStart = 0.0;
        private double _guidanceEnd = 1.0;

        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public double DefaultWeight { get; set; }
        public string[] SupportedPreprocessors { get; set; } = Array.Empty<string>();
        public string PreprocessorType { get; set; } = string.Empty;
        public string InputImagePath { get; set; } = string.Empty;
        public int Order { get; set; }

        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public double Weight
        {
            get => _weight;
            set
            {
                _weight = value;
                OnPropertyChanged();
            }
        }

        public double GuidanceStart
        {
            get => _guidanceStart;
            set
            {
                _guidanceStart = value;
                OnPropertyChanged();
            }
        }

        public double GuidanceEnd
        {
            get => _guidanceEnd;
            set
            {
                _guidanceEnd = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple command implementation to avoid external dependencies
    public class ActionCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public ActionCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    public class ActionCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public ActionCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}