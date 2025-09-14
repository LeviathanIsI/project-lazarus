using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Windows.Shapes;
using Lazarus.Desktop.Views.Effects;

namespace Lazarus.Desktop.Views
{
    public partial class StartupWindow : Window
    {
        private readonly Stopwatch _sw = new();
        private readonly DispatcherTimer _tracerTimer;
        private double _progress01; // 0..1 clamped
        private bool _hasFlickeredTitle; // prevent multiple flickers
        private bool _hasCompleted; // prevent multiple completion bursts

        // Named elements resolved at runtime (avoid reliance on generated fields)
        private ParticleCanvas? _fx;
        private TextBlock? _titleText;
        private TextBlock? _statusText;
        private TextBlock? _percentText;
        private Grid? _progressClip;
        private Rectangle? _progressFill;
        private Rectangle? _tracer;

        public StartupWindow()
        {
            InitializeComponent();
            // Resolve elements explicitly
            _fx = (ParticleCanvas?)FindName("Fx");
            _titleText = (TextBlock?)FindName("TitleText");
            _statusText = (TextBlock?)FindName("StatusText");
            _percentText = (TextBlock?)FindName("PercentText");
            _progressClip = (Grid?)FindName("ProgressClip");
            _progressFill = (Rectangle?)FindName("ProgressFill");
            _tracer = (Rectangle?)FindName("Tracer");

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            ContentRendered += (_, __) => ApplyTheme();

            // tracer sweep
            _tracerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _tracerTimer.Tick += (_, __) => UpdateTracer();
            _tracerTimer.Start();
        }

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _sw.Start();

            // configure particles - will be overridden by ApplyTheme()
            if (_fx is not null)
            {
                _fx.BackgroundColor = Colors.Black;
                _fx.NeonColor = Colors.Magenta;
                _fx.AshEmission = 120;
                _fx.TracerEmission = 30;
                _fx.MaxParticles = 600;
            }
            UpdateLayout();
            ApplyTheme();
        }

        void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Keep progress bar sizing correct when window resizes
            if (_progressClip is not null && _progressFill is not null)
            {
                var width = ((FrameworkElement)_progressClip).ActualWidth;
                _progressFill.Width = width * _progress01;
            }
            RecenterOrbit();
        }

        void RecenterOrbit()
        {
            // center orbit around the LAZARUS title
            if (_titleText is null || _fx is null) return;
            var pt = _titleText.TranslatePoint(new Point(_titleText.ActualWidth / 2, _titleText.ActualHeight / 2), _fx);
            _fx.OrbitCenter = pt;
            _fx.OrbitRadius = Math.Max(_titleText.ActualWidth, 320) * 0.55;
        }

        void UpdateTracer()
        {
            // animate the contained tracer sweeping across the progress fill
            if (_progressClip is null || _tracer is null || _progressFill is null) return;

            var clipWidth = ((FrameworkElement)_progressClip).ActualWidth;
            var fillWidth = _progressFill.Width;
            var tracerWidth = _tracer.Width;

            // Contain tracer within progress fill bounds
            var maxX = Math.Max(0, fillWidth - tracerWidth * 0.5);
            var x = Math.Max(0, Math.Min(maxX, fillWidth - tracerWidth));

            if (_tracer.RenderTransform is TranslateTransform tt)
            {
                tt.X = x;
            }
        }


        // called from your bootstrapper
        public void SetStatus(string step, int percent)
        {
            if (_statusText is not null)
            {
                _statusText.Text = step;
            }

            if (_percentText is not null)
            {
                _percentText.Text = $"{percent}%";
            }

            _progress01 = Math.Clamp(percent / 100.0, 0, 1);
            if (_progressClip is not null && _progressFill is not null)
            {
                var width = ((FrameworkElement)_progressClip).ActualWidth;
                _progressFill.Width = width * _progress01;
            }

            // drive particle intensity with progress
            if (_fx is not null)
            {
                _fx.TracerIntensity = 0.3 + 0.9 * _progress01;
                _fx.AshIntensity = 0.6 + 0.6 * _progress01;
            }

            // trigger title flicker once at 70%
            if (percent >= 70 && !_hasFlickeredTitle)
            {
                _hasFlickeredTitle = true;
                TryRunStoryboard("TitleFlicker");
            }

            // trigger completion burst once at 100%
            if (percent >= 100 && !_hasCompleted)
            {
                _hasCompleted = true;
            }
        }

        private void ApplyTheme()
        {
            // Get theme colors from resources
            var accentBrush = TryFindResource("AccentBrush") as SolidColorBrush;
            var accentHoverBrush = TryFindResource("AccentHoverColor") as SolidColorBrush;
            var surfaceBrush = TryFindResource("SurfaceBrush") as SolidColorBrush;

            var accent = accentBrush?.Color ?? Color.FromRgb(233, 0, 255);
            var accentHover = accentHoverBrush?.Color ?? Color.FromRgb(255, 100, 255);
            var surface = surfaceBrush?.Color ?? Color.FromRgb(15, 15, 15);

            // Particle canvas
            if (_fx is not null)
            {
                _fx.NeonColor = accent;
                _fx.BackgroundColor = surface;
            }

            // Progress bar gradient (already set in XAML)
            if (_progressFill?.Effect is DropShadowEffect pe)
            {
                pe.Color = accent;
            }

            // Title gradient (already set in XAML)
            if (_titleText?.Effect is DropShadowEffect ds)
            {
                ds.Color = accent;
            }
        }

        private void TryRunStoryboard(string key)
        {
            if (TryFindResource(key) is System.Windows.Media.Animation.Storyboard sb)
            {
                sb.Begin(this, true);
            }
        }
    }
}