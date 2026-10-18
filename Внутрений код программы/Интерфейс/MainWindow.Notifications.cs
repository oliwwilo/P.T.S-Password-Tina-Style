using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class MainWindow
{
    private Canvas? _currentCanvas;
    private Border? _currentDialog;
    private Border? _loadingOverlay;
    private TextBlock? _loadingText;

    // ============ ДИАЛОГ ПОДТВЕРЖДЕНИЯ ============
    private void ShowConfirmation(string message, Action onConfirm)
    {
        _currentCanvas = new Canvas
        {
            Background = Brush.Parse("#80000000"),
            Width = this.Bounds.Width,
            Height = this.Bounds.Height
        };

        _currentDialog = CreateConfirmationDialog(message, onConfirm);
        CenterDialogOnCanvas();
        _currentCanvas.Children.Add(_currentDialog);
        this.SizeChanged += OnWindowSizeChangedForCanvas;
        (this.Content as Panel)?.Children.Add(_currentCanvas);
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

        cancelBtn.Click += (s, e) => CloseCanvas();
        confirmBtn.Click += (s, e) =>
        {
            CloseCanvas();
            onConfirm();
            ShowSuccessFlash($"{FluentIcons.CheckMark} Удаление выполнено!");
        };

        return dialog;
    }

   public void ShowLoading(string message)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_loadingOverlay == null)
            {
                _loadingOverlay = new Border
                {
                    Background = Brush.Parse("#80000000"),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Padding = new Thickness(30, 20, 30, 20)
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
            else
            {
                _loadingText.Text = message;
                _loadingOverlay.IsVisible = true;
            }

            var parent = this.Content as Panel;
            if (parent != null && !parent.Children.Contains(_loadingOverlay))
                parent.Children.Add(_loadingOverlay);
            _loadingOverlay.IsVisible = true;
        });
    }

    public void HideLoading()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_loadingOverlay != null)
                _loadingOverlay.IsVisible = false;
        });
    }

    // ============ ВСПЫШКИ ============
    public async void ShowSuccessFlash(string message)
    {
        await ShowRightFlash(message, "#4CAF50", 80);
    }

    public async void ShowError(string message)
    {
        await ShowRightFlash(message, "#FF4444", -1);
    }

    private async Task ShowRightFlash(string message, string color, int topOffset = 80)
    {
        try
        {
            // Создаём всплывающий элемент
            var flash = new Border
            {
                Background = Brush.Parse(color),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 10, 20, 10),
                Opacity = 0,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, topOffset, 20, 0),
                MaxWidth = 400,
                ZIndex = 999
            };

            var textBlock = FluentIcons.Build(message, 14, Brushes.White, TextWrapping.Wrap);

            flash.Child = textBlock;

            // Находим родительский контейнер (Panel)
            var parent = this.Content as Panel;
            if (parent == null) return;

            // Добавляем всплывашку на экран
            parent.Children.Add(flash);

            // Анимация появления
            for (int i = 0; i <= 10; i++)
            {
                flash.Opacity = i * 0.1;
                await Task.Delay(10);
            }

            // Задержка (показываем сообщение)
            await Task.Delay(2500);

            // Анимация исчезновения
            for (int i = 10; i >= 0; i--)
            {
                flash.Opacity = i * 0.1;
                await Task.Delay(10);
            }

            // Удаляем элемент
            parent.Children.Remove(flash);
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

    private void CloseCanvas()
    {
        if (_currentCanvas != null)
        {
            this.SizeChanged -= OnWindowSizeChangedForCanvas;
            (this.Content as Panel)?.Children.Remove(_currentCanvas);
            _currentCanvas = null;
            _currentDialog = null;
        }
    }
}
