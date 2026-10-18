using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace TinaPASS.Main
{
    public class CleanGradientEffect : BackgroundEffect
    {
        private double _w = 1200;
        private double _h = 700;
        private double _t;

        private static readonly string[] Palette =
        {
            "#FF3B30", "#FF9560", "#4AFF80", "#D4E157", "#34C759", "#00C7BE",
            "#64D2FF", "#0A84FF", "#5E5CE6", "#BF5AF2", "#FF2D55", "#FF375F"
        };

        private Rectangle? _base;
        private Blob[] _blobs = Array.Empty<Blob>();
        private readonly Rectangle[] _mesh = new Rectangle[2];
        private readonly Rectangle[] _edges = new Rectangle[4];

        private class Blob
        {
            public string Color;
            public double Cx, Cy, Rx, Ry;
            public double Ax, Ay, Bx, By;
            public double FX, FY;
            public double BaseOpacity;

            public Shape Shape;

            public Blob(string color, double cx, double cy, double rx, double ry,
                        double ax, double ay, double bx, double by, double fx, double fy,
                        double baseOpacity, Shape shape)
            {
                Color = color;
                Cx = cx; Cy = cy; Rx = rx; Ry = ry;
                Ax = ax; Ay = ay; Bx = bx; By = by;
                FX = fx; FY = fy;
                BaseOpacity = baseOpacity;
                Shape = shape;
            }
        }

        public CleanGradientEffect(Canvas canvas, string effectName) : base(canvas)
        {
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
                new { c = "#FF3B30", cx = 0.4, cy = 0.5, rx = 1.05, ry = 1.45, ax = 0.3, ay = 0.0, bx = 0.0, by = 0.0, fx = 1.0, fy = 0.0, op = 1.0 }
            };

            var rnd = new Random(2026);

            _blobs = new Blob[blobSpecs.Length];

            for (int i = 0; i < _blobs.Length; i++)
            {
                var s = blobSpecs[i];
                var radial = new ConicGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    Angle = 0.0
                };
                for (int k = 0; k < Palette.Length; k++)
                {
                    double off = (double)k / Palette.Length;
                    radial.GradientStops.Add(new GradientStop { Offset = off, Color = FromHexAlpha(Palette[k], 255) });
                }
                radial.GradientStops.Add(new GradientStop { Offset = 1.0, Color = FromHexAlpha(Palette[0], 255) });

                int pts;
                Point[] points;
                if (i == 5)
                {
                    pts = 10;
                    points = new Point[pts];
                    for (int p = 0; p < pts; p++)
                    {
                        double ang = -Math.PI / 2.0 + p * Math.PI / 5.0;
                        double rr = (p % 2 == 0) ? 0.5 : 0.20;
                        points[p] = new Point(0.5 + Math.Cos(ang) * rr, 0.5 + Math.Sin(ang) * rr);
                    }
                }
                else
                {
                    const int perEdge = 24;
                    pts = 3 * perEdge;
                    points = new Point[pts];
                    var tri = new Point[3];
                    double rot = rnd.NextDouble() * Math.PI * 2.0;
                    for (int p = 0; p < 3; p++)
                    {
                        double ang = rot + Math.PI * 2.0 * p / 3.0;
                        double rad = 0.5 * (0.70 + rnd.NextDouble() * 0.30);
                        tri[p] = new Point(0.5 + Math.Cos(ang) * rad, 0.5 + Math.Sin(ang) * rad);
                    }
                    int idx = 0;
                    for (int e = 0; e < 3; e++)
                    {
                        Point a = tri[e];
                        Point b = tri[(e + 1) % 3];
                        double dx = b.X - a.X;
                        double dy = b.Y - a.Y;
                        double len = Math.Sqrt(dx * dx + dy * dy);
                        double nx = -dy / len;
                        double ny = dx / len;
                        for (int m = 0; m < perEdge; m++)
                        {
                            double t = (double)m / perEdge;
                            double off = Math.Sin(t * Math.PI * 3.0) * 0.06;
                            points[idx++] = new Point(a.X + dx * t + nx * off, a.Y + dy * t + ny * off);
                        }
                    }
                }

                double scale = 0.85 + rnd.NextDouble() * 0.35;

                var shape = new Polygon
                {
                    Points = points,
                    Stretch = Stretch.Fill,
                    Width = _w * s.rx * scale,
                    Height = _h * s.ry * scale,
                    Fill = radial,
                    Effect = new BlurEffect { Radius = 340 + rnd.Next(30) },
                    IsHitTestVisible = false
                };
                Canvas.Children.Add(shape);

                _blobs[i] = new Blob(
                    s.c, s.cx, s.cy, s.rx * scale, s.ry * scale,
                    s.ax, s.ay, s.bx, s.by, s.fx, s.fy, s.op, shape);
            }

            var meshSpecs = new[]
            {
                new { c = "#C2C7CE", a = 0, start = new RelativePoint(0.0, 1.0, RelativeUnit.Relative), end = new RelativePoint(1.0, 0.0, RelativeUnit.Relative) },
                new { c = "#7E848C", a = 0, start = new RelativePoint(1.0, 0.0, RelativeUnit.Relative), end = new RelativePoint(0.0, 1.0, RelativeUnit.Relative) }
            };

            for (int i = 0; i < 2; i++)
            {
                var spec = meshSpecs[i];
                var brush = new LinearGradientBrush { StartPoint = spec.start, EndPoint = spec.end };
                brush.GradientStops.Add(new GradientStop { Offset = 0.0, Color = FromHexAlpha(spec.c, 0) });
                brush.GradientStops.Add(new GradientStop { Offset = 0.55, Color = FromHexAlpha(spec.c, (byte)spec.a) });
                brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = FromHexAlpha(spec.c, 0) });

                _mesh[i] = new Rectangle
                {
                    Width = _w,
                    Height = _h,
                    Fill = brush,
                    IsHitTestVisible = false
                };
                Canvas.Children.Add(_mesh[i]);
            }

            double edge = 0.30;
            var edgeColor = Color.FromArgb(40, 0, 0, 0);
            _edges[0] = VerticalEdge(edgeColor, 0.0, edge);
            _edges[1] = VerticalEdge(edgeColor, 1.0, 1.0 - edge);
            _edges[2] = HorizontalEdge(edgeColor, 0.0, edge);
            _edges[3] = HorizontalEdge(edgeColor, 1.0, 1.0 - edge);
            foreach (var e2 in _edges)
                Canvas.Children.Add(e2);

            UpdateGraphic();
        }

        private Rectangle VerticalEdge(Color color, double darkY, double clearY)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, darkY, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, clearY, RelativeUnit.Relative)
            };
            brush.GradientStops.Add(new GradientStop { Offset = 0.0, Color = color });
            brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Color.FromArgb(0, color.R, color.G, color.B) });

            return new Rectangle
            {
                Width = _w,
                Height = _h,
                Fill = brush,
                IsHitTestVisible = false
            };
        }

        private Rectangle HorizontalEdge(Color color, double darkX, double clearX)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(darkX, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(clearX, 0, RelativeUnit.Relative)
            };
            brush.GradientStops.Add(new GradientStop { Offset = 0.0, Color = color });
            brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Color.FromArgb(0, color.R, color.G, color.B) });

            return new Rectangle
            {
                Width = _w,
                Height = _h,
                Fill = brush,
                IsHitTestVisible = false
            };
        }

        private static Brush BuildBaseGradient()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1.0, 1.0, RelativeUnit.Relative)
            };
            brush.GradientStops.Add(new GradientStop { Offset = 0.0, Color = Color.FromRgb(0x12, 0x12, 0x12) });
            brush.GradientStops.Add(new GradientStop { Offset = 0.5, Color = Color.FromRgb(0x12, 0x12, 0x12) });
            brush.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Color.FromRgb(0x12, 0x12, 0x12) });
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
                double cx = b.Cx * _w;
                double cy = b.Cy * _h;
                Canvas.SetLeft(b.Shape, cx - b.Shape.Width / 2);
                Canvas.SetTop(b.Shape, cy - b.Shape.Height / 2);
                b.Shape.Opacity = b.BaseOpacity;

                if (b.Shape.Fill is ConicGradientBrush cb)
                {
                    double phase = _t * 0.05;
                    foreach (var stop in cb.GradientStops)
                        stop.Color = SamplePalette(stop.Offset + phase);
                }
            }
        }

        private static Color SamplePalette(double p)
        {
            p -= Math.Floor(p);
            double x = p * Palette.Length;
            int i0 = (int)x % Palette.Length;
            int i1 = (i0 + 1) % Palette.Length;
            double f = x - Math.Floor(x);
            Color c0 = FromHexAlpha(Palette[i0], 255);
            Color c1 = FromHexAlpha(Palette[i1], 255);
            return Color.FromRgb(
                (byte)(c0.R + (c1.R - c0.R) * f),
                (byte)(c0.G + (c1.G - c0.G) * f),
                (byte)(c0.B + (c1.B - c0.B) * f));
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (_w <= 1 || _h <= 1) return;
            _t += 0.02;
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

            if (_mesh[0] != null)
            {
                _mesh[0].Width = _w;
                _mesh[0].Height = _h;
                _mesh[1].Width = _w;
                _mesh[1].Height = _h;
            }

            _edges[0].Width = _w;
            _edges[0].Height = _h;
            _edges[1].Width = _w;
            _edges[1].Height = _h;
            _edges[2].Width = _w;
            _edges[2].Height = _h;
            _edges[3].Width = _w;
            _edges[3].Height = _h;

            UpdateGraphic();
        }
    }
}