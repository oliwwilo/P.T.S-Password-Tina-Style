using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading;

namespace TinaPASS.Main
{
    public class SlideTip : IDisposable
    {
        private readonly Border _panel = new Border();
        private readonly Border _tape = new Border();
        private readonly TranslateTransform _pos = new TranslateTransform();
        private readonly TextBlock _text = new TextBlock();
        private readonly Control _anchor;
        private readonly Panel _host;
        private CancellationTokenSource? _hideCts;
        private bool _attached;
        private double _fullWidth = 0;
        private double _fullHeight = 0;

        public SlideTip(Control anchor, Panel host, string text)
        {
            _anchor = anchor;
            _host = host;

            _panel.IsHitTestVisible = false;
            _panel.HorizontalAlignment = HorizontalAlignment.Left;
            _panel.VerticalAlignment = VerticalAlignment.Top;
            _panel.RenderTransform = _pos;

            _text.Text = text;
            _text.Foreground = Brushes.White;
            _text.FontSize = 12;
            _text.FontFamily = new FontFamily("Inter, Segoe UI");
            _text.VerticalAlignment = VerticalAlignment.Center;

            _tape.Padding = new Thickness(0, 8);
            _tape.Width = 8;
            _tape.HorizontalAlignment = HorizontalAlignment.Right;
            _tape.Opacity = 1;
            _tape.RenderTransform = new TranslateTransform();

            _tape.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Border.WidthProperty,
                    Duration = TimeSpan.FromMilliseconds(240),
                    Easing = new CubicEaseOut()
                }
            };

            _tape.Child = _text;
            _panel.Child = _tape;

            _anchor.PointerEntered += (_, _) => Show();
            _anchor.PointerExited += (_, _) => Hide();
        }

        private void Show()
        {
            _hideCts?.Cancel();
            _hideCts = null;

            if (!_attached)
            {
                _host.Children.Add(_panel);
                _attached = true;
            }

            _text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _fullWidth = _text.DesiredSize.Width + 24 + 2;
            _fullHeight = _text.DesiredSize.Height + 16 + 2;

            _panel.Width = _fullWidth;
            _panel.Height = _fullHeight;

            var pt = _anchor.TranslatePoint(new Point(0, 0), _host);
            if (pt.HasValue)
            {
                double restX = pt.Value.X - _fullWidth;
                double restY = pt.Value.Y + (_anchor.Bounds.Height - _fullHeight) / 2;
                _pos.X = restX;
                _pos.Y = restY;
            }

            Dispatcher.UIThread.Post(() =>
            {
                _tape.Width = _fullWidth;
            });
        }

        private void Hide()
        {
            _hideCts?.Cancel();
            var cts = new CancellationTokenSource();
            _hideCts = cts;

            _tape.Width = 4;

            DispatcherTimer.RunOnce(() =>
            {
                if (cts.IsCancellationRequested) return;
                if (_attached)
                {
                    _host.Children.Remove(_panel);
                    _attached = false;
                }
            }, TimeSpan.FromMilliseconds(320));
        }

        public void Dispose()
        {
            _hideCts?.Cancel();
            if (_attached)
            {
                _host.Children.Remove(_panel);
                _attached = false;
            }
        }
    }
}
