using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Lazarus.Desktop.Views
{
    public partial class StartupWindow : Window
    {
        private readonly Stopwatch _sw = new();
        private readonly DispatcherTimer _tracerTimer;
        private double _progress01; // 0..1
        

        public StartupWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += (_, __) => RecenterOrbit();

            // tracer sweep
            _tracerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(12) };
            _tracerTimer.Tick += (_, __) => UpdateTracer();
            _tracerTimer.Start();
        }

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _sw.Start();

            // configure particles
            Fx.BackgroundColor = Color.FromRgb(15, 10, 21);
            Fx.NeonColor = Color.FromRgb(233, 0, 255); // magenta
            Fx.AshEmission = 140;          // particles/sec rising
            Fx.TracerEmission = 40;        // orbiting sparks/sec
            Fx.MaxParticles = 800;
            RecenterOrbit();
        }

        void RecenterOrbit()
        {
            // center orbit around the LAZARUS title
            var pt = TitleText.TranslatePoint(new Point(TitleText.ActualWidth / 2, TitleText.ActualHeight / 2), Fx);
            Fx.OrbitCenter = pt;
            Fx.OrbitRadius = Math.Max(TitleText.ActualWidth, 320) * 0.55;
        }

        void UpdateTracer()
        {
            // animate the white tracer sweeping across the fill
            var width = ((FrameworkElement)ProgressClip).ActualWidth;
            var sweep = (DateTime.UtcNow.Ticks / 1e7) % 2.0; // 0..2
            var x = (_progress01 * width) - 120 + (Math.Sin(sweep * Math.PI) * 40);
            if (x < 0) x = 0;
            Canvas.SetLeft(Tracer, x);
        }

        // called from your bootstrapper
        public void SetStatus(string step, int percent)
        {
            StatusText.Text = step;
            PercentText.Text = $"{percent}%";

            _progress01 = Math.Max(0, Math.Min(1, percent / 100.0));
            var width = ((FrameworkElement)ProgressClip).ActualWidth;
            ProgressFill.Width = width * _progress01;

            // drive particle intensity with progress
            Fx.TracerIntensity = 0.3 + 0.9 * _progress01;  // more sparks later
            Fx.AshIntensity = 0.6 + 0.6 * _progress01;     // more ashes rising

            // PHOENIX: map to 0..1 progress
            Phoenix.Progress = _progress01;

            // burst is awaited explicitly by App.xaml.cs after initialization completes
        }

        public async Task PlayCompletionBurstAsync()
        {
            // flare the phoenix + extra particles, then give it ~550ms before close
            Fx.Burst(ashes: 220, tracers: 140);
            await Phoenix.PlayBurstAsync();
        }
    }
}