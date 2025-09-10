using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Lazarus.Shared;
using Lazarus.Backend.Services;

namespace Lazarus.Desktop.Views
{
    public partial class ImagesView : UserControl, System.ComponentModel.INotifyPropertyChanged
    {
        
        private sealed class RelayCommand : ICommand
        {
            private readonly Action _exec;
            private readonly Func<bool>? _can;
            public RelayCommand(Action exec, Func<bool>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
            public void Execute(object? parameter) => _exec();
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        public sealed record RunnerCandidate(string Engine, string DisplayName, string ResolvedPath, string Entrypoint);

        // Dummy counters (bound in XAML)
        private int _totalImages;
        public int TotalImages { get => _totalImages; set { _totalImages = value; OnPropertyChanged(nameof(TotalImages)); } }
        private int _generatedToday;
        public int GeneratedToday { get => _generatedToday; set { _generatedToday = value; OnPropertyChanged(nameof(GeneratedToday)); } }
        private double _storageUsedMb;
        public double StorageUsedMb { get => _storageUsedMb; set { _storageUsedMb = value; OnPropertyChanged(nameof(StorageUsedMb)); } }

        public enum ImageMode { Txt2Img, Img2Img, Inpaint }
        private ImageMode _mode = ImageMode.Txt2Img;
        public ImageMode Mode { get => _mode; set { _mode = value; OnPropertyChanged(nameof(Mode)); OnPropertyChanged(nameof(ModeString)); } }
        public string ModeString => Mode.ToString();

        // Normalized runner parameters
        public ObservableCollection<string> ImageModels { get; } = new();
        private string? _selectedImageModel;
        public string? SelectedImageModel { get => _selectedImageModel; set { _selectedImageModel = value; OnPropertyChanged(nameof(SelectedImageModel)); } }

        public ObservableCollection<string> Samplers { get; } = new(new[] { "Euler", "EulerA", "DPM2", "DPM2S", "DDIM", "Heun", "PLMS", "DPM++" });
        private string _sampler = "Euler";
        public string Sampler { get => _sampler; set { _sampler = value; OnPropertyChanged(nameof(Sampler)); } }

        private int _batch = 1;
        public int Batch { get => _batch; set { _batch = Math.Max(1, value); OnPropertyChanged(nameof(Batch)); } }

        private int _threads = Math.Max(1, Environment.ProcessorCount / 2);
        public int Threads { get => _threads; set { _threads = Math.Max(0, value); OnPropertyChanged(nameof(Threads)); } }

        public ObservableCollection<string> Precisions { get; } = new(new[] { "fp32", "fp16", "bf16", "int8" });
        private string _precision = "fp16";
        public string Precision { get => _precision; set { _precision = value; OnPropertyChanged(nameof(Precision)); } }

        public ObservableCollection<string> Devices { get; } = new(new[] { "Auto", "CPU", "GPU" });
        private string _device = "Auto";
        public string Device { get => _device; set { _device = value; OnPropertyChanged(nameof(Device)); } }

        public ObservableCollection<string> OutputFormats { get; } = new(new[] { "png", "jpg", "webp" });
        private string _outputFormat = "png";
        public string OutputFormat { get => _outputFormat; set { _outputFormat = value; OnPropertyChanged(nameof(OutputFormat)); } }

        private string? _filenamePrefix;
        public string? FilenamePrefix { get => _filenamePrefix; set { _filenamePrefix = value; OnPropertyChanged(nameof(FilenamePrefix)); } }

        // Steps / CFG (surface via sliders)
        private int _steps = 30;
        public int Steps { get => _steps; set { _steps = Math.Max(1, value); OnPropertyChanged(nameof(Steps)); } }

        private double _cfgScale = 7.0;
        public double CfgScale { get => _cfgScale; set { _cfgScale = Math.Max(0.0, value); OnPropertyChanged(nameof(CfgScale)); } }

        // Advanced: multiple LoRAs with optional weights (comma-separated: name[:weight])
        private string? _loraList;
        public string? LoraList { get => _loraList; set { _loraList = value; OnPropertyChanged(nameof(LoraList)); } }

        // ControlNet input image (optional)
        private string? _controlNetInputPath;
        public string? ControlNetInputPath { get => _controlNetInputPath; set { _controlNetInputPath = value; OnPropertyChanged(nameof(ControlNetInputPath)); } }

        private int _seed;
        public int Seed { get => _seed; set { _seed = value; OnPropertyChanged(nameof(Seed)); OnPropertyChanged(nameof(GenerateButtonText)); } }
        private bool _seedLocked;
        public bool SeedLocked { get => _seedLocked; set { _seedLocked = value; OnPropertyChanged(nameof(SeedLocked)); OnPropertyChanged(nameof(GenerateButtonText)); UpdateLockGlyph(); } }

        private bool _isRunning;
        public bool IsRunning { get => _isRunning; set { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); OnPropertyChanged(nameof(InputsEnabled)); OnPropertyChanged(nameof(GenerateButtonText)); OnPropertyChanged(nameof(CanCancel)); } }
        public bool InputsEnabled => !IsRunning;
        // Run-state (explicit)
        private string _statusText = "Idle";
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(nameof(StatusText)); } }
        private double? _progress; // null = indeterminate
        public double? Progress { get => _progress; set { _progress = value; OnPropertyChanged(nameof(Progress)); } }
        public bool CanCancel => IsRunning;
        public ICommand? GenerateImagesCommand { get; private set; }
        public ICommand? CancelCommand { get; private set; }
        private int _progressPercent;
        public int ProgressPercent { get => _progressPercent; set { _progressPercent = value; OnPropertyChanged(nameof(ProgressPercent)); } }
        private bool _lastRunFailed;
        public bool LastRunFailed { get => _lastRunFailed; set { _lastRunFailed = value; OnPropertyChanged(nameof(LastRunFailed)); } }

        public string GenerateButtonText => IsRunning ? "Generatingâ€¦" : "Generate";

        public string? InitImagePath { get; set; }
        public string? MaskImagePath { get; set; }

        private CancellationTokenSource? _cts;

        public sealed class ToastItem { public Guid Id { get; } = Guid.NewGuid(); public string Text { get; set; } = string.Empty; public bool IsError { get; set; } }
        public ObservableCollection<ToastItem> Toasts { get; } = new();

        // Backend (resolved via DI when available)
        private IImageService? _imageService;
        private Lazarus.Desktop.Services.IOrchestratorRunnerClient? _runnerClient;
        private Lazarus.Desktop.Services.IOrchestratorClient? _orchestratorClient;
        private Lazarus.Shared.Settings.ISettingsService? _settingsService;
        private System.Diagnostics.Process? _imageRunnerProcess;
        private string? _imageRunnerOutLog;
        private string? _imageRunnerErrLog;

        // Image Runners catalog (recursively scanned under %LOCALAPPDATA%\Lazarus\Runners\Images)
        public ObservableCollection<RunnerCandidate> RunnerCatalog { get; } = new();
        public System.Collections.Generic.IEnumerable<RunnerCandidate> VisibleRunnerCatalog => RunnerCatalog;
        private RunnerCandidate? _selectedRunner;
        public RunnerCandidate? SelectedRunner
        {
            get => _selectedRunner;
            set
            {
                _selectedRunner = value;
                OnPropertyChanged(nameof(SelectedRunner));
                try
                {
                    var path = value?.ResolvedPath ?? string.Empty;
                    _ = _settingsService?.SetValueAsync("LastImageRunnerPath", path);
                }
                catch { }
            }
        }

        // Runner diagnostics (align with Models runner card bindings)
        public bool IsRunnerRunning { get => _isRunnerRunning; private set { _isRunnerRunning = value; OnPropertyChanged(nameof(IsRunnerRunning)); } }
        private bool _isRunnerRunning;
        public string? RunnerModelPath { get => _runnerModelPath; private set { _runnerModelPath = value; OnPropertyChanged(nameof(RunnerModelPath)); } }
        private string? _runnerModelPath;
        public int? RunnerPid { get => _runnerPid; private set { _runnerPid = value; OnPropertyChanged(nameof(RunnerPid)); } }
        private int? _runnerPid;
        public int? RunnerPort { get => _runnerPort; private set { _runnerPort = value; OnPropertyChanged(nameof(RunnerPort)); } }
        private int? _runnerPort;
        public string? RunnerExePath { get => _runnerExePath; private set { _runnerExePath = value; OnPropertyChanged(nameof(RunnerExePath)); } }
        private string? _runnerExePath;
        public string? RunnerErrLog { get => _runnerErrLog; private set { _runnerErrLog = value; OnPropertyChanged(nameof(RunnerErrLog)); } }
        private string? _runnerErrLog;
        public string? RunnerOutLog { get => _runnerOutLog; private set { _runnerOutLog = value; OnPropertyChanged(nameof(RunnerOutLog)); } }
        private string? _runnerOutLog;
        public string? RunnerStatusMessage { get => _runnerStatusMessage; private set { _runnerStatusMessage = value; OnPropertyChanged(nameof(RunnerStatusMessage)); } }
        private string? _runnerStatusMessage;

        // Commands (mirror Models UX)
        public Lazarus.Desktop.ViewModels.RelayCommand LoadSelectedRunnerCommand { get; private set; } = null!;
        public Lazarus.Desktop.ViewModels.RelayCommand UnloadRunnerCommand { get; private set; } = null!;

        public ImagesView()
        {
            InitializeComponent();
            // Bind to self for simple dummy values
            DataContext = this;
            // Commands for keyboard bindings
            GenerateImagesCommand = new RelayCommand(() => OnGenerateClick(this, new RoutedEventArgs()), () => !IsRunning);
            CancelCommand = new RelayCommand(() => OnCancelClick(this, new RoutedEventArgs()), () => IsRunning);
            Seed = RandomNumberGenerator.GetInt32(0, int.MaxValue);

            try { _imageService = Lazarus.Desktop.App.ServiceProvider?.GetService(typeof(IImageService)) as IImageService; } catch { }
            try { _runnerClient = Lazarus.Desktop.App.ServiceProvider?.GetService(typeof(Lazarus.Desktop.Services.IOrchestratorRunnerClient)) as Lazarus.Desktop.Services.IOrchestratorRunnerClient; } catch { }
            try { _orchestratorClient = Lazarus.Desktop.App.ServiceProvider?.GetService(typeof(Lazarus.Desktop.Services.IOrchestratorClient)) as Lazarus.Desktop.Services.IOrchestratorClient; } catch { }
            try { _settingsService = Lazarus.Desktop.App.ServiceProvider?.GetService(typeof(Lazarus.Shared.Settings.ISettingsService)) as Lazarus.Shared.Settings.ISettingsService; } catch { }
            try { if (_runnerClient != null) _runnerClient.RunnerStatusChanged += (_, s) => Dispatcher?.Invoke(() => ApplyStatus(s)); } catch { }
            try { RefreshRunnersCatalog(); } catch { }

            // Populate image models list
            try
            {
                ImageModels.Clear();
                // Only scan Stable Diffusion models (separate from LLM models)
                foreach (var m in EnumerateFilesSafe(LazarusPaths.GenAssets.StableDiffusionModels, new[] { ".safetensors", ".ckpt", ".ckp" }))
                    ImageModels.Add(m);
            }
            catch { }

            // Commands
            LoadSelectedRunnerCommand = new Lazarus.Desktop.ViewModels.RelayCommand(async () =>
            {
                try
                {
                    var path = SelectedRunner?.ResolvedPath ?? string.Empty;
                    if (_settingsService != null)
                    {
                        await _settingsService.SetValueAsync("LastImageRunnerPath", path).ConfigureAwait(true);
                    }
                    if (SelectedRunner is null)
                    {
                        RunnerStatusMessage = "No runner selected";
                        return;
                    }

                    // Stop previous image runner if any
                    await StopImageRunnerAsync().ConfigureAwait(true);

                    // Start selected engine entrypoint
                    var started = await StartImageRunnerAsync(SelectedRunner, string.Empty).ConfigureAwait(true);
                    RunnerStatusMessage = started ? $"Started {SelectedRunner.Engine} at {RunnerExePath}" : "Failed to start image runner";
                    if (started) ShowToast("Runner started"); else ShowToast("Failed to start runner", isError: true);
                }
                catch { }
            }, () => SelectedRunner != null);

            UnloadRunnerCommand = new Lazarus.Desktop.ViewModels.RelayCommand(async () =>
            {
                try
                {
                    await StopImageRunnerAsync().ConfigureAwait(true);
                    SelectedRunner = null;
                    if (_settingsService != null)
                        await _settingsService.SetValueAsync("LastImageRunnerPath", string.Empty).ConfigureAwait(true);
                    RunnerStatusMessage = "Runner cleared";
                    ShowToast("Runner cleared");
                }
                catch { }
            }, () => true);
        }

        private async Task<bool> StartImageRunnerAsync(RunnerCandidate candidate, string normalizedArgs)
        {
            try
            {
                string exe = candidate.Entrypoint;
                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) return false;

                var ext = Path.GetExtension(exe).ToLowerInvariant();
                var psi = new ProcessStartInfo();
                psi.WorkingDirectory = Path.GetDirectoryName(exe)!;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                // Per-engine launch arguments and command mapping
                var engine = (candidate.Engine ?? string.Empty).Trim().ToLowerInvariant();
                var desiredPort = GetDefaultPort(engine);
                var (fileName, arguments) = BuildLaunchCommand(engine, ext, exe, desiredPort);
                if (string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(arguments)) return false;
                psi.FileName = fileName;
                psi.Arguments = arguments;
                if (!string.IsNullOrWhiteSpace(normalizedArgs))
                {
                    try { psi.EnvironmentVariables["LAZARUS_IMAGE_RUNNER_EXTRA_ARGS"] = normalizedArgs; } catch { }
                }

                // Prepare logs
                try { Directory.CreateDirectory(LazarusPaths.SystemData.Logs); } catch { }
                var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                _imageRunnerOutLog = Path.Combine(LazarusPaths.SystemData.Logs, $"image-runner-{ts}.out.log");
                _imageRunnerErrLog = Path.Combine(LazarusPaths.SystemData.Logs, $"image-runner-{ts}.err.log");

                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (_, e) =>
                {
                    try
                    {
                        if (e.Data != null)
                        {
                            File.AppendAllText(_imageRunnerOutLog!, e.Data + Environment.NewLine);
                            OnRunnerOutputLine(engine, e.Data);
                        }
                    }
                    catch { }
                };
                proc.ErrorDataReceived  += (_, e) =>
                {
                    try
                    {
                        if (e.Data != null)
                        {
                            File.AppendAllText(_imageRunnerErrLog!, e.Data + Environment.NewLine);
                            OnRunnerOutputLine(engine, e.Data);
                        }
                    }
                    catch { }
                };
                proc.Exited += (_, __) => Dispatcher?.Invoke(() => { IsRunnerRunning = false; });

                var ok = proc.Start();
                if (!ok) return false;
                try { proc.BeginOutputReadLine(); proc.BeginErrorReadLine(); } catch { }

                _imageRunnerProcess = proc;
                IsRunnerRunning = true;
                RunnerPid = proc.Id;
                RunnerExePath = exe;
                RunnerOutLog = _imageRunnerOutLog;
                RunnerErrLog = _imageRunnerErrLog;
                OnPropertyChanged(nameof(RunnerPid));
                OnPropertyChanged(nameof(RunnerExePath));
                OnPropertyChanged(nameof(RunnerOutLog));
                OnPropertyChanged(nameof(RunnerErrLog));
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        private static int GetDefaultPort(string engine)
        {
            // Allow override via env var
            var env = Environment.GetEnvironmentVariable("LAZARUS_IMAGE_RUNNER_PORT");
            if (int.TryParse(env, out var p) && p > 0) return p;
            return engine switch
            {
                "comfyui" => 8188,
                "sdwebui" => 7860,
                "stable-diffusion" => 7860,
                "invokeai" => 9090,
                _ => 0
            };
        }

        private static (string fileName, string arguments) BuildLaunchCommand(string engine, string ext, string exe, int desiredPort)
        {
            string fileName;
            string args;
            // Common port args per engine
            string portArg = desiredPort > 0 ? desiredPort.ToString() : string.Empty;
            var extra = Environment.GetEnvironmentVariable("LAZARUS_IMAGE_RUNNER_EXTRA_ARGS") ?? string.Empty;

            if (ext is ".bat" or ".cmd")
            {
                fileName = "cmd.exe";
                var tail = engine switch
                {
                    "comfyui" => string.IsNullOrEmpty(portArg) ? string.Empty : $" --port {portArg} --listen 127.0.0.1",
                    // Do not force --api for SD engines; distributions vary.
                    "sdwebui" or "stable-diffusion" => string.Empty,
                    "invokeai" => string.IsNullOrEmpty(portArg) ? " --host 127.0.0.1" : $" --host 127.0.0.1 --port {portArg}",
                    _ => string.Empty
                };
                if (!string.IsNullOrWhiteSpace(extra)) tail += ($" " + extra);
                args = $"/c \"{exe}{tail}\"";
            }
            else if (ext == ".exe")
            {
                fileName = exe;
                args = engine switch
                {
                    "comfyui" => string.IsNullOrEmpty(portArg) ? string.Empty : $" --port {portArg} --listen 127.0.0.1",
                    // Do not force --api for SD engines; distributions vary.
                    "sdwebui" or "stable-diffusion" => string.Empty,
                    "invokeai" => string.IsNullOrEmpty(portArg) ? " --host 127.0.0.1" : $" --host 127.0.0.1 --port {portArg}",
                    _ => string.Empty
                };
                if (!string.IsNullOrWhiteSpace(extra)) args += ($" " + extra);
            }
            else if (ext == ".py")
            {
                fileName = "python";
                var tail = engine switch
                {
                    "comfyui" => string.IsNullOrEmpty(portArg) ? string.Empty : $" --port {portArg} --listen 127.0.0.1",
                    "invokeai" => string.IsNullOrEmpty(portArg) ? " --host 127.0.0.1" : $" --host 127.0.0.1 --port {portArg}",
                    _ => string.Empty
                };
                if (!string.IsNullOrWhiteSpace(extra)) tail += ($" " + extra);
                args = $"\"{exe}\"{tail}";
            }
            else
            {
                return (string.Empty, string.Empty);
            }
            return (fileName, args);
        }

        private void OnRunnerOutputLine(string engine, string line)
        {
            try
            {
                // Detect ready and URL/port
                // Common patterns
                if (line.IndexOf("Running on local URL:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Local URL:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Uvicorn running on", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Started server process", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("app started", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Dispatcher?.Invoke(() =>
                    {
                        RunnerStatusMessage = "Ready";
                        OnPropertyChanged(nameof(RunnerStatusMessage));
                    });
                }

                // Extract http port if present
                var m = System.Text.RegularExpressions.Regex.Match(line, @"http[s]?://[\w\.-]+:(\d+)");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var port))
                {
                    Dispatcher?.Invoke(() => { RunnerPort = port; OnPropertyChanged(nameof(RunnerPort)); });
                }
            }
            catch { }
        }

        private async Task StopImageRunnerAsync()
        {
            try
            {
                var p = _imageRunnerProcess;
                _imageRunnerProcess = null;
                if (p == null) return;
                if (!p.HasExited)
                {
                    try { p.CloseMainWindow(); } catch { }
                    try
                    {
                        if (!p.WaitForExit(1000))
                        {
                            p.Kill(true);
                            p.WaitForExit(2000);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                IsRunnerRunning = false;
                await Task.CompletedTask;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Default mode selection
                Mode = ImageMode.Txt2Img;
                RefreshAssets();
                
                // Helpful tooltips showing actual directories
                ControlNetCombo.ToolTip  = LazarusPaths.GenAssets.ControlNet;
                LoraCombo.ToolTip        = LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_LoRAs, LazarusPaths.GenAssets.StylePresets);
                EmbeddingCombo.ToolTip   = LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_Embeddings, LazarusPaths.GenAssets.StylePresets);
                HyperCombo.ToolTip       = LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_Hypernetworks, LazarusPaths.GenAssets.StylePresets);
                UpscalerCombo.ToolTip    = LazarusPaths.GenAssets.Upscale;
                VaeCombo.ToolTip         = LazarusPaths.GenAssets.Vae;
            }
            catch
            {
                // Swallow startup enumeration issues to keep UI reliable
            }
        }

        private void RefreshRunnersCatalog()
        {
            try
            {
                var list = ScanImageRunners();
                RunnerCatalog.Clear();
                foreach (var r in list) RunnerCatalog.Add(r);
                OnPropertyChanged(nameof(RunnerCatalog));
                OnPropertyChanged(nameof(VisibleRunnerCatalog));
                TryRestoreSelectedRunner();
            }
            catch { }
        }

        private void TryRestoreSelectedRunner()
        {
            try
            {
                var saved = _settingsService?.GetValue<string>("LastImageRunnerPath", string.Empty) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(saved))
                {
                    var cand = RunnerCatalog.FirstOrDefault(r => string.Equals(r.ResolvedPath, saved, StringComparison.OrdinalIgnoreCase));
                    if (cand != null) SelectedRunner = cand;
                }
            }
            catch { }
        }

        private static IReadOnlyList<RunnerCandidate> ScanImageRunners()
        {
            var results = new List<RunnerCandidate>();
            var root = LazarusPaths.Runners.RootDir;
            var imagesRoot = LazarusPaths.Runners.ImagesRoot;
            if (!Directory.Exists(root) && !Directory.Exists(imagesRoot)) return results;

            var engineDirs = new List<string>();
            void AddTop(string dir)
            {
                try
                {
                    if (Directory.Exists(dir))
                        engineDirs.AddRange(Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly));
                }
                catch { }
            }

            // Domain root and legacy flat roots
            AddTop(imagesRoot);
            AddTop(root);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var engineDir in engineDirs)
            {
                if (!seen.Add(engineDir)) continue;
                var engineKey = Path.GetFileName(engineDir).Trim();
                var key = engineKey.ToLowerInvariant();
                // Only consider known image engines; ignore domain folders like "Images", "Audio", etc.
                string[] patterns = key switch
                {
                    // Popular image engines with common entrypoints (Windows)
                    "stable-diffusion" => new[] { "webui-user.bat", "webui.bat", "launch*.bat", "start*.bat", "sd.exe", "sd*.exe" },
                    "sdwebui"          => new[] { "webui-user.bat", "webui.bat" },
                    "comfyui"          => new[] { "run*.bat", "main.py" },
                    "invokeai"         => new[] { "invoke*.bat", "invokeai*.exe" },
                    _ => Array.Empty<string>()
                };
                if (patterns.Length == 0) continue; // skip unknown folders completely

                foreach (var pattern in patterns)
                {
                    IEnumerable<string> files;
                    try { files = Directory.EnumerateFiles(engineDir, pattern, SearchOption.AllDirectories); }
                    catch { continue; }
                    foreach (var entry in files)
                    {
                        var folder = Path.GetDirectoryName(entry)!;
                        string leaf;
                        try { leaf = new DirectoryInfo(folder).Name; } catch { leaf = folder; }
                        results.Add(new RunnerCandidate(engineKey, leaf, folder, entry));
                    }
                }

                // If no entrypoints matched in this engine, skip it silently
            }

            return results
                .GroupBy(r => Tuple.Create(r.Engine.ToLowerInvariant(), r.ResolvedPath), EqualityComparer<Tuple<string, string>>.Default)
                .Select(g => g.First())
                .OrderBy(r => r.Engine)
                .ThenBy(r => r.DisplayName)
                .ToList();
        }

        private void RefreshAssets()
        {
            ControlNetCombo.ItemsSource  = EnumerateFilesSafe(
                LazarusPaths.GenAssets.ControlNet,
                new[] { ".pt", ".pth", ".onnx", ".safetensors", ".bin" }
            );
            // LoRA/Embedding/Hypernetwork split with legacy fallback
            var loraDir = LazarusPaths.ResolveFirstExisting(
                LazarusPaths.GenAssets.StylePresets_LoRAs,
                LazarusPaths.GenAssets.StylePresets // legacy flat
            );
            var embedDir = LazarusPaths.ResolveFirstExisting(
                LazarusPaths.GenAssets.StylePresets_Embeddings,
                LazarusPaths.GenAssets.StylePresets
            );
            var hyperDir = LazarusPaths.ResolveFirstExisting(
                LazarusPaths.GenAssets.StylePresets_Hypernetworks,
                LazarusPaths.GenAssets.StylePresets
            );
            SetComboItems(LoraCombo,   EnumerateFilesSafe(loraDir,  new[] { ".safetensors", ".pt", ".pth" }));
            SetComboItems(EmbeddingCombo, EnumerateFilesSafe(embedDir, new[] { ".pt", ".bin", ".txt", ".safetensors" }));
            SetComboItems(HyperCombo, EnumerateFilesSafe(hyperDir, new[] { ".pt", ".pth" }));
            UpscalerCombo.ItemsSource    = EnumerateFilesSafe(
                LazarusPaths.GenAssets.Upscale,
                new[] { ".pt", ".pth", ".onnx", ".bin", ".safetensors" }
            );
            VaeCombo.ItemsSource         = EnumerateFilesSafe(
                LazarusPaths.GenAssets.Vae,
                new[] { ".pt", ".pth", ".safetensors", ".bin" }
            );
            // Enable/disable if no items
            UpdateComboEnabled(LoraCombo);
            UpdateComboEnabled(EmbeddingCombo);
            UpdateComboEnabled(HyperCombo);
            UpdateComboEnabled(ControlNetCombo);
            UpdateComboEnabled(UpscalerCombo);
            UpdateComboEnabled(VaeCombo);
        }

        private static void SetComboItems(ComboBox combo, IEnumerable<string> items)
        {
            var list = items?.ToList() ?? new List<string>();
            if (list.Count == 0)
            {
                combo.ItemsSource = new[] { "(none found)" };
                combo.SelectedIndex = 0;
                combo.IsEnabled = false;
            }
            else
            {
                combo.ItemsSource = list;
                combo.IsEnabled = true;
            }
        }

        private static void UpdateComboEnabled(ComboBox combo)
        {
            try
            {
                if (combo.Items.Count == 1 && string.Equals(combo.Items[0]?.ToString(), "(none found)", StringComparison.Ordinal))
                    combo.IsEnabled = false;
            }
            catch { }
        }

        private static IEnumerable<string> EnumerateFilesSafe(string root, string[]? allowedExtensions = null)
        {
            try
            {
                if (!Directory.Exists(root)) return Array.Empty<string>();
                var files = Directory.EnumerateFiles(root, "*.*", SearchOption.TopDirectoryOnly);
                if (allowedExtensions != null && allowedExtensions.Length > 0)
                {
                    var set = new HashSet<string>(allowedExtensions.Select(e => e.ToLowerInvariant()));
                    files = files.Where(f => set.Contains(Path.GetExtension(f).ToLowerInvariant()));
                }
                return files
                        .Select(Path.GetFileName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n)
                        .ToArray();
            }
            catch { return Array.Empty<string>(); }
        }

        private bool HasBackend() => true; // one-shot local runner invocation; backend stub not required

        private async void OnGenerateClick(object sender, RoutedEventArgs e)
        {
            // Show a placeholder PNG from app resources
            try
            {
                if (IsRunning) return;

                // simple validation
                var prompt = PromptBox.Text; // keep spaces/newlines as-is
                if (Mode == ImageMode.Txt2Img && string.IsNullOrWhiteSpace(prompt))
                {
                    ShowToast("Enter a prompt.", isError: true);
                    return;
                }
                if (Mode != ImageMode.Txt2Img && string.IsNullOrWhiteSpace(InitImagePath))
                {
                    ShowToast("Provide an init image for this mode", isError: true);
                    return;
                }

                _cts = new CancellationTokenSource();
                IsRunning = true;
                StatusText = "Starting generation";
                Progress = null; // indeterminate
                ProgressPercent = 0;
                LastRunFailed = false;
                try { VisualStateManager.GoToState(GenerateButton, "Running", true); } catch { }

                // If no backend, warn and bail without touching preview
                if (!HasBackend())
                {
                    ShowToast("Image backend not configured. Configure a runner to generate.", isError: true);
                    return;
                }

                // Call backend (best effort) â€“ interface returns an output path
                string? outputPath = null;
                try
                {
                    // Map mode string
                    var modeStr = Mode.ToString();
                    // Minimal request shim: many backends accept a simple prompt; ours uses ImageJob indirectly.
                    // We expose only essential fields here to avoid tight coupling; service should handle defaults.
                    outputPath = await _imageService!.GenerateAsync(new Lazarus.Data.Entities.ImageJob
                    {
                        Mode = modeStr,
                        Prompt = prompt ?? string.Empty,
                        NegativePrompt = NegativePromptBox.Text ?? string.Empty,
                        Seed = Seed,
                        SourceImagePath = InitImagePath,
                        MaskImagePath = MaskImagePath
                    }).ConfigureAwait(true);
                }
                catch (TaskCanceledException) when (_cts?.IsCancellationRequested == true) { }

                // One-shot runner preferred: if backend didn't produce an output, try invoking runner now
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    try
                    {
                        if (SelectedRunner != null)
                        {
                            var normArgs = BuildNormalizedArgs(prompt ?? string.Empty);
                            var started = await StartImageRunnerAsync(SelectedRunner, normArgs).ConfigureAwait(true);
                            if (started)
                            {
                                var outDir = LazarusPaths.UserContent.GeneratedOutput;
                                try { Directory.CreateDirectory(outDir); } catch { }
                                var startTime = DateTime.UtcNow;
                                var exts = new HashSet<string>(new[] { ".png", ".jpg", ".jpeg", ".webp" }, StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < 120; i++)
                                {
                                    try
                                    {
                                        var candidate = Directory.EnumerateFiles(outDir, "*.*", SearchOption.TopDirectoryOnly)
                                            .Where(f => exts.Contains(Path.GetExtension(f)))
                                            .Select(f => new FileInfo(f))
                                            .OrderByDescending(fi => fi.LastWriteTimeUtc)
                                            .FirstOrDefault();
                                        if (candidate != null && candidate.LastWriteTimeUtc >= startTime.AddSeconds(-2))
                                        {
                                            outputPath = candidate.FullName;
                                            break;
                                        }
                                    }
                                    catch { }
                                    await Task.Delay(1000);
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (_cts.IsCancellationRequested)
                {
                    ProgressPercent = 0;
                    StatusText = "Canceled.";
                    ShowToast("Generation canceled", isError: true);
                    try { VisualStateManager.GoToState(GenerateButton, "Idle", true); } catch { }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.UriSource = new Uri(outputPath, UriKind.Absolute);
                            bmp.EndInit();
                            bmp.Freeze();
                            PreviewImage.Source = bmp;
                            PlaceholderLabel.Visibility = Visibility.Collapsed;
                        }
                        catch (Exception ex)
                        {
                            ShowToast("Failed to load generated image: " + ex.Message, isError: true);
                        }
                    }
                    else
                    {
                        ShowToast("Generation returned no image.", isError: true);
                    }
                    StatusText = "Done.";
                    TotalImages += 1; GeneratedToday += 1; StorageUsedMb += 0.001;
                    ShowToast($"Rendered  seed {Seed}");
                    try { VisualStateManager.GoToState(GenerateButton, "Success", true); } catch { }
                }
            }
            catch (Exception ex)
            {
                StatusText = "Generation failed.";
                ShowToast("Generation failed: " + ex.Message, isError: true);
                try { VisualStateManager.GoToState(GenerateButton, "Error", true); } catch { }
            }
            finally
            {
                _cts?.Dispose(); _cts = null;
                Progress = null;
                IsRunning = false;
                await Task.Delay(500);
                try { VisualStateManager.GoToState(GenerateButton, "Idle", true); } catch { }
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e) => _cts?.Cancel();

        private void OnRandomizeSeed(object sender, RoutedEventArgs e)
        {
            if (SeedLocked) { ShowToast("Seed is locked", isError: true); return; }
            Seed = RandomNumberGenerator.GetInt32(0, int.MaxValue);
        }

        private void OnToggleSeedLock(object sender, RoutedEventArgs e)
        {
            SeedLocked = !SeedLocked;
        }

        private void UpdateLockGlyph()
        {
            if (LockBtn != null)
                LockBtn.Content = SeedLocked ? "ðŸ”’" : "ðŸ”“";
        }

        private void OnModeTxt2Img(object sender, RoutedEventArgs e) { Mode = ImageMode.Txt2Img; }
        private void OnModeImg2Img(object sender, RoutedEventArgs e) { Mode = ImageMode.Img2Img; }
        private void OnModeInpaint(object sender, RoutedEventArgs e) { Mode = ImageMode.Inpaint; }

        private void OnSeedPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter triggers Generate; plain Enter stays for new lines
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Enter && !IsRunning)
            {
                OnGenerateClick(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.O)
            {
                TryOpenInitImage();
                e.Handled = true;
            }
            else if (e.Key == Key.D1 || e.Key == Key.NumPad1) { Mode = ImageMode.Txt2Img; e.Handled = true; }
            else if (e.Key == Key.D2 || e.Key == Key.NumPad2) { Mode = ImageMode.Img2Img; e.Handled = true; }
            else if (e.Key == Key.D3 || e.Key == Key.NumPad3) { Mode = ImageMode.Inpaint; e.Handled = true; }
        }

        private void TryOpenInitImage()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*",
                    Multiselect = false
                };
                if (dlg.ShowDialog() == true)
                {
                    InitImagePath = dlg.FileName;
                    Mode = ImageMode.Img2Img;
                    PlaceholderLabel.Text = System.IO.Path.GetFileName(InitImagePath);
                }
            }
            catch { }
        }

        private void OnBrowseControlNetInput(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*",
                    Multiselect = false
                };
                if (dlg.ShowDialog() == true)
                {
                    ControlNetInputPath = dlg.FileName;
                }
            }
            catch { }
        }

        private void OnDropZoneDragEnter(object sender, DragEventArgs e) => OnDropZoneDragOver(sender, e);
        private void OnDropZoneDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths != null && paths.Length > 0 && IsSupportedImage(paths[0]))
                {
                    e.Effects = DragDropEffects.Copy; e.Handled = true; return;
                }
            }
            e.Effects = DragDropEffects.None; e.Handled = true;
        }
        private void OnDropZoneDrop(object sender, DragEventArgs e)
        {
            try
            {
                // Ripple effect
                AnimateDropRipple();
                var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (paths == null || paths.Length == 0) return;
                var file = paths[0];
                if (!IsSupportedImage(file)) { ShowToast("Unsupported file type", isError: true); return; }

                if (Mode == ImageMode.Txt2Img) Mode = ImageMode.Img2Img;
                InitImagePath = file;
                PlaceholderLabel.Text = System.IO.Path.GetFileName(file);

                if (Mode == ImageMode.Inpaint && IsPngWithAlpha(file))
                {
                    MaskImagePath = file;
                }
            }
            catch (Exception ex) { ShowToast("Drop failed: " + ex.Message, isError: true); }
        }

        private void OnDropZoneDragLeave(object sender, DragEventArgs e)
        {
            try
            {
                DZ1.Color = (Color)ColorConverter.ConvertFromString("#101319");
                DZ2.Color = (Color)ColorConverter.ConvertFromString("#07090C");
            }
            catch { }
        }

        private void AnimateDropRipple()
        {
            try
            {
                var sb = new System.Windows.Media.Animation.Storyboard();
                var a1 = new System.Windows.Media.Animation.DoubleAnimation(1.0, 1.02, TimeSpan.FromMilliseconds(120)) { AutoReverse = true };
                var a2 = new System.Windows.Media.Animation.DoubleAnimation(1.0, 1.02, TimeSpan.FromMilliseconds(120)) { AutoReverse = true };
                System.Windows.Media.Animation.Storyboard.SetTarget(a1, DropScale);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(a1, new PropertyPath(ScaleTransform.ScaleXProperty));
                System.Windows.Media.Animation.Storyboard.SetTarget(a2, DropScale);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(a2, new PropertyPath(ScaleTransform.ScaleYProperty));
                sb.Children.Add(a1); sb.Children.Add(a2);
                sb.Begin();
            }
            catch { }
        }

        private void OnDropZonePreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var element = (FrameworkElement)sender;
                var pos = e.GetPosition(element);
                var cx = element.ActualWidth / 2.0;
                var cy = element.ActualHeight / 2.0;
                var dx = pos.X - cx;
                var dy = pos.Y - cy;
                var max = Math.Max(element.ActualWidth, element.ActualHeight);
                var dist = Math.Sqrt(dx * dx + dy * dy);
                var scale = 1.0 + Math.Min(0.01, dist / max * 0.02);
                DropTranslate.X = dx * 0.02;
                DropTranslate.Y = dy * 0.02;
                DropScale.ScaleX = scale;
                DropScale.ScaleY = scale;
            }
            catch { }
        }

        private static bool IsSupportedImage(string path)
        {
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp";
        }

        private static bool IsPngWithAlpha(string path)
        {
            try
            {
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return false;
                using var fs = File.OpenRead(path);
                var dec = new PngBitmapDecoder(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var frame = dec.Frames[0];
                var fmt = frame.Format;
                return fmt == PixelFormats.Pbgra32 || fmt == PixelFormats.Bgra32 || fmt == PixelFormats.Rgba64 || fmt == PixelFormats.Prgba64;
            }
            catch { return false; }
        }

        // Build normalized CLI flags for image runners
        private string BuildNormalizedArgs(string prompt)
        {
            var sb = new System.Text.StringBuilder();
            string q(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";

            // Core
            if (!string.IsNullOrWhiteSpace(SelectedImageModel))
            {
                var modelPath = Path.Combine(LazarusPaths.GenAssets.StableDiffusionModels, SelectedImageModel);
                sb.Append(" --model ").Append(q(modelPath));
            }
            if (!string.IsNullOrWhiteSpace(prompt)) sb.Append(" --prompt ").Append(q(prompt));
            if (!string.IsNullOrWhiteSpace(NegativePromptBox.Text)) sb.Append(" --negative ").Append(q(NegativePromptBox.Text));
            sb.Append(" --seed ").Append(Seed);
            var stepsVal = 30; // default if not bound here
            sb.Append(" --steps ").Append(stepsVal);
            if (!string.IsNullOrWhiteSpace(Sampler)) sb.Append(" --sampler ").Append(q(Sampler));
            sb.Append(" --cfg ").Append(7.0);

            // Dimensions / output
            sb.Append(" --W ").Append(Width);
            sb.Append(" --H ").Append(Height);
            sb.Append(" --outdir ").Append(q(LazarusPaths.UserContent.GeneratedOutput));
            if (!string.IsNullOrWhiteSpace(OutputFormat)) sb.Append(" --format ").Append(OutputFormat.ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(FilenamePrefix)) sb.Append(" --prefix ").Append(q(FilenamePrefix));

            // Model add-ons
            var vaeItem = VaeCombo?.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(vaeItem) && !vaeItem!.Contains("(none", StringComparison.OrdinalIgnoreCase))
            {
                var vaePath = Path.Combine(LazarusPaths.GenAssets.Vae, vaeItem!);
                sb.Append(" --vae ").Append(q(vaePath));
            }
            var loraItem = LoraCombo?.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(loraItem) && !loraItem!.Contains("(none", StringComparison.OrdinalIgnoreCase))
            {
                var loraPath = Path.Combine(LazarusPaths.GenAssets.StylePresets, loraItem!);
                sb.Append(" --loras ").Append(q(loraPath));
            }
            var cnItem = ControlNetCombo?.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(cnItem) && !cnItem!.Contains("(none", StringComparison.OrdinalIgnoreCase))
            {
                var cnPath = Path.Combine(LazarusPaths.GenAssets.ControlNet, cnItem!);
                sb.Append(" --controlnet ").Append(q(cnPath));
            }

            // Performance / device
            if (Threads > 0) sb.Append(" --threads ").Append(Threads);
            if (Batch > 1) sb.Append(" --batch ").Append(Batch);
            if (!string.IsNullOrWhiteSpace(Device) && !string.Equals(Device, "Auto", StringComparison.OrdinalIgnoreCase)) sb.Append(" --device ").Append(Device.ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(Precision)) sb.Append(" --precision ").Append(Precision.ToLowerInvariant());

            // Extras
            if (!string.Equals(Mode.ToString(), "Txt2Img", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(InitImagePath))
            {
                sb.Append(" --init-img ").Append(q(InitImagePath!));
                if (!string.IsNullOrWhiteSpace(MaskImagePath)) sb.Append(" --mask ").Append(q(MaskImagePath!));
                // Strength optional; not bound in this view yet
            }

            return sb.ToString();
        }

        private void EnqueueToast(string text, bool isError = false)
        {
            var item = new ToastItem { Text = text, IsError = isError };
            Toasts.Add(item);
            var _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                Dispatcher.Invoke(() => Toasts.Remove(item));
            });
        }

        private async void OnRefreshRunnersClick(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshRunnersCatalog();
                if (_runnerClient != null)
                {
                    var s = await _runnerClient.GetStatusAsync().ConfigureAwait(true);
                    ApplyStatus(s);
                }
                ShowToast("Runners refreshed");
            }
            catch { }
        }

        private void ApplyStatus(Lazarus.Desktop.Services.RunnerProcessStatus s)
        {
            try
            {
                IsRunnerRunning = s.IsRunning;
                RunnerModelPath = s.ModelPath;
                RunnerPid = s.Pid;
                RunnerPort = s.Port;
                RunnerExePath = s.ExePath;
                RunnerErrLog = s.ErrLog;
                RunnerOutLog = s.OutLog;
                RunnerStatusMessage = null;
            }
            catch { }
        }

        private void ShowToast(string msg, bool isError = false) => EnqueueToast(msg, isError);

        private void OnOpenControlNetFolder(object sender, RoutedEventArgs e) => OpenFolderSafe(LazarusPaths.GenAssets.ControlNet);
        private void OnOpenLoRAFolder(object sender, RoutedEventArgs e)
            => OpenFolderSafe(LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_LoRAs, LazarusPaths.GenAssets.StylePresets));
        private void OnOpenEmbeddingFolder(object sender, RoutedEventArgs e)
            => OpenFolderSafe(LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_Embeddings, LazarusPaths.GenAssets.StylePresets));
        private void OnOpenHyperFolder(object sender, RoutedEventArgs e)
            => OpenFolderSafe(LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_Hypernetworks, LazarusPaths.GenAssets.StylePresets));

        // Empty-state selection handlers: open folder when placeholder clicked
        private void OnLoraSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TryOpenIfPlaceholder((ComboBox)sender, LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_LoRAs, LazarusPaths.GenAssets.StylePresets));
        }
        private void OnEmbeddingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TryOpenIfPlaceholder((ComboBox)sender, LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_Embeddings, LazarusPaths.GenAssets.StylePresets));
        }
        private void OnHyperSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TryOpenIfPlaceholder((ComboBox)sender, LazarusPaths.ResolveFirstExisting(LazarusPaths.GenAssets.StylePresets_Hypernetworks, LazarusPaths.GenAssets.StylePresets));
        }
        private static void TryOpenIfPlaceholder(ComboBox combo, string path)
        {
            try
            {
                var s = combo.SelectedItem as string;
                if (string.Equals(s, "Configureâ€¦", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "(none found)", StringComparison.OrdinalIgnoreCase) || s?.StartsWith("Configure ") == true)
                {
                    // Do not open folders automatically based on selection.
                    // Leave explicit opening to dedicated buttons/commands.
                    combo.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void OnOpenUpscaleFolder(object sender, RoutedEventArgs e) => OpenFolderSafe(LazarusPaths.GenAssets.Upscale);
        private void OnOpenVaeFolder(object sender, RoutedEventArgs e) => OpenFolderSafe(LazarusPaths.GenAssets.Vae);
        private static void OpenFolderSafe(string path)
        {
            try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true }); } catch { }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}

