using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;

namespace TinaPASS.Main
{
    public class SnowfallEffect : BackgroundEffect
    {
        private List<Snowflake> _snowflakes = new();
        private readonly List<Ellipse> _ellipses = new();
        private int _count = 100;
        private double _width = 800;
        private double _height = 500;

        private class Snowflake
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Size { get; set; }
            public double Speed { get; set; }
            public double Wind { get; set; }
            public double Opacity { get; set; }
        }

        public SnowfallEffect(Canvas canvas) : base(canvas) { }

        protected override void InitializeEffect()
        {
            _snowflakes.Clear();
            _width = Canvas.Bounds.Width > 0 ? Canvas.Bounds.Width : 800;
            _height = Canvas.Bounds.Height > 0 ? Canvas.Bounds.Height : 500;

            for (int i = 0; i < _count; i++)
            {
                _snowflakes.Add(new Snowflake
                {
                    X = Random.NextDouble() * _width,
                    Y = Random.NextDouble() * _height,
                    Size = Random.NextDouble() * 5 + 2,
                    Speed = Random.NextDouble() * 2 + 1,
                    Wind = Random.NextDouble() * 0.5 - 0.25,
                    Opacity = Random.NextDouble() * 0.5 + 0.5
                });
            }

            _ellipses.Clear();
            for (int i = 0; i < _snowflakes.Count; i++)
            {
                var flake = _snowflakes[i];
                var ellipse = new Ellipse
                {
                    Width = flake.Size,
                    Height = flake.Size,
                    Fill = Brushes.White,
                    Opacity = flake.Opacity
                };
                _ellipses.Add(ellipse);
                Canvas.Children.Add(ellipse);
            }
        }

        protected override void OnTick(object? sender, EventArgs e)
        {
            if (!IsRunning) return;

            for (int i = 0; i < _snowflakes.Count; i++)
            {
                var flake = _snowflakes[i];

                flake.Y += flake.Speed;
                flake.X += flake.Wind + Math.Sin(flake.Y / 50) * 0.5;

                if (flake.Y > _height)
                {
                    flake.Y = -10;
                    flake.X = Random.NextDouble() * _width;
                }
                if (flake.X < -10) flake.X = _width + 10;
                if (flake.X > _width + 10) flake.X = -10;

                var ellipse = _ellipses[i];
                ellipse.Opacity = flake.Opacity;
                Canvas.SetLeft(ellipse, flake.X);
                Canvas.SetTop(ellipse, flake.Y);
            }
        }

        public override void Resize(double width, double height)
        {
            _width = width;
            _height = height;
        }
    }
}