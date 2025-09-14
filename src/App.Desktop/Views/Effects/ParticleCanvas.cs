using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Lazarus.Desktop.Views.Effects
{
    public sealed class ParticleCanvas : FrameworkElement
    {
        readonly Stopwatch _clock = new();
        readonly Random _rng = new();
        readonly List<Particle> _particles = new();

        public int MaxParticles { get; set; } = 800;
        public double AshEmission { get; set; } = 120;      // particles/sec
        public double TracerEmission { get; set; } = 30;    // particles/sec
        public double AshIntensity { get; set; } = 1.0;     // 0..2 scale
        public double TracerIntensity { get; set; } = 1.0;  // 0..2 scale

        public Point OrbitCenter { get; set; }
        public double OrbitRadius { get; set; } = 220;

        public Color BackgroundColor { get; set; } = Color.FromRgb(12, 9, 18);
        public Color NeonColor { get; set; } = Color.FromRgb(233, 0, 255);

        public ParticleCanvas()
        {
            Loaded += (_, __) =>
            {
                _clock.Start();
                CompositionTarget.Rendering += OnFrame;
            };
            Unloaded += (_, __) =>
            {
                CompositionTarget.Rendering -= OnFrame;
                _clock.Stop();
            };
        }

        protected override void OnRender(DrawingContext dc)
        {
            // subtle vignette background
            var rect = new Rect(new Point(0, 0), RenderSize);
            var gradient = new RadialGradientBrush(
                Color.FromArgb(255, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B),
                Color.FromArgb(255, (byte)(BackgroundColor.R * 0.5), (byte)(BackgroundColor.G * 0.5), (byte)(BackgroundColor.B * 0.6)))
            { Center = new Point(0.5, 0.45), RadiusX = 0.9, RadiusY = 0.9 };
            dc.DrawRectangle(gradient, null, rect);

            // glow haze behind orbit
            if (OrbitCenter.X > 0)
            {
                var glow = new RadialGradientBrush(
                    Color.FromArgb(60, NeonColor.R, NeonColor.G, NeonColor.B),
                    Color.FromArgb(0, NeonColor.R, NeonColor.G, NeonColor.B))
                { Center = new Point(OrbitCenter.X / ActualWidth, OrbitCenter.Y / ActualHeight), RadiusX = OrbitRadius / ActualWidth, RadiusY = OrbitRadius / ActualHeight };
                dc.PushOpacity(0.8);
                dc.DrawEllipse(glow, null, OrbitCenter, OrbitRadius * 1.1, OrbitRadius * 1.1);
                dc.Pop();
            }

            foreach (var p in _particles)
            {
                var a = (byte)(Math.Clamp(p.Alpha, 0, 1) * 255);
                var c = Color.FromArgb(a, p.Color.R, p.Color.G, p.Color.B);

                if (p.Kind == ParticleKind.Ash)
                {
                    // soft ember
                    var brush = new SolidColorBrush(c);
                    dc.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Size, p.Size);
                }
                else
                {
                    // tracer streak
                    var pen = new Pen(new SolidColorBrush(c), Math.Max(1.0, p.Size * 0.75));
                    pen.StartLineCap = PenLineCap.Round; pen.EndLineCap = PenLineCap.Round;
                    var from = new Point(p.X - p.VX * 0.02, p.Y - p.VY * 0.02);
                    var to = new Point(p.X, p.Y);
                    dc.DrawLine(pen, from, to);
                }
            }
        }

        void OnFrame(object? sender, EventArgs e)
        {
            double dt = Math.Max(0.001, _clock.Elapsed.TotalSeconds);
            _clock.Restart();

            SpawnAsh((int)(AshEmission * AshIntensity * dt));
            SpawnTracers((int)(TracerEmission * TracerIntensity * dt));

            // simulate
            double w = ActualWidth, h = ActualHeight;
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Age += dt;
                if (p.Age >= p.Life) { _particles.RemoveAt(i); continue; }

                double t = p.Age / p.Life;

                if (p.Kind == ParticleKind.Ash)
                {
                    // upward drift with jitter
                    p.VX += (Noise(p.Seed, p.Age) - 0.5) * 6.0 * dt;
                    p.X += p.VX * dt;
                    p.Y += p.VY * dt;
                    p.Alpha = (1.0 - t) * 0.8;
                    p.Size *= 0.999;
                }
                else
                {
                    // orbital tracers
                    p.OrbitAngle += p.OrbitSpeed * dt;
                    p.X = OrbitCenter.X + Math.Cos(p.OrbitAngle) * OrbitRadius;
                    p.Y = OrbitCenter.Y + Math.Sin(p.OrbitAngle) * OrbitRadius * 0.85;
                    p.Alpha = 0.35 + 0.65 * Math.Sin(p.OrbitAngle * 2 + p.Seed);
                }

                // Cull if offscreen
                if (p.X < -40 || p.X > w + 40 || p.Y < -40 || p.Y > h + 40)
                { _particles.RemoveAt(i); continue; }

                _particles[i] = p;
            }

            // cap
            if (_particles.Count > MaxParticles)
                _particles.RemoveRange(0, _particles.Count - MaxParticles);

            InvalidateVisual();
        }

        void SpawnAsh(int count)
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            double w = ActualWidth, h = ActualHeight;

            for (int i = 0; i < count; i++)
            {
                var p = new Particle
                {
                    Kind = ParticleKind.Ash,
                    X = _rng.NextDouble() * w,
                    Y = h + _rng.NextDouble() * 20,
                    VX = (_rng.NextDouble() - 0.5) * 30,
                    VY = -40 - _rng.NextDouble() * 60,
                    Size = 1.0 + _rng.NextDouble() * 2.8,
                    Life = 1.6 + _rng.NextDouble() * 2.0,
                    Color = Lerp(Color.FromRgb(120, 60, 150), NeonColor, 0.25 + _rng.NextDouble() * 0.5),
                    Alpha = 0.9,
                    Seed = _rng.NextDouble() * 10
                };
                _particles.Add(p);
            }
        }

        void SpawnTracers(int count)
        {
            if (OrbitCenter == default || OrbitRadius <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var a = _rng.NextDouble() * Math.PI * 2.0;
                var p = new Particle
                {
                    Kind = ParticleKind.Tracer,
                    OrbitAngle = a,
                    OrbitSpeed = 1.6 + _rng.NextDouble() * 2.4,
                    Size = 1.6 + _rng.NextDouble() * 1.4,
                    Life = 8.0 + _rng.NextDouble() * 6.0,
                    Color = Color.FromRgb(
                        (byte)(NeonColor.R),
                        (byte)Math.Min(255, NeonColor.G + 40),
                        (byte)255),
                    Alpha = 0.8,
                    Seed = _rng.NextDouble() * 10
                };
                // initial XY will be computed each frame
                _particles.Add(p);
            }
        }

        public void Burst(int ashes = 140, int tracers = 90)
        {
            SpawnAsh(ashes);
            SpawnTracers(tracers);
        }

        static Color Lerp(Color a, Color b, double t) =>
            Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));

        static double Noise(double seed, double x)
        {
            // tiny 1D hash/noise
            double s = Math.Sin(x * 12.9898 + seed * 78.233) * 43758.5453;
            return s - Math.Floor(s);
        }

        struct Particle
        {
            public ParticleKind Kind;
            public double X, Y, VX, VY;
            public double Size;
            public double Life, Age, Alpha, Seed;

            // orbit-only
            public double OrbitAngle, OrbitSpeed;

            public Color Color;
        }

        enum ParticleKind { Ash, Tracer }
    }
}
