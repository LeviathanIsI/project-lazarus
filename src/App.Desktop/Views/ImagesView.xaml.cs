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

        private int _seed;
        public int Seed { get => _seed; set { _seed = value; OnPropertyChanged(nameof(Seed)); OnPropertyChanged(nameof(GenerateButtonText)); } }
        private bool _seedLocked;
        public bool SeedLocked { get => _seedLocked; set { _seedLocked = value; OnPropertyChanged(nameof(SeedLocked)); OnPropertyChanged(nameof(GenerateButtonText)); UpdateLockGlyph(); } }

        private bool _isRunning;
        public bool IsRunning { get => _isRunning; set { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); OnPropertyChanged(nameof(InputsEnabled)); OnPropertyChanged(nameof(GenerateButtonText)); } }
        public bool InputsEnabled => !IsRunning;
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

        // Image Runners catalog (recursively scanned under %LOCALAPPDATA%\Lazarus\Runners\Images)
        public ObservableCollection<RunnerCandidate> RunnerCatalog { get; } = new();
        private RunnerCandidate? _selectedRunner;
        public RunnerCandidate? SelectedRunner { get => _selectedRunner; set { _selectedRunner = value; OnPropertyChanged(nameof(SelectedRunner)); } }

        public ImagesView()
        {
            InitializeComponent();
            // Bind to self for simple dummy values
            DataContext = this;
            Seed = RandomNumberGenerator.GetInt32(0, int.MaxValue);

            try { _imageService = Lazarus.Desktop.App.ServiceProvider?.GetService(typeof(IImageService)) as IImageService; } catch { }
            try { RefreshRunnersCatalog(); } catch { }
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
                    "stable-diffusion" => new[] { "webui-user.bat", "webui.bat", "launch*.bat", "start*.bat" },
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

        private bool HasBackend() => _imageService != null; // no IsConfigured on interface; treat presence as configured

        private async void OnGenerateClick(object sender, RoutedEventArgs e)
        {
            // Show a placeholder PNG from app resources
            try
            {
                if (IsRunning) return;

                // simple validation
                var prompt = PromptBox.Text?.Trim();
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

                if (_cts.IsCancellationRequested)
                {
                    ProgressPercent = 0;
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
                    TotalImages += 1; GeneratedToday += 1; StorageUsedMb += 0.001;
                    ShowToast($"Rendered  seed {Seed}");
                    try { VisualStateManager.GoToState(GenerateButton, "Success", true); } catch { }
                }
            }
            catch (Exception ex)
            {
                ShowToast("Generation failed: " + ex.Message, isError: true);
                try { VisualStateManager.GoToState(GenerateButton, "Error", true); } catch { }
            }
            finally
            {
                _cts?.Dispose(); _cts = null;
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
            if (e.Key == Key.Space && !IsRunning)
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

        private void OnRefreshRunnersClick(object sender, RoutedEventArgs e)
        {
            try { RefreshRunnersCatalog(); ShowToast("Runners refreshed"); } catch { }
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

