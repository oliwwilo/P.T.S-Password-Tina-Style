using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
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
    /// Анимация выполняется на render-потоке через Transitions, а не ручным циклом.
    /// Предыдущая анимация отменяется, чтобы не было наложений и лагов.
    /// </summary>
    public async Task AnimateGroupHighlight(Border highlightBar, int steps = 16, int delayMs = 12)
    {
        _groupHighlightCts?.Cancel();
        var cts = new CancellationTokenSource();
        _groupHighlightCts = cts;

        var duration = TimeSpan.FromMilliseconds(
            Math.Clamp(steps * Math.Max(delayMs, 1), 120, 320));

        // Стартовое состояние — без анимации, чтобы не мигало.
        highlightBar.Transitions = null;
        highlightBar.Background = HighlightGradientBrush;
        highlightBar.BoxShadow = new BoxShadows();
        highlightBar.Opacity = 0;

        await Task.Yield();
        if (cts.IsCancellationRequested) return;

        highlightBar.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = new CubicEaseOut()
            }
        };
        highlightBar.Opacity = 1;

        await Task.Delay(duration);
        if (cts.IsCancellationRequested) return;

        highlightBar.Opacity = 1;
        highlightBar.Background = HighlightGradientBrush;
        highlightBar.Transitions = null;
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
/// Общий помощник плавного появления/скрытия панелей.
/// Анимации выполняются на render-потоке через Transitions — без ручных циклов
/// и Task.Delay на каждый кадр, поэтому движение остаётся плавным при любой нагрузке.
/// </summary>
public static class PanelFader
{
    private static readonly Dictionary<Control, CancellationTokenSource> _cts = new();

    /// <summary>Длительность перехода выводим из старых параметров steps/delayMs.</summary>
    private static TimeSpan DurationFor(int steps, int delayMs)
        => TimeSpan.FromMilliseconds(Math.Clamp(steps * Math.Max(delayMs, 1), 120, 420));

    private static Transitions OpacityTransition(TimeSpan duration, Easing easing)
        => new()
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = easing
            }
        };

    public static async void FadeIn(Control? panel, int steps = 18, int delayMs = 14)
    {
        if (panel == null) return;

        var cts = Reset(panel);
        var duration = DurationFor(steps, delayMs);

        // Стартовое состояние ставим мгновенно, без анимации.
        panel.Transitions = null;
        panel.Opacity = 0;
        panel.IsVisible = true;

        await Task.Yield();
        if (cts.IsCancellationRequested) return;

        panel.Transitions = OpacityTransition(duration, new CubicEaseOut());
        panel.Opacity = 1;

        await Task.Delay(duration);
        if (cts.IsCancellationRequested) return;

        panel.Opacity = 1;
        panel.Transitions = null;
    }

    public static async void FadeOut(Control? panel, int steps = 18, int delayMs = 14)
    {
        if (panel == null) return;

        var duration = DurationFor(steps, delayMs);

        if (!panel.IsVisible || panel.Opacity <= 0.001)
        {
            panel.Transitions = null;
            panel.Opacity = 0;
            panel.IsVisible = false;
            return;
        }

        var cts = Reset(panel);
        panel.Transitions = OpacityTransition(duration, new CubicEaseIn());
        panel.Opacity = 0;

        await Task.Delay(duration);
        if (cts.IsCancellationRequested) return;

        panel.IsVisible = false;
        panel.Opacity = 0;
        panel.Transitions = null;
    }

    public static async void PopIn(Control? panel, int steps = 22, int delayMs = 12)
    {
        if (panel == null) return;

        var cts = Reset(panel);
        var duration = DurationFor(steps, delayMs);

        panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        panel.Transitions = null;
        panel.Opacity = 0;
        panel.RenderTransform = TransformOperations.Parse("scale(0.9) translate(0px, 18px)");
        panel.IsVisible = true;

        await Task.Yield();
        if (cts.IsCancellationRequested) return;

        panel.Transitions = new Transitions
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
        panel.Opacity = 1;
        panel.RenderTransform = TransformOperations.Parse("scale(1) translate(0px, 0px)");

        await Task.Delay(duration);
        if (cts.IsCancellationRequested) return;

        panel.Transitions = null;
        panel.Opacity = 1;
        panel.RenderTransform = null;
    }

    private static CancellationTokenSource Reset(Control panel)
    {
        if (_cts.TryGetValue(panel, out var old)) old.Cancel();
        var cts = new CancellationTokenSource();
        _cts[panel] = cts;
        return cts;
    }
}