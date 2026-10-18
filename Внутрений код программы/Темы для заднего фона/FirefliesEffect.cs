using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;

namespace TinaPASS.Main
{
    public class FirefliesEffect : BackgroundEffect
    {
        private List<Firefly> _fireflies = new();
        private readonly List<Ellipse> _ellipses = new();
        private readonly List<SolidColorBrush> _brushes = new();
        private int _count = 30;
        private double _width = 800;
        private double _height = 500;

        private class Firefly
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double TargetX { get; set; }
            public double TargetY { get; set; }
            public double Size { get; set; }
            public double Speed { get; set; }
            public double Opacity { get; set; }
            public double Phase { get; set; }
            public double ColorHue { get; set; }
        }

        public FirefliesEffect(Canvas canvas) : base(canvas) { }

        protected override void InitializeEffect()
        {
            _fireflies.Clear();
            _width = Canvas.Bounds.Width > 0 ? Canvas.Bounds.Width : 800;
            _height = Canvas.Bounds.Height > 0 ? Canvas.Bounds.Height : 500;

            for (int i = 0; i < _count; i++)
            {
                _fireflies.Add(new Firefly
                {
                    X = Random.NextDouble() * _width,
                    Y = Random.NextDouble() * _height,
                    TargetX = Random.NextDouble() * _width,
                    TargetY = Random.NextDouble() * _height,
                    Size = Random.NextDouble() * 4 + 2,
                    Speed = Random.NextDouble() * 0.02 + 0.005,
                    Opacity = Random.NextDouble() * 0.8 + 0.2,
                    Phase = Random.NextDouble() * Math.PI * 2,
                    ColorHue = Random.NextDouble() * 360
                });
            }

            _ellipses.Clear();
            _brushes.Clear();
            for (int i = 0; i < _fireflies.Count; i++)
            {
                var fly = _fireflies[i];
                var brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                var ellipse = new Ellipse
                {
                    Width = fly.Size * 3,
                    Height = fly.Size * 3,
                    Fill = brush,
                    Opacity = fly.Opacity * 0.6
                };
                _ellipses.Add(ellipse);
                _brushes.Add(brush);
                Canvas.Children.Add(ellipse);
            }
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (!IsRunning) return;

            for (int i = 0; i < _fireflies.Count; i++)
            {
                var fly = _fireflies[i];

                fly.X += (fly.TargetX - fly.X) * fly.Speed;
                fly.Y += (fly.TargetY - fly.Y) * fly.Speed;

                if (Math.Abs(fly.X - fly.TargetX) < 5 && Math.Abs(fly.Y - fly.TargetY) < 5)
                {
                    fly.TargetX = Random.NextDouble() * _width;
                    fly.TargetY = Random.NextDouble() * _height;
                }

                fly.Opacity = 0.3 + Math.Sin(fly.Phase) * 0.3 + 0.4;
                fly.Phase += 0.05;

                var color = Color.FromRgb(
                    (byte)(200 + Math.Sin(fly.ColorHue) * 55),
                    (byte)(200 + Math.Sin(fly.ColorHue + 2) * 55),
                    (byte)(200 + Math.Sin(fly.ColorHue + 4) * 55)
                );

                var ellipse = _ellipses[i];
                _brushes[i].Color = color;
                ellipse.Opacity = fly.Opacity * 0.6;
                Canvas.SetLeft(ellipse, fly.X - fly.Size);
                Canvas.SetTop(ellipse, fly.Y - fly.Size);
            }
        }

        public override void Resize(double width, double height)
        {
            _width = width;
            _height = height;
        }
    }
}