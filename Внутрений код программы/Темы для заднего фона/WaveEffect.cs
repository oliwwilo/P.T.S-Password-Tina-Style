using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;

namespace TinaPASS.Main
{
    public class WaveEffect : BackgroundEffect
    {
        private class Layer
        {
            public Color Color;
            public Color Glow;
            public bool HasGlow;
            public double Width;
            public double GlowWidth;
            public double Amp;
            public double Freq;
            public double Speed;
            public double Phase;

            public Layer(string color, double amp, double freq, double speed, double phase, double width,
                         string? glow = null, double glowWidth = 10)
            {
                Color = Hex(color);
                Amp = amp;
                Freq = freq;
                Speed = speed;
                Phase = phase;
                Width = width;
                if (glow != null)
                {
                    HasGlow = true;
                    Glow = Hex(glow);
                    GlowWidth = glowWidth;
                }
            }
        }

        private class Preset
        {
            public string Name;
            public Color Top;
            public Color Bottom;
            public double BaseY;
            public Layer[] Layers;

            public Preset(string name, string top, string bottom, double baseY, params Layer[] layers)
            {
                Name = name;
                Top = Hex(top);
                Bottom = Hex(bottom);
                BaseY = baseY;
                Layers = layers;
            }
        }

        private static readonly Preset[] Presets =
        {
            new Preset("Океан", "#B8E2F5", "#1976B2", 0.5,
                new Layer("#FFFFFF", 18, 0.016, 0.055, 0, 3, "#FFFFFF", 12)),

            new Preset("Закат", "#FF8A5C", "#7C2D5E", 0.5,
                new Layer("#FFE07A", 20, 0.011, 0.040, 0, 3, "#FFE07A", 10),
                new Layer("#D95A6B", 24, 0.014, -0.028, Math.PI, 2)),

            new Preset("Неоновые волны", "#131A2E", "#062A4F", 0.5,
                new Layer("#00E5FF", 22, 0.013, 0.050, 0, 3, "#00E5FF", 14),
                new Layer("#7B2FBE", 28, 0.009, -0.035, Math.PI * 0.5, 3, "#7B2FBE", 12),
                new Layer("#00E5FF", 18, 0.017, 0.030, Math.PI, 2)),

            new Preset("Лунная ночь", "#0A1122", "#159E8C", 0.55,
                new Layer("#E8F1F8", 15, 0.009, 0.030, 0, 1.5, "#E8F1F8", 8)),

            new Preset("Степь", "#CFE8B8", "#3E9B4F", 0.5,
                new Layer("#2E7D32", 16, 0.013, 0.045, 0, 2.5),
                new Layer("#66BB6A", 22, 0.009, -0.030, Math.PI * 0.6, 2),
                new Layer("#A8D89A", 12, 0.017, 0.040, Math.PI * 1.3, 2))
        };

        private static Color Hex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        private static Color WithAlpha(Color color, byte a) => new Color(a, color.R, color.G, color.B);

        private readonly Preset _preset;
        private Rectangle? _topRect;
        private Rectangle? _bottomRect;
        private readonly List<Path> _mainPaths = new List<Path>();
        private readonly List<Path> _glowPaths = new List<Path>();
        private readonly List<double[]> _ys = new List<double[]>();
        private double[] _xs = Array.Empty<double>();
        private readonly double[] _phases;
        private double _w = 1200;
        private double _h = 700;
        private double _t;

        public WaveEffect(Canvas canvas, string presetName) : base(canvas)
        {
            _preset = Array.Find(Presets, p => p.Name == presetName) ?? Presets[0];
            _phases = new double[_preset.Layers.Length];
        }

        protected override void InitializeEffect()
        {
            _w = Canvas.Bounds.Width > 1 ? Canvas.Bounds.Width : _w;
            _h = Canvas.Bounds.Height > 1 ? Canvas.Bounds.Height : _h;

            _topRect = new Rectangle
            {
                Width = _w,
                Height = _h * _preset.BaseY,
                Fill = new SolidColorBrush(_preset.Top),
                IsHitTestVisible = false
            };

            _bottomRect = new Rectangle
            {
                Width = _w,
                Height = _h * (1 - _preset.BaseY),
                Fill = new SolidColorBrush(_preset.Bottom),
                IsHitTestVisible = false
            };
            Canvas.SetTop(_bottomRect, _h * _preset.BaseY);

            BuildWavePaths();

            Canvas.Children.Add(_topRect);
            Canvas.Children.Add(_bottomRect);
            foreach (var p in _glowPaths)
                Canvas.Children.Add(p);
            foreach (var p in _mainPaths)
                Canvas.Children.Add(p);
        }

        private void BuildWavePaths()
        {
            _mainPaths.Clear();
            _glowPaths.Clear();
            _ys.Clear();

            int samples = Math.Max(2, (int)(_w / 6) + 2);
            _xs = new double[samples];
            for (int i = 0; i < samples; i++)
                _xs[i] = i * _w / (samples - 1);

            foreach (Layer layer in _preset.Layers)
            {
                _ys.Add(new double[samples]);

                var main = new Path
                {
                    Stroke = new SolidColorBrush(layer.Color),
                    StrokeThickness = layer.Width,
                    IsHitTestVisible = false
                };
                _mainPaths.Add(main);

                if (layer.HasGlow)
                {
                    var glow = new Path
                    {
                        Stroke = new SolidColorBrush(WithAlpha(layer.Glow, 90)),
                        StrokeThickness = layer.GlowWidth,
                        IsHitTestVisible = false
                    };
                    _glowPaths.Add(glow);
                }
                else
                {
                    _glowPaths.Add(null);
                }
            }
        }

        private void RebuildSamples()
        {
            int samples = Math.Max(2, (int)(_w / 6) + 2);
            _xs = new double[samples];
            for (int i = 0; i < samples; i++)
                _xs[i] = i * _w / (samples - 1);

            for (int li = 0; li < _preset.Layers.Length; li++)
                _ys[li] = new double[samples];
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (_w <= 1 || _h <= 1) return;
            _t += 0.05;

            double baseline = _h * _preset.BaseY;

            for (int li = 0; li < _preset.Layers.Length; li++)
            {
                Layer layer = _preset.Layers[li];
                _phases[li] += layer.Speed;

                double breath = 0.72 + 0.28 * Math.Sin(_t * 0.01 + li * 1.7);
                double amp = layer.Amp * breath;

                double[] ys = _ys[li];
                for (int i = 0; i < ys.Length; i++)
                    ys[i] = baseline + Math.Sin(_xs[i] * layer.Freq + _phases[li]) * amp;

                var geom = BuildGeometry(ys);
                _mainPaths[li].Data = geom;
                if (_glowPaths[li] != null)
                    _glowPaths[li].Data = geom;
            }
        }

        private StreamGeometry BuildGeometry(double[] ys)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(_xs[0], ys[0]), false);
                for (int i = 1; i < ys.Length; i++)
                    ctx.LineTo(new Point(_xs[i], ys[i]));
                ctx.EndFigure(false);
            }
            return geometry;
        }

        public override void Resize(double width, double height)
        {
            if (width <= 1 || height <= 1) return;
            _w = width;
            _h = height;

            if (_topRect != null)
            {
                _topRect.Width = _w;
                _topRect.Height = _h * _preset.BaseY;
            }
            if (_bottomRect != null)
            {
                _bottomRect.Width = _w;
                _bottomRect.Height = _h * (1 - _preset.BaseY);
                Canvas.SetTop(_bottomRect, _h * _preset.BaseY);
            }

            RebuildSamples();
        }
    }
}