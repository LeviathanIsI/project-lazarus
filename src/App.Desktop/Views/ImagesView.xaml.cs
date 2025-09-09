using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Lazarus.Shared;

namespace Lazarus.Desktop.Views
{
    public partial class ImagesView : UserControl
    {
        // Dummy counters (bound in XAML)
        public int TotalImages { get; set; } = 0;
        public int GeneratedToday { get; set; } = 0;
        public double StorageUsedMb { get; set; } = 0.0;

        public ImagesView()
        {
            InitializeComponent();
            // Bind to self for simple dummy values
            DataContext = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Default mode selection
                if (ModeCombo.Items.Count > 0)
                    ModeCombo.SelectedIndex = 0;

                RefreshAssets();
                
                // Helpful tooltips showing actual directories
                ControlNetCombo.ToolTip  = LazarusPaths.GenAssets.ControlNet;
                StylePresetCombo.ToolTip = LazarusPaths.GenAssets.StylePresets;
                UpscalerCombo.ToolTip    = LazarusPaths.GenAssets.Upscale;
                VaeCombo.ToolTip         = LazarusPaths.GenAssets.Vae;
            }
            catch
            {
                // Swallow startup enumeration issues to keep UI reliable
            }
        }

        private void RefreshAssets()
        {
            ControlNetCombo.ItemsSource  = EnumerateFilesSafe(
                LazarusPaths.GenAssets.ControlNet,
                new[] { ".pt", ".pth", ".onnx", ".safetensors", ".bin" }
            );
            StylePresetCombo.ItemsSource = EnumerateFilesSafe(
                LazarusPaths.GenAssets.StylePresets,
                new[] { ".json", ".yaml", ".yml" }
            );
            UpscalerCombo.ItemsSource    = EnumerateFilesSafe(
                LazarusPaths.GenAssets.Upscale,
                new[] { ".pt", ".pth", ".onnx", ".bin" }
            );
            VaeCombo.ItemsSource         = EnumerateFilesSafe(
                LazarusPaths.GenAssets.Vae,
                new[] { ".pt", ".pth", ".safetensors", ".bin" }
            );
        }

        private static IEnumerable<string> EnumerateFilesSafe(string root, string[]? allowedExtensions = null)
        {
            try
            {
                if (!Directory.Exists(root)) return Array.Empty<string>();
                var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories);
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

        private void OnGenerateClick(object sender, RoutedEventArgs e)
        {
            // Show a placeholder PNG from app resources
            try
            {
                var res = TryFindResource("LazarusPngLogo32") as BitmapSource
                          ?? TryFindResource("LazarusPngLogo") as BitmapSource;

                if (res != null)
                {
                    PreviewImage.Source = res;
                    PlaceholderLabel.Visibility = Visibility.Collapsed;
                    // bump counters locally so UI feels responsive
                    TotalImages += 1;
                    GeneratedToday += 1;
                    StorageUsedMb += 0.001; // pretend small size
                    // Refresh binding targets
                    // Since we didn't implement INotifyPropertyChanged here, rebind by resetting DataContext
                    var dc = DataContext; DataContext = null; DataContext = dc;
                    return;
                }
            }
            catch { }

            // Fallback: simple pack URI if resource missing
            try
            {
                var uri = new Uri("pack://application:,,,/LazarusLogo.png", UriKind.Absolute);
                var bmp = new BitmapImage(uri);
                PreviewImage.Source = bmp;
                PlaceholderLabel.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // Leave placeholder label if we couldn't load an image
            }
        }
    }
}
