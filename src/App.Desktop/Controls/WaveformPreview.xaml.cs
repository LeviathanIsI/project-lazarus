using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lazarus.Desktop.Controls;

public partial class WaveformPreview : UserControl
{
    public WaveformPreview() { InitializeComponent(); Loaded += OnLoaded; }

    public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register(
        nameof(SourcePath), typeof(string), typeof(WaveformPreview), new PropertyMetadata(null, OnSourceChanged));
    public string? SourcePath { get => (string?)GetValue(SourcePathProperty); set => SetValue(SourcePathProperty, value); }

    public static readonly DependencyProperty SelectionBrushProperty = DependencyProperty.Register(
        nameof(SelectionBrush), typeof(Brush), typeof(WaveformPreview), new PropertyMetadata(Brushes.DeepSkyBlue));
    public Brush SelectionBrush { get => (Brush)GetValue(SelectionBrushProperty); set => SetValue(SelectionBrushProperty, value); }

    public static readonly DependencyProperty SelectionStartSecProperty = DependencyProperty.Register(
        nameof(SelectionStartSec), typeof(double), typeof(WaveformPreview), new PropertyMetadata(0d));
    public double SelectionStartSec { get => (double)GetValue(SelectionStartSecProperty); set => SetValue(SelectionStartSecProperty, value); }

    public static readonly DependencyProperty SelectionEndSecProperty = DependencyProperty.Register(
        nameof(SelectionEndSec), typeof(double), typeof(WaveformPreview), new PropertyMetadata(0d));
    public double SelectionEndSec { get => (double)GetValue(SelectionEndSecProperty); set => SetValue(SelectionEndSecProperty, value); }

    private double _zoom = 1.0;
    private Point? _dragStart;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Overlay.MouseLeftButtonDown += (_, args) => { _dragStart = args.GetPosition(Overlay); SelectionRect.Visibility = Visibility.Visible; Overlay.CaptureMouse(); };
        Overlay.MouseMove += (_, args) =>
        {
            if (_dragStart is null) return;
            var p = args.GetPosition(Overlay);
            var x = Math.Min(_dragStart.Value.X, p.X);
            var w = Math.Abs(p.X - _dragStart.Value.X);
            Canvas.SetLeft(SelectionRect, x);
            SelectionRect.Width = w;
            Canvas.SetTop(SelectionRect, 0);
            SelectionRect.Height = Overlay.ActualHeight;
            SelectionRect.Fill = SelectionBrush;
        };
        Overlay.MouseLeftButtonUp += (_, __) => { _dragStart = null; Overlay.ReleaseMouseCapture(); };
        Overlay.SizeChanged += (_, __) => { SelectionRect.Height = Overlay.ActualHeight; };
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (WaveformPreview)d;
        ctrl.LoadImage(e.NewValue as string);
    }

    private void LoadImage(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                Image.Source = bmp;
                Overlay.Width = bmp.PixelWidth; Overlay.Height = bmp.PixelHeight;
            }
            else
            {
                Image.Source = null; SelectionRect.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            Image.Source = null; SelectionRect.Visibility = Visibility.Collapsed;
        }
    }

    private void OnZoomIn(object sender, RoutedEventArgs e) { _zoom = Math.Min(8.0, _zoom * 1.25); ApplyZoom(); }
    private void OnZoomOut(object sender, RoutedEventArgs e) { _zoom = Math.Max(1.0, _zoom / 1.25); ApplyZoom(); }
    private void ApplyZoom()
    {
        Image.LayoutTransform = new ScaleTransform(_zoom, 1.0);
        Overlay.LayoutTransform = new ScaleTransform(_zoom, 1.0);
    }
}

