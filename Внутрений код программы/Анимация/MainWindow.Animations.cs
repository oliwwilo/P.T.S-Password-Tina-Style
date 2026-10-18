using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;  // ← Для async/await

namespace TinaPASS.Main;

public partial class MainWindow
{
    // ============ АНИМАЦИЯ ПОДСВЕТКИ ГРУППЫ ============

    private CancellationTokenSource? _groupHighlightCts;

    /// <summary>
    /// Цвет подсветки активной группы — простой зелёный.
    /// Используется и при анимации, и как постоянный цвет активной группы.
    /// </summary>
    private static readonly Color HighlightColor = Color.FromRgb(0x00, 0xE6, 0x76);
    private static readonly IBrush HighlightGradientBrush = new SolidColorBrush(HighlightColor);

    /// <summary>
    /// Плавное появление подсветки активной группы (мягкий fade без пульсации).
    /// Предыдущая анимация отменяется, чтобы не было наложений и лагов.
    /// </summary>
    public async Task AnimateGroupHighlight(Border highlightBar, int steps = 16, int delayMs = 12)
    {
        _groupHighlightCts?.Cancel();
        var cts = new CancellationTokenSource();
        _groupHighlightCts = cts;

        highlightBar.Background = HighlightGradientBrush;
        highlightBar.BoxShadow = new BoxShadows();
        highlightBar.Opacity = 0;

        try
        {
            for (int i = 1; i <= steps; i++)
            {
                if (cts.IsCancellationRequested) return;
                double t = (double)i / steps;
                double eased = 1 - Math.Pow(1 - t, 3);
                highlightBar.Opacity = eased;
                await Task.Delay(delayMs);
            }
        }
        finally
        {
            highlightBar.Opacity = 1;
            highlightBar.Background = HighlightGradientBrush;
        }
    }

    public void CancelGroupHighlight() => _groupHighlightCts?.Cancel();
   public void StartDragGroup(Border group)
    {
        group.Opacity = 0.6;
        group.ZIndex = 100;
    }

    public void StopDragGroup(Border group)
    {
        group.Opacity = 1;
        group.RenderTransform = null;
        group.ZIndex = 0;
    }
    private void ShiftGroups(StackPanel panel, int fromIndex, int toIndex, Border draggedItem)
    {
        int start = Math.Min(fromIndex, toIndex);
        int end = Math.Max(fromIndex, toIndex);
        for (int i = start; i <= end && i < panel.Children.Count; i++)
        {
            var child = panel.Children[i];
            if (child is Border b && b != draggedItem)
            {
                b.RenderTransform = new TranslateTransform(0, -25);
                b.Opacity = 0.7;
            }
        }
    }
    public async Task AnimateGroupReturn(Border group, int steps = 10, int delayMs = 10)
    {
        if (group.RenderTransform is TranslateTransform transform)
        {
            double startX = transform.X;
            double startY = transform.Y;

            for (int i = steps; i >= 0; i--)
            {
                double progress = i / (double)steps;
                transform.X = startX * progress;
                transform.Y = startY * progress;
                group.Opacity = 0.5 + progress * 0.5;
                await Task.Delay(delayMs);
            }
        }

        group.Opacity = 1;
        group.RenderTransform = null;
        group.ZIndex = 0;
    }
    public async Task PressEffect(Control element, double scale = 0.95, int steps = 5, int delayMs = 15)
    {
        for (int i = 0; i <= steps; i++)
        {
            double progress = i / (double)steps;
            double currentScale = 1 - (1 - scale) * progress;
            element.RenderTransform = new ScaleTransform(currentScale, currentScale);
            await Task.Delay(delayMs);
        }
        
        for (int i = steps; i >= 0; i--)
        {
            double progress = i / (double)steps;
            double currentScale = 1 - (1 - scale) * progress;
            element.RenderTransform = new ScaleTransform(currentScale, currentScale);
            await Task.Delay(delayMs);
        }
        
        element.RenderTransform = new ScaleTransform(1, 1);
    }

    // ============ ПЛАВНОЕ ПОЯВЛЕНИЕ/СКРЫТИЕ ВСПЛЫВАЮЩИХ ПАНЕЛЕЙ ============

    public void FadeInPanel(Control panel, int steps = 18, int delayMs = 14)
        => PanelFader.FadeIn(panel, steps, delayMs);

    public void FadeOutPanel(Control panel, int steps = 18, int delayMs = 14)
        => PanelFader.FadeOut(panel, steps, delayMs);

    public void PopInPanel(Control panel, int steps = 22, int delayMs = 12)
        => PanelFader.PopIn(panel, steps, delayMs);
}

/// <summary>
/// Общий помощник плавного появления/скрытия панелей (fade-in / fade-out).
/// </summary>
public static class PanelFader
{
    private static readonly Dictionary<Control, CancellationTokenSource> _cts = new();

    public static async void FadeIn(Control? panel, int steps = 18, int delayMs = 14)
    {
        if (panel == null) return;

        var cts = Reset(panel);
        panel.IsVisible = true;
        panel.Opacity = 0;
        await Task.Delay(delayMs);

        try
        {
            for (int i = 1; i <= steps; i++)
            {
                if (cts.IsCancellationRequested) return;
                double t = (double)i / steps;
                double eased = t * t * t * (t * (t * 6 - 15) + 10);
                panel.Opacity = eased;
                await Task.Delay(delayMs);
            }
        }
        finally
        {
            if (!cts.IsCancellationRequested) panel.Opacity = 1;
        }
    }

    public static async void FadeOut(Control? panel, int steps = 18, int delayMs = 14)
    {
        if (panel == null) return;

        if (!panel.IsVisible || panel.Opacity <= 0.001)
        {
            panel.Opacity = 1;
            panel.IsVisible = false;
            return;
        }

        var cts = Reset(panel);
        double start = panel.Opacity;
        await Task.Delay(delayMs);

        try
        {
            for (int i = 1; i <= steps; i++)
            {
                if (cts.IsCancellationRequested) return;
                double t = (double)i / steps;
                double eased = t * t * t * (t * (t * 6 - 15) + 10);
                panel.Opacity = start * (1 - eased);
                await Task.Delay(delayMs);
            }
        }
        finally
        {
            if (!cts.IsCancellationRequested)
            {
                panel.Opacity = 1;
                panel.IsVisible = false;
            }
        }
    }

    public static async void PopIn(Control? panel, int steps = 22, int delayMs = 12)
    {
        if (panel == null) return;

        var cts = Reset(panel);
        panel.IsVisible = true;
        panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        try
        {
            for (int i = 0; i <= steps; i++)
            {
                if (cts.IsCancellationRequested) return;
                double t = (double)i / steps;
                double eased = EaseOutBack(t);

                panel.Opacity = Math.Clamp(eased, 0, 1);

                double scale = 0.86 + 0.14 * eased;
                double dy = (1 - eased) * 26;

                var group = new TransformGroup();
                group.Children.Add(new ScaleTransform(scale, scale));
                group.Children.Add(new TranslateTransform(0, dy));
                panel.RenderTransform = group;

                await Task.Delay(delayMs);
            }
        }
        finally
        {
            if (!cts.IsCancellationRequested)
            {
                panel.Opacity = 1;
                panel.RenderTransform = new ScaleTransform(1, 1);
            }
        }
    }

    private static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        double x = t - 1;
        return 1 + c3 * x * x * x + c1 * x * x;
    }

    private static CancellationTokenSource Reset(Control panel)
    {
        if (_cts.TryGetValue(panel, out var old)) old.Cancel();
        var cts = new CancellationTokenSource();
        _cts[panel] = cts;
        return cts;
    }
}