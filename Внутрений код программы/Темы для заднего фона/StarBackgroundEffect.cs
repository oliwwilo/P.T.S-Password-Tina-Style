using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace TinaPASS.Main
{
    public class StarBackgroundEffect : BackgroundEffect
    {
        private List<Star> _stars = new();
        private readonly List<Avalonia.Controls.Shapes.Path> _paths = new();
        private readonly List<RotateTransform> _rotations = new();
        private readonly List<SolidColorBrush> _brushes = new();
        private Geometry? _starGeometry;
        private int _starCount = 150;
        private double _width = 800;
        private double _height = 500;
        
        private class Star
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Size { get; set; }
            public double Opacity { get; set; }
            public double Speed { get; set; }
            public double Phase { get; set; }
            public double ColorPhase { get; set; }
            public double Rotation { get; set; }
            public double ColorSpeed { get; set; }
        }

        private Color[] _colors = new Color[]
        {
            Color.FromRgb(255, 255, 255),
            Color.FromRgb(255, 220, 200),
            Color.FromRgb(200, 220, 255),
            Color.FromRgb(230, 200, 255),
            Color.FromRgb(255, 200, 240),
            Color.FromRgb(200, 255, 220),
            Color.FromRgb(255, 230, 180),
        };

        private const string STAR_PATH = "M 0,-10 L 2.5,-3 L 9.5,-3 L 4,2 L 6,9 L 0,5 L -6,9 L -4,2 L -9.5,-3 L -2.5,-3 Z";

        public StarBackgroundEffect(Canvas canvas) : base(canvas) { }

        protected override void InitializeEffect()
        {
            _stars.Clear();
            _width = Canvas.Bounds.Width > 0 ? Canvas.Bounds.Width : 800;
            _height = Canvas.Bounds.Height > 0 ? Canvas.Bounds.Height : 500;

            for (int i = 0; i < _starCount; i++)
            {
                double size = Random.NextDouble() * 8 + 4;
                _stars.Add(new Star
                {
                    X = Random.NextDouble() * _width,
                    Y = Random.NextDouble() * _height,
                    Size = size,
                    Opacity = Random.NextDouble() * 0.3 + 0.3,
                    Speed = Random.NextDouble() * 0.02 + 0.005,
                    Phase = Random.NextDouble() * Math.PI * 2,
                    ColorPhase = Random.NextDouble(),
                    Rotation = Random.NextDouble() * 360,
                    ColorSpeed = Random.NextDouble() * 0.02 + 0.005
                });
            }

            _starGeometry = Geometry.Parse(STAR_PATH);
            _paths.Clear();
            _rotations.Clear();
            _brushes.Clear();
            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                var brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                var rotation = new RotateTransform(star.Rotation);
                var path = new Avalonia.Controls.Shapes.Path
                {
                    Data = _starGeometry,
                    Fill = brush,
                    Opacity = star.Opacity,
                    Width = star.Size,
                    Height = star.Size,
                    Stretch = Stretch.Uniform,
                    RenderTransform = rotation
                };
                _paths.Add(path);
                _rotations.Add(rotation);
                _brushes.Add(brush);
                Canvas.Children.Add(path);
            }
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (!IsRunning) return;

            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                var path = _paths[i];

                _brushes[i].Color = GetColorForStar(star);
                path.Opacity = star.Opacity;
                _rotations[i].Angle = star.Rotation;

                Canvas.SetLeft(path, star.X - star.Size / 2);
                Canvas.SetTop(path, star.Y - star.Size / 2);

                // Обновление
                star.Opacity = 0.15 + Math.Sin(star.Phase) * 0.25 + 0.35;
                star.Phase += star.Speed;
                star.Rotation += 0.15;
                if (star.Rotation > 360) star.Rotation = 0;
                star.ColorPhase += star.ColorSpeed;
                if (star.ColorPhase >= 1.0) star.ColorPhase = 0;
            }
        }

        private Color GetColorForStar(Star star)
        {
            double phase = star.ColorPhase % 1.0;
            double totalColors = _colors.Length;
            double position = phase * totalColors;
            int index = (int)position;
            int nextIndex = (index + 1) % _colors.Length;
            double t = position - index;

            return Color.FromRgb(
                (byte)(_colors[index].R + (_colors[nextIndex].R - _colors[index].R) * t),
                (byte)(_colors[index].G + (_colors[nextIndex].G - _colors[index].G) * t),
                (byte)(_colors[index].B + (_colors[nextIndex].B - _colors[index].B) * t)
            );
        }

        public override void Resize(double width, double height)
        {
            _width = width;
            _height = height;
            foreach (var star in _stars)
            {
                star.X = Random.NextDouble() * _width;
                star.Y = Random.NextDouble() * _height;
            }
        }
    }
}