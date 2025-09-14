using System;
using System.Windows;
using System.Windows.Controls;

namespace Lazarus.Desktop.Controls;

public partial class AudioMeters : UserControl
{
    public AudioMeters() { InitializeComponent(); SizeChanged += (_, __) => UpdateHeights(); }

    public static readonly DependencyProperty PeakLProperty = DependencyProperty.Register(
        nameof(PeakL), typeof(float), typeof(AudioMeters), new PropertyMetadata(0f, OnAnyChanged));
    public static readonly DependencyProperty PeakRProperty = DependencyProperty.Register(
        nameof(PeakR), typeof(float), typeof(AudioMeters), new PropertyMetadata(0f, OnAnyChanged));
    public static readonly DependencyProperty RmsLProperty = DependencyProperty.Register(
        nameof(RmsL), typeof(float), typeof(AudioMeters), new PropertyMetadata(0f, OnAnyChanged));
    public static readonly DependencyProperty RmsRProperty = DependencyProperty.Register(
        nameof(RmsR), typeof(float), typeof(AudioMeters), new PropertyMetadata(0f, OnAnyChanged));

    public float PeakL { get => (float)GetValue(PeakLProperty); set => SetValue(PeakLProperty, value); }
    public float PeakR { get => (float)GetValue(PeakRProperty); set => SetValue(PeakRProperty, value); }
    public float RmsL { get => (float)GetValue(RmsLProperty); set => SetValue(RmsLProperty, value); }
    public float RmsR { get => (float)GetValue(RmsRProperty); set => SetValue(RmsRProperty, value); }

    private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (AudioMeters)d; ctrl.UpdateHeights();
    }

    private void UpdateHeights()
    {
        var h = Math.Max(0, ActualHeight - 2);
        LPeak.Height = h * Clamp01(PeakL);
        LRms.Height = h * Clamp01(RmsL);
        RPeak.Height = h * Clamp01(PeakR);
        RRms.Height = h * Clamp01(RmsR);
    }
    private static double Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
}

