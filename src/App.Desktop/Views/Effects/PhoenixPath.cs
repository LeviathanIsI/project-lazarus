using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Lazarus.Desktop.Views.Effects
{
    public sealed class PhoenixPath : FrameworkElement
    {
        public static readonly DependencyProperty GeometryProperty =
            DependencyProperty.Register(nameof(Geometry), typeof(Geometry), typeof(PhoenixPath),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnGeomChanged));

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register(nameof(Progress), typeof(double), typeof(PhoenixPath),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Geometry? Geometry
        {
            get => (Geometry?)GetValue(GeometryProperty);
            set => SetValue(GeometryProperty, value);
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, Math.Max(0, Math.Min(1, value)));
        }

        public Brush Stroke { get; set; } =
            new LinearGradientBrush(Color.FromRgb(169, 0, 255), Color.FromRgb(233, 0, 255), 0);

        public Color NeonColor { get; set; } = Color.FromRgb(233, 0, 255);
        public double StrokeThickness { get; set; } = 2.4;
        public double GlowThickness { get; set; } = 9.0;
        public double Padding { get; set; } = 8.0;

        // internal
        private readonly List<Line> _lines = new();
        private double _totalLen;
        private readonly Stopwatch _flare = new();
        private TaskCompletionSource<bool>? _flareTcs;

        protected override void OnRender(DrawingContext dc)
        {
            var g = Geometry;
            if (g is null || ActualWidth <= 0 || ActualHeight <= 0 || _totalLen <= 0)
                return;

            var target = _totalLen * Progress;
            var partial = BuildPartial(target);

            // Fit to bounds with uniform scale
            var srcBounds = g.Bounds;
            var dst = new Rect(Padding, Padding, Math.Max(0, ActualWidth - 2*Padding), Math.Max(0, ActualHeight - 2*Padding));
            double sx = dst.Width  / Math.Max(1, srcBounds.Width);
            double sy = dst.Height / Math.Max(1, srcBounds.Height);
            double s = Math.Min(sx, sy);
            var tx = dst.X + (dst.Width  - srcBounds.Width  * s) * 0.5 - srcBounds.X * s;
            var ty = dst.Y + (dst.Height - srcBounds.Height * s) * 0.5 - srcBounds.Y * s;

            // Flare (100%): pulse scale + glow
            double flareT = 0;
            if (_flare.IsRunning)
            {
                flareT = Math.Min(1.0, _flare.Elapsed.TotalMilliseconds / 550.0);
                if (flareT >= 1.0)
                {
                    _flare.Reset();
                    _flareTcs?.TrySetResult(true);
                    _flareTcs = null;
                }
            }
            var pulse = _flare.IsRunning ? (1.0 + 0.15 * EaseOutCubic(1 - Math.Abs(2*flareT - 1))) : 1.0;

            var tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(s * pulse, s * pulse));
            tg.Children.Add(new TranslateTransform(tx, ty));
            dc.PushTransform(tg);

            // soft neon glow
            var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(100, NeonColor.R, NeonColor.G, NeonColor.B)), GlowThickness);
            glowPen.StartLineCap = PenLineCap.Round; glowPen.EndLineCap = PenLineCap.Round;
            dc.PushOpacity(0.65);
            dc.DrawGeometry(null, glowPen, partial);
            dc.Pop();

            // main stroke
            var pen = new Pen(Stroke, StrokeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawGeometry(null, pen, partial);

            // flare bloom
            if (_flare.IsRunning)
            {
                var b = partial.Bounds;
                var center = new Point(b.X + b.Width/2, b.Y + b.Height/3);
                double r = Math.Max(b.Width, b.Height) * (0.15 + 0.35 * flareT);
                var bloom = new RadialGradientBrush(
                    Color.FromArgb((byte)(160 * (1.0 - flareT)), NeonColor.R, NeonColor.G, NeonColor.B),
                    Color.FromArgb(0, NeonColor.R, NeonColor.G, NeonColor.B));
                dc.PushOpacity(0.8);
                dc.DrawEllipse(bloom, null, center, r, r);
                dc.Pop();
            }

            dc.Pop(); // transform
        }

        public void TriggerBurst()
        {
            if (!_flare.IsRunning) _flare.Restart();
            InvalidateVisual();
        }

        public Task PlayBurstAsync()
        {
            if (_flareTcs is { Task.IsCompleted: false }) return _flareTcs.Task;
            _flareTcs = new TaskCompletionSource<bool>();
            TriggerBurst();
            return _flareTcs.Task;
        }

        static void OnGeomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhoenixPath)d).Rebuild();
        }

        void Rebuild()
        {
            _lines.Clear();
            _totalLen = 0;
            if (Geometry is null) return;

            var flat = Geometry.GetFlattenedPathGeometry(0.25, ToleranceType.Relative);
            foreach (var fig in flat.Figures)
            {
                var p = fig.StartPoint;
                foreach (var seg in fig.Segments)
                {
                    if (seg is PolyLineSegment pls)
                    {
                        foreach (var q in pls.Points)
                        {
                            var ln = new Line(p, q);
                            if (ln.Length > 0.0001)
                            {
                                _lines.Add(ln);
                                _totalLen += ln.Length;
                            }
                            p = q;
                        }
                    }
                }
            }
            InvalidateVisual();
        }

        Geometry BuildPartial(double targetLen)
        {
            targetLen = Math.Max(0, Math.Min(_totalLen, targetLen));
            var figs = new List<PathFigure>();
            if (_lines.Count == 0) return new PathGeometry(figs, FillRule.Nonzero, null);

            double acc = 0;
            bool figureOpen = false;
            Point currentStart = _lines[0].A;
            var segs = new List<LineSegment>();

            for (int i = 0; i < _lines.Count; i++)
            {
                var ln = _lines[i];
                if (!figureOpen)
                {
                    currentStart = ln.A;
                    segs = new List<LineSegment>();
                    figureOpen = true;
                }

                if (acc + ln.Length <= targetLen)
                {
                    segs.Add(new LineSegment(ln.B, true));
                    acc += ln.Length;
                }
                else
                {
                    double remain = targetLen - acc;
                    if (remain > 0)
                    {
                        var t = remain / ln.Length;
                        var partialPoint = new Point(ln.A.X + (ln.B.X - ln.A.X) * t, ln.A.Y + (ln.B.Y - ln.A.Y) * t);
                        segs.Add(new LineSegment(partialPoint, true));
                    }
                    // close current figure
                    figs.Add(new PathFigure(currentStart, segs, false));
                    figureOpen = false;
                    break;
                }

                // if next line starts a new figure (discontinuous), close
                if (i + 1 < _lines.Count && _lines[i + 1].A != ln.B)
                {
                    figs.Add(new PathFigure(currentStart, segs, false));
                    figureOpen = false;
                }
            }

            if (figureOpen) figs.Add(new PathFigure(currentStart, segs, false));
            return new PathGeometry(figs, FillRule.Nonzero, null);
        }

        readonly struct Line
        {
            public readonly Point A, B;
            public readonly double Length;
            public Line(Point a, Point b) { A = a; B = b; Length = Math.Sqrt((b.X - a.X)*(b.X - a.X) + (b.Y - a.Y)*(b.Y - a.Y)); }
        }

        static double EaseOutCubic(double x) => 1 - Math.Pow(1 - x, 3);
    }
}