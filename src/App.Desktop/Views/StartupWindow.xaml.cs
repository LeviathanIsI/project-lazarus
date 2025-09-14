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
        private readonly DispatcherTimer _corruptionTimer;
        private readonly Random _rng = new();
        private double _progress01; // 0..1
        private double _corruptionState; // 0..1 for corrupted resurrection

        public double CorruptionState
        {
            get => _corruptionState;
            set
            {
                _corruptionState = Math.Max(0, Math.Min(1, value));
                OnPropertyChanged(nameof(CorruptionState));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        // Named elements resolved at runtime (avoid reliance on generated fields)
        private ParticleCanvas? _fx;
        private PhoenixPath? _phoenix;
        private TextBlock? _titleText;
        private TextBlock? _statusText;
        private TextBlock? _percentText;
        private Grid? _progressClip;
        private Rectangle? _progressFill;
        private Rectangle? _tracer;
        private Canvas? _progressParticles;
        private TextBlock? _hexOverlay;
        private Rectangle? _fragmentOverlay;
        private Rectangle? _corruptionTrail;

        public StartupWindow()
        {
            InitializeComponent();
            // Resolve elements explicitly
            _fx = (ParticleCanvas?)FindName("Fx");
            _phoenix = (PhoenixPath?)FindName("Phoenix");
            _titleText = (TextBlock?)FindName("TitleText");
            _statusText = (TextBlock?)FindName("StatusText");
            _percentText = (TextBlock?)FindName("PercentText");
            _progressClip = (Grid?)FindName("ProgressClip");
            _progressFill = (Rectangle?)FindName("ProgressFill");
            _tracer = (Rectangle?)FindName("Tracer");
            _progressParticles = (Canvas?)FindName("ProgressParticles");
            _hexOverlay = (TextBlock?)FindName("HexOverlay");
            _fragmentOverlay = (Rectangle?)FindName("FragmentOverlay");
            _corruptionTrail = (Rectangle?)FindName("CorruptionTrail");

            Loaded += OnLoaded;
            SizeChanged += (_, __) => RecenterOrbit();
            ContentRendered += (_, __) => ApplyTheme();

            // tracer sweep
            _tracerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(12) };
            _tracerTimer.Tick += (_, __) => UpdateTracer();
            _tracerTimer.Start();

            // corruption resurrection timer
            _corruptionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _corruptionTimer.Tick += (_, __) => UpdateCorruption();
            _corruptionTimer.Start();
        }

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _sw.Start();

            // configure particles
            // Theme will override these in ApplyTheme()
            if (_fx is not null)
            {
                _fx.BackgroundColor = Color.FromRgb(15, 10, 21);
                _fx.NeonColor = Color.FromRgb(233, 0, 255);
                _fx.AshEmission = 140;          // particles/sec rising
                _fx.TracerEmission = 40;        // orbiting sparks/sec
                _fx.MaxParticles = 800;
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
            // animate the white tracer sweeping across the fill
            if (_progressClip is null || _tracer is null) return;
            // scanner storyboard now drives movement; leave occasional short glitch trails by tiny jitter
            var width = ((FrameworkElement)_progressClip).ActualWidth;
            var sweep = (DateTime.UtcNow.Ticks / 1e7) % 2.0; // 0..2
            double jitter = (Math.Sin(sweep * 17) * 2);
            Canvas.SetLeft(_tracer, Math.Max(0, (_progress01 * width) + jitter - 60));
        }

        void UpdateCorruption()
        {
            // Dynamic corruption based on progress and time
            double time = _sw.Elapsed.TotalSeconds;
            double baseCorruption = Math.Max(0, Math.Min(1, (_progress01 - 0.3) / 0.4)); // Start corruption at 30% progress
            double timeCorruption = Math.Sin(time * 2) * 0.3 + 0.3; // 0.0 to 0.6 oscillation
            double glitchSpike = (_rng.NextDouble() < 0.05) ? _rng.NextDouble() * 0.8 : 0; // Random glitch spikes

            CorruptionState = Math.Min(1.0, baseCorruption + timeCorruption + glitchSpike);

            // Apply corruption effects to progress bar
            if (_progressClip != null)
            {
                double corruptionOpacity = CorruptionState * 0.6;

                // Fragmentation effect
                if (_fragmentOverlay != null)
                {
                    _fragmentOverlay.Opacity = corruptionOpacity;
                    _fragmentOverlay.Width = _progressFill?.Width ?? 0;
                }

                // Hex memory addresses
                if (_hexOverlay != null)
                {
                    _hexOverlay.Opacity = corruptionOpacity * 0.5;
                    if (_rng.NextDouble() < 0.1) // Occasionally change addresses
                    {
                        string[] hexAddresses = { "0xDEAD", "0xBEEF", "0xCAFE", "0xFACE", "0xFEED", "0xC0DE" };
                        _hexOverlay.Text = $"{hexAddresses[_rng.Next(hexAddresses.Length)]} {hexAddresses[_rng.Next(hexAddresses.Length)]} {hexAddresses[_rng.Next(hexAddresses.Length)]}";
                    }
                }

                // Corruption trail following tracer
                if (_corruptionTrail != null && _tracer != null)
                {
                    _corruptionTrail.Opacity = corruptionOpacity * 0.4;
                    double trailX = Canvas.GetLeft(_tracer) - 40 + _rng.NextDouble() * 20;
                    Canvas.SetLeft(_corruptionTrail, Math.Max(0, trailX));
                }

                // Trigger enhanced corruption scanner at high corruption levels
                if (CorruptionState > 0.6)
                {
                    TryRunStoryboard("CorruptionScannerStoryboard");
                }
            }
        }

        // called from your bootstrapper
        public void SetStatus(string step, int percent)
        {
            // Apply corruption effects to status text
            string corruptedStep = ApplyTextCorruption(step, CorruptionState);

            if (_statusText is not null)
            {
                _statusText.Text = corruptedStep;

                // Apply text inversion effects based on corruption
                if (CorruptionState > 0.5)
                {
                    ApplyTextInversion(_statusText, CorruptionState);
                }
            }

            // Apply percentage glitching
            string percentText = ApplyPercentageGlitch(percent, CorruptionState);
            if (_percentText is not null) _percentText.Text = percentText;

            _progress01 = Math.Max(0, Math.Min(1, percent / 100.0));
            if (_progressClip is not null && _progressFill is not null)
            {
                var width = ((FrameworkElement)_progressClip).ActualWidth;
                _progressFill.Width = width * _progress01;
            }

            // drive particle intensity with progress
            if (_fx is not null)
            {
                _fx.TracerIntensity = 0.3 + 0.9 * _progress01;  // more sparks later
                _fx.AshIntensity = 0.6 + 0.6 * _progress01;     // more ashes rising
            }

            // PHOENIX: map to 0..1 progress
            if (_phoenix is not null) _phoenix.Progress = _progress01;

            // burst is awaited explicitly by App.xaml.cs after initialization completes
            // Occasionally trigger RGB glitch on major progress thresholds
            if (percent == 25 || percent == 50 || percent == 75)
            {
                TryRunStoryboard("RgbGlitchStoryboard");
            }
        }

        string ApplyTextCorruption(string original, double corruptionLevel)
        {
            if (corruptionLevel < 0.2) return original;

            char[] chars = original.ToCharArray();
            double corruptionChance = corruptionLevel * 0.3; // 0-30% chance per character

            for (int i = 0; i < chars.Length; i++)
            {
                if (_rng.NextDouble() < corruptionChance)
                {
                    // Replace with ASCII code or random character
                    if (_rng.NextDouble() < 0.5)
                    {
                        chars[i] = (char)_rng.Next(33, 126); // Random ASCII printable
                    }
                    else
                    {
                        // Show ASCII code representation
                        int ascii = (int)chars[i];
                        string asciiStr = $"[{ascii:X2}]";
                        // Replace single character with ASCII code (truncate if needed)
                        if (asciiStr.Length <= original.Length - i)
                        {
                            for (int j = 0; j < asciiStr.Length && i + j < chars.Length; j++)
                            {
                                chars[i + j] = asciiStr[j];
                            }
                        }
                    }
                }
            }

            return new string(chars);
        }

        void ApplyTextInversion(TextBlock textBlock, double corruptionLevel)
        {
            // Create inversion effect by manipulating opacity and color
            double inversionStrength = (corruptionLevel - 0.5) * 2; // 0 to 1

            if (inversionStrength > 0.3)
            {
                // Add drop shadow for "behind interface" effect
                if (textBlock.Effect is DropShadowEffect shadow)
                {
                    shadow.Opacity = Math.Min(1, inversionStrength * 0.8);
                    shadow.Color = Colors.White; // Inverted shadow
                }
                else
                {
                    textBlock.Effect = new DropShadowEffect
                    {
                        Opacity = inversionStrength * 0.8,
                        Color = Colors.White,
                        BlurRadius = 4,
                        ShadowDepth = 0
                    };
                }
            }
        }

        string ApplyPercentageGlitch(int percent, double corruptionLevel)
        {
            if (corruptionLevel < 0.4) return $"{percent}%";

            double glitchChance = (corruptionLevel - 0.4) / 0.6; // 0-1 scale

            if (_rng.NextDouble() < glitchChance * 0.2) // 20% chance at max corruption
            {
                string[] glitchValues = { "101%", "-1%", "NaN", "0xDEAD", "∞%", "ERROR%" };
                return glitchValues[_rng.Next(glitchValues.Length)];
            }

            return $"{percent}%";
        }

        public async Task PlayCompletionBurstAsync()
        {
            // flare the phoenix + extra particles, then give it ~550ms before close
            _fx?.Burst(ashes: 220, tracers: 140);
            if (_phoenix is not null)
            {
                await _phoenix.PlayBurstAsync();
            }
        }

        private void ApplyTheme()
        {
            Color accent = TryGetColor("AccentColor")
                           ?? TryGetBrushColor("AccentBrush")
                           ?? Color.FromRgb(233, 0, 255);

            Color surface = TryGetColor("SurfaceColor")
                            ?? TryGetBrushColor("SurfaceBrush")
                            ?? Color.FromRgb(15, 15, 15);

            // Particle canvas
            if (_fx is not null)
            {
                _fx.NeonColor = accent;
                _fx.BackgroundColor = surface;
                _fx.CorruptionLevel = CorruptionState;
                _fx.TextFragmentIntensity = 0.5 + CorruptionState * 0.8; // Increase text fragments with corruption
            }

            // Phoenix stroke + glow
            if (_phoenix is not null)
            {
                _phoenix.NeonColor = accent;
                _phoenix.Stroke = new LinearGradientBrush(
                    Lighten(accent, 0.10), Lighten(accent, 0.35), 0);
            }

            // Progress bar gradient + glow
            if (_progressFill is not null)
            {
                // Create a new LinearGradientBrush instead of modifying the frozen one
                var newBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                newBrush.GradientStops.Add(new GradientStop(Lighten(accent, -0.05), 0));
                newBrush.GradientStops.Add(new GradientStop(Lighten(accent, 0.25), 1));
                _progressFill.Fill = newBrush;
            }
            if (_progressFill?.Effect is DropShadowEffect pe)
            {
                pe.Color = accent;
            }

            // Title + shadow
            if (_titleText is not null)
            {
                _titleText.Foreground = new SolidColorBrush(Lighten(accent, 0.15));
                if (_titleText.Effect is DropShadowEffect ds)
                {
                    ds.Color = accent;
                }
            }
        }

        private void TryRunStoryboard(string key)
        {
            if (TryFindResource(key) is System.Windows.Media.Animation.Storyboard sb)
            {
                sb.Begin(this, true);
            }
        }

        private Color? TryGetColor(string key)
        {
            if (TryFindResource(key) is Color c) return c;
            return null;
        }

        private Color? TryGetBrushColor(string key)
        {
            if (TryFindResource(key) is SolidColorBrush b) return b.Color;
            return null;
        }

        private static Color Lighten(Color c, double delta)
        {
            static byte L(byte v, double d) => (byte)Math.Max(0, Math.Min(255, v + d * 255.0));
            return Color.FromRgb(L(c.R, delta), L(c.G, delta), L(c.B, delta));
        }
    }
}