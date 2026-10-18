using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace TinaPASS.Main
{
    public class StrategyGradientEffect : BackgroundEffect
    {
        private double _w = 1200;
        private double _h = 700;
        private Rectangle? _bg;
        private Ellipse? _glow;
        private TextBlock? _title;
        private double _time;

        public StrategyGradientEffect(Canvas canvas, string effectName) : base(canvas)
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

            var gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1.0, 1.0, RelativeUnit.Relative)
            };
            gradient.GradientStops.Add(new GradientStop { Offset = 0.0, Color = Color.FromRgb(0x02, 0x0A, 0x17) });
            gradient.GradientStops.Add(new GradientStop { Offset = 0.55, Color = Color.FromRgb(0x0B, 0x33, 0x66) });
            gradient.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Color.FromRgb(0x23, 0x8F, 0xFF) });

            _bg = new Rectangle
            {
                Width = _w,
                Height = _h,
                Fill = gradient,
                IsHitTestVisible = false
            };
            Canvas.Children.Add(_bg);

            var radial = new RadialGradientBrush { Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative) };
            radial.GradientStops.Add(new GradientStop { Offset = 0.0, Color = Color.FromArgb(30, 140, 200, 255) });
            radial.GradientStops.Add(new GradientStop { Offset = 1.0, Color = Color.FromArgb(0, 140, 200, 255) });

            _glow = new Ellipse
            {
                Width = _w * 0.55,
                Height = _h * 0.8,
                Fill = radial,
                Opacity = 1.0,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(_glow, (_w - _glow.Width) / 2);
            Canvas.SetTop(_glow, (_h - _glow.Height) / 2);
            Canvas.Children.Add(_glow);

            _title = new TextBlock
            {
                Text = "Mobile Strategy Review",
                FontFamily = new FontFamily("Inter"),
                FontSize = Math.Clamp(Math.Min(_w, _h) * 0.05, 18, 40),
                FontWeight = FontWeight.ExtraBold,
                LetterSpacing = 2,
                Foreground = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            _title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var titleSize = _title.DesiredSize;
            Canvas.SetLeft(_title, (_w - titleSize.Width) / 2);
            Canvas.SetTop(_title, (_h - titleSize.Height) / 2);
            Canvas.Children.Add(_title);
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (_glow == null) return;
            _time += 0.06;
            _glow.Opacity = 0.75 + 0.25 * Math.Sin(_time * 0.01);
        }

        public override void Resize(double width, double height)
        {
            if (width <= 1 || height <= 1) return;
            _w = width;
            _h = height;

            if (_bg != null)
            {
                _bg.Width = _w;
                _bg.Height = _h;
            }
            if (_glow != null)
            {
                _glow.Width = _w * 0.55;
                _glow.Height = _h * 0.8;
                Canvas.SetLeft(_glow, (_w - _glow.Width) / 2);
                Canvas.SetTop(_glow, (_h - _glow.Height) / 2);
            }
            if (_title != null)
            {
                _title.FontSize = Math.Clamp(Math.Min(_w, _h) * 0.05, 18, 40);
                _title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var titleSize = _title.DesiredSize;
                Canvas.SetLeft(_title, (_w - titleSize.Width) / 2);
                Canvas.SetTop(_title, (_h - titleSize.Height) / 2);
            }
        }
    }
}