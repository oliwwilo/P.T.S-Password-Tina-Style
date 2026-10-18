using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace TinaPASS.Main
{
    /// <summary>
    /// Фон в стиле Figma-макета: неоновая розовая основа
    /// с мягкими радиальными «пятнами» (циан, белый, розовый),
    /// которые медленно дрейфуют по экрану.
    /// </summary>
    public class FigmaNeonEffect : BackgroundEffect
    {
        private double _w = 1200;
        private double _h = 700;
        private double _t;

        private Rectangle? _base;
        private readonly Blob[] _blobs = new Blob[4];

        private class Blob
        {
            public string Color;
            public double Cx, Cy, Rx, Ry;
            public double Ax, Ay, Bx, By;
            public double FX, FY;
            public double BaseOpacity;
            public Ellipse Shape;

            public Blob(string color, double cx, double cy, double rx, double ry,
                        double ax, double ay, double bx, double by, double fx, double fy,
                        double baseOpacity, Ellipse shape)
            {
                Color = color;
                Cx = cx; Cy = cy; Rx = rx; Ry = ry;
                Ax = ax; Ay = ay; Bx = bx; By = by;
                FX = fx; FY = fy;
                BaseOpacity = baseOpacity;
                Shape = shape;
            }
        }

        public FigmaNeonEffect(Canvas canvas, string effectName) : base(canvas)
        {
        }

        public override void Start()
        {
            IsRunning = true;
            Canvas.Children.Clear();
            InitializeEffect();
            Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(66)
            };
            Timer.Tick += OnTick;
            Timer.Start();
        }

        public override void Stop()
        {
            IsRunning = false;
            Timer?.Stop();
            Timer = null;
            Canvas.Children.Clear();
        }

        protected override void InitializeEffect()
        {
            _w = Canvas.Bounds.Width > 1 ? Canvas.Bounds.Width : _w;
            _h = Canvas.Bounds.Height > 1 ? Canvas.Bounds.Height : _h;

            _base = new Rectangle
            {
                Width = _w,
                Height = _h,
                Fill = BuildBaseGradient(),
                IsHitTestVisible = false
            };
            Canvas.Children.Add(_base);

            var blobSpecs = new[]
            {
                new { c = "#69DBFE", cx = 0.25, cy = 0.30, rx = 0.55, ry = 0.6, ax = 0.05, ay = 0.04, bx = 0.07, by = 0.06, fx = 0.009, fy = 0.007, op = 0.45 },
                new { c = "#FFFFFF", cx = 0.80, cy = 0.25, rx = 0.5, ry = 0.55, ax = 0.06, ay = 0.05, bx = 0.05, by = 0.08, fx = 0.007, fy = 0.011, op = 0.28 },
                new { c = "#FF66CC", cx = 0.72, cy = 0.75, rx = 0.55, ry = 0.55, ax = 0.05, ay = 0.06, bx = 0.08, by = 0.05, fx = 0.011, fy = 0.008, op = 0.4 },
                new { c = "#69DBFE", cx = 0.25, cy = 0.8, rx = 0.6, ry = 0.5, ax = 0.04, ay = 0.05, bx = 0.07, by = 0.07, fx = 0.008, fy = 0.01, op = 0.35 }
            };

            for (int i = 0; i < _blobs.Length; i++)
            {
                var s = blobSpecs[i];
                var radial = new RadialGradientBrush { Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative) };
                radial.GradientStops.Add(new GradientStop { Offset = 0.0, Color = FromHexAlpha(s.c, 180) });
                radial.GradientStops.Add(new GradientStop { Offset = 0.6, Color = FromHexAlpha(s.c, 60) });
                radial.GradientStops.Add(new GradientStop { Offset = 1.0, Color = FromHexAlpha(s.c, 0) });

                var shape = new Ellipse
                {
                    Width = _w * s.rx,
                    Height = _h * s.ry,
                    Fill = radial,
                    IsHitTestVisible = false
                };
                Canvas.Children.Add(shape);

                _blobs[i] = new Blob(
                    s.c, s.cx, s.cy, s.rx, s.ry,
                    s.ax, s.ay, s.bx, s.by, s.fx, s.fy, s.op, shape);
            }

            UpdateGraphic();
        }

        private static Brush BuildBaseGradient()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1.0, 1.0, RelativeUnit.Relative)
            };
            brush.GradientStops.Add(new GradientStop { Offset = 0.0, Color = Color.FromRgb(0xFF, 0x44, 0xBB) });
            brush.GradientStops.Add(new GradientStop { Offset = 0.5, Color = Color.FromRgb(0xFE, 0x01, 0xA4) });
            brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Color.FromRgb(0xB4, 0x00, 0x80) });
            return brush;
        }

        private static Color FromHexAlpha(string hex, byte alpha)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(alpha,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        private void UpdateGraphic()
        {
            for (int i = 0; i < _blobs.Length; i++)
            {
                Blob b = _blobs[i];
                double px = b.Cx + Math.Sin(_t * b.FX) * b.Ax + Math.Cos(_t * b.FX * 0.7) * b.Bx;
                double py = b.Cy + Math.Sin(_t * b.FY) * b.Ay + Math.Cos(_t * b.FY * 0.8) * b.By;
                px = Math.Clamp(px, 0.08, 0.92);
                py = Math.Clamp(py, 0.08, 0.92);

                double cx = px * _w;
                double cy = py * _h;
                Canvas.SetLeft(b.Shape, cx - b.Shape.Width / 2);
                Canvas.SetTop(b.Shape, cy - b.Shape.Height / 2);
                b.Shape.Opacity = b.BaseOpacity * (0.85 + 0.15 * Math.Sin(_t * 0.004 + i * 2.1));
            }
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (_w <= 1 || _h <= 1) return;
            _t += 1;
            UpdateGraphic();
        }

        public override void Resize(double width, double height)
        {
            if (width <= 1 || height <= 1) return;
            _w = width;
            _h = height;

            if (_base != null)
            {
                _base.Width = _w;
                _base.Height = _h;
            }

            for (int i = 0; i < _blobs.Length; i++)
            {
                Blob b = _blobs[i];
                b.Shape.Width = _w * b.Rx;
                b.Shape.Height = _h * b.Ry;
            }

            UpdateGraphic();
        }
    }
}