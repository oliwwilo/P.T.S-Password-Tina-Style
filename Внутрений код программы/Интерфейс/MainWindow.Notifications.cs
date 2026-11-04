using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class MainWindow
{
    private Canvas? _currentCanvas;
    private Border? _currentDialog;
    private Border? _loadingOverlay;
    private TextBlock? _loadingText;
    private bool _isClosingCanvas;
    private int _loadingVisibilityVersion;

    // ============ ДИАЛОГ ПОДТВЕРЖДЕНИЯ ============
    private void ShowConfirmation(string message, Action onConfirm)
    {
        // Если предыдущий диалог ещё открыт/закрывается — убираем его мгновенно,
        // чтобы не наслаивать затемнения.
        if (_currentCanvas != null)
        {
            this.SizeChanged -= OnWindowSizeChangedForCanvas;
            (this.Content as Panel)?.Children.Remove(_currentCanvas);
            _currentCanvas = null;
            _currentDialog = null;
            _isClosingCanvas = false;
        }

        _currentCanvas = new Canvas
        {
            Background = Brush.Parse("#80000000"),
            Width = this.Bounds.Width,
            Height = this.Bounds.Height,
            Opacity = 0
        };

        _currentDialog = CreateConfirmationDialog(message, onConfirm);
        _currentDialog.Opacity = 0;
        _currentDialog.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _currentDialog.RenderTransform = TransformOperations.Parse("scale(0.92) translate(0px, 12px)");

        CenterDialogOnCanvas();
        _currentCanvas.Children.Add(_currentDialog);
        this.SizeChanged += OnWindowSizeChangedForCanvas;
        (this.Content as Panel)?.Children.Add(_currentCanvas);

        AnimateCanvasIn();
    }

    private async void AnimateCanvasIn()
    {
        var canvas = _currentCanvas;
        var dialog = _currentDialog;
        if (canvas == null || dialog == null) return;

        var duration = TimeSpan.FromMilliseconds(200);

        await Task.Yield();
        if (!ReferenceEquals(canvas, _currentCanvas)) return;

        canvas.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            }
        };
        dialog.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            },
            new TransformOperationsTransition
            {
                Property = Visual.RenderTransformProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            }
        };

        canvas.Opacity = 1;
        dialog.Opacity = 1;
        dialog.RenderTransform = TransformOperations.Parse("scale(1) translate(0px, 0px)");
    }
    private Border CreateConfirmationDialog(string message, Action onConfirm)
    {
        var dialog = new Border
        {
            Background = Brush.Parse("#1a1a2e"),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(25, 20, 25, 20),
            Width = 340,
            BorderBrush = Brush.Parse("#FF4444"),
            BorderThickness = new Thickness(1)
        };

        var content = new StackPanel { Spacing = 15 };

        content.Children.Add(new TextBlock
        {
            Text = FluentIcons.Warning,
            FontFamily = new FontFamily(FluentIcons.Font),
            FontSize = 48,
            Foreground = Brush.Parse("#FF6B6B"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        content.Children.Add(new TextBlock
        {
            Text = "Подтверждение",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        content.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 13,
            Foreground = Brush.Parse("#CCCCCC"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = Avalonia.Media.TextAlignment.Center
        });

        var buttonsPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 15,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var cancelBtn = new Button
        {
            Content = "Отмена",
            Background = Brush.Parse("#2D2D2D"),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Width = 100,
            Height = 35,
            FontSize = 13,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var confirmBtn = new Button
        {
            Content = "Удалить",
            Background = Brush.Parse("#FF4444"),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Width = 100,
            Height = 35,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        buttonsPanel.Children.Add(cancelBtn);
        buttonsPanel.Children.Add(confirmBtn);
        content.Children.Add(buttonsPanel);
        dialog.Child = content;

        cancelBtn.Click += async (s, e) => await CloseCanvas();
        confirmBtn.Click += async (s, e) =>
        {
            await CloseCanvas();
            onConfirm();
        };

        return dialog;
    }

    public void ShowLoading(string message)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            _loadingVisibilityVersion++;
            bool isNew = _loadingOverlay == null;

            if (isNew)
            {
                _loadingOverlay = new Border
                {
                    Background = Brush.Parse("#80000000"),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Padding = new Thickness(30, 20, 30, 20),
                    Opacity = 0
                };

                var stackPanel = new StackPanel { Spacing = 15 };
                var spinner = new ProgressBar
                {
                    Width = 40,
                    Height = 40,
                    IsIndeterminate = true,
                    Foreground = Brush.Parse("#CBFF21")
                };

                _loadingText = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeight.SemiBold
                };

                stackPanel.Children.Add(spinner);
                stackPanel.Children.Add(_loadingText);
                _loadingOverlay.Child = stackPanel;
            }
            else if (_loadingText != null)
            {
                _loadingText.Text = message;
            }

            var overlay = _loadingOverlay;
            if (overlay == null) return;

            var parent = this.Content as Panel;
            if (parent != null && !parent.Children.Contains(overlay))
                parent.Children.Add(overlay);

            bool wasVisible = overlay.IsVisible && overlay.Opacity > 0.5;
            overlay.IsVisible = true;

            if (isNew || !wasVisible)
            {
                overlay.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = Visual.OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(180),
                        Easing = new CubicEaseOut()
                    }
                };
            }

            overlay.Opacity = 1;
        });
    }

    public void HideLoading()
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var overlay = _loadingOverlay;
            if (overlay == null || !overlay.IsVisible) return;

            // Если за время анимации загрузку показали снова — не скрываем её.
            int version = _loadingVisibilityVersion;

            overlay.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(150),
                    Easing = new CubicEaseIn()
                }
            };
            overlay.Opacity = 0;

            await Task.Delay(150);

            if (version != _loadingVisibilityVersion) return;
            if (!ReferenceEquals(_loadingOverlay, overlay)) return;

            overlay.IsVisible = false;
            overlay.Opacity = 1;
            overlay.Transitions = null;
        });
    }

    // ============ ВСПЫШКИ ============
    // Все уведомления живут в одном стеке в правом верхнем углу,
    // поэтому появляются с одной стороны и не перекрывают друг друга.
    private const int MaxVisibleNotifications = 3;
    private StackPanel? _notificationHost;
    private readonly List<Border> _activeNotifications = new();

    public async void ShowSuccessFlash(string message)
    {
        await ShowNotification(message, "#4CAF50", TimeSpan.FromMilliseconds(2500));
    }

    public async void ShowError(string message)
    {
        await ShowNotification(message, "#FF4444", TimeSpan.FromMilliseconds(3200));
    }

    public async void ShowWarning(string message)
    {
        await ShowNotification(message, "#E6A700", TimeSpan.FromMilliseconds(2600));
    }

    private StackPanel? EnsureNotificationHost()
    {
        var parent = this.Content as Panel;
        if (parent == null) return null;

        if (_notificationHost == null || !parent.Children.Contains(_notificationHost))
        {
            _activeNotifications.Clear();
            _notificationHost = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, 80, 20, 0),
                MaxWidth = 400,
                ZIndex = 999,
                IsHitTestVisible = false
            };
            parent.Children.Add(_notificationHost);
        }

        return _notificationHost;
    }

    private async Task ShowNotification(string message, string color, TimeSpan visibleFor)
    {
        try
        {
            var host = EnsureNotificationHost();
            if (host == null) return;

            var notification = new Border
            {
                Background = Brush.Parse(color),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 10, 20, 10),
                Opacity = 0,
                RenderTransform = TransformOperations.Parse("translateX(26px)"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                MaxWidth = 400
            };

            notification.Child = FluentIcons.Build(message, 14, Brushes.White, TextWrapping.Wrap);
            host.Children.Add(notification);
            _activeNotifications.Add(notification);

            // Не даём уведомлениям копиться: самое старое убираем сразу.
            while (_activeNotifications.Count > MaxVisibleNotifications)
            {
                var oldest = _activeNotifications[0];
                _activeNotifications.RemoveAt(0);
                host.Children.Remove(oldest);
            }

            // Появление: плавный выезд справа + fade.
            var enter = TimeSpan.FromMilliseconds(220);
            await Task.Yield();
            if (!host.Children.Contains(notification)) return;

            notification.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = enter,
                    Easing = new CubicEaseOut()
                },
                new TransformOperationsTransition
                {
                    Property = Visual.RenderTransformProperty,
                    Duration = enter,
                    Easing = new CubicEaseOut()
                }
            };
            notification.Opacity = 1;
            notification.RenderTransform = TransformOperations.Parse("translateX(0px)");

            await Task.Delay(visibleFor);
            if (!host.Children.Contains(notification)) return;

            // Исчезновение: мягкий уход вправо + fade.
            var exit = TimeSpan.FromMilliseconds(180);
            notification.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = exit,
                    Easing = new CubicEaseIn()
                },
                new TransformOperationsTransition
                {
                    Property = Visual.RenderTransformProperty,
                    Duration = exit,
                    Easing = new CubicEaseIn()
                }
            };
            notification.Opacity = 0;
            notification.RenderTransform = TransformOperations.Parse("translateX(16px)");

            await Task.Delay(exit);

            host.Children.Remove(notification);
            _activeNotifications.Remove(notification);
        }
        catch (Exception ex)
        {
            // Если что-то пошло не так – просто игнорируем, чтобы не ломать UI
            Console.WriteLine($"Ошибка при показе уведомления: {ex.Message}");
        }
    }

    // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============
    private void CenterDialogOnCanvas()
    {
        if (_currentCanvas == null || _currentDialog == null) return;
        Canvas.SetLeft(_currentDialog, (this.Bounds.Width - 340) / 2);
        Canvas.SetTop(_currentDialog, (this.Bounds.Height - 220) / 2);
    }

    private void OnWindowSizeChangedForCanvas(object? sender, SizeChangedEventArgs e)
    {
        if (_currentCanvas != null && _currentCanvas.IsVisible)
        {
            _currentCanvas.Width = this.Bounds.Width;
            _currentCanvas.Height = this.Bounds.Height;
            CenterDialogOnCanvas();
        }
    }

    private async Task CloseCanvas()
    {
        if (_currentCanvas == null || _isClosingCanvas) return;

        _isClosingCanvas = true;
        var canvas = _currentCanvas;
        var dialog = _currentDialog;
        this.SizeChanged -= OnWindowSizeChangedForCanvas;

        var duration = TimeSpan.FromMilliseconds(160);

        canvas.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = new CubicEaseIn()
            }
        };

        if (dialog != null)
        {
            dialog.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = duration,
                    Easing = new CubicEaseIn()
                },
                new TransformOperationsTransition
                {
                    Property = Visual.RenderTransformProperty,
                    Duration = duration,
                    Easing = new CubicEaseIn()
                }
            };
            dialog.Opacity = 0;
            dialog.RenderTransform = TransformOperations.Parse("scale(0.95) translate(0px, 8px)");
        }

        canvas.Opacity = 0;
        await Task.Delay(duration);

        (this.Content as Panel)?.Children.Remove(canvas);

        if (ReferenceEquals(_currentCanvas, canvas))
        {
            _currentCanvas = null;
            _currentDialog = null;
        }
        _isClosingCanvas = false;
    }
}
