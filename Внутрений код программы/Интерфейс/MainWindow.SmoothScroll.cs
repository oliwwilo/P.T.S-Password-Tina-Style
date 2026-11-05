using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace TinaPASS.Main;

public partial class MainWindow : Window
{
    // ============ Плавный скролл ============
    // Шаг колеса за один щелчок (px). Больше, чем стандартные 50, — иначе скролл «вязкий».
    private const double WheelScrollStep = 120.0;
    // Постоянная времени сглаживания (сек): за неё проходит ~63% оставшегося пути.
    // Меньше — резче и быстрее, больше — плавнее и «тягучее».
    private const double SmoothScrollTimeConstant = 0.06;
    // Минимальный шаг, чтобы анимация не залипала на маленьких дельтах тачпада.
    private const double SmoothScrollMinStep = 0.5;
    // Если позицию изменили извне больше чем на столько пикселей — не мешаем (ползунок/клавиатура).
    private const double SmoothScrollExternalDelta = 4.0;

    private bool _smoothScrollFrameRequested;
    private TimeSpan _smoothScrollLastFrame;

    private void OnSmoothPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var viewer = FindSmoothScrollViewer(e.Source as Visual);
        if (viewer == null) return;

        double maxY = viewer.ScrollBarMaximum.Y;
        if (maxY <= 0) return;

        // Новый target считаем от уже накопленной цели, а не от текущего offset,
        // чтобы быстрые щелчки колеса не «съедали» друг друга.
        double baseY = _smoothScrollTargets.TryGetValue(viewer, out var pending)
            ? pending
            : viewer.Offset.Y;

        double target = Math.Clamp(baseY - e.Delta.Y * WheelScrollStep, 0, maxY);

        _smoothScrollTargets[viewer] = target;
        _smoothScrollLastY[viewer] = viewer.Offset.Y;
        e.Handled = true;

        // Пока идёт анимация, фон не перерисовываем: тяжёлые градиенты со
        // сглаживанием отбирают кадры у прокрутки.
        PauseBackgroundForScroll();
        RequestSmoothScrollFrame();
    }

    private ScrollViewer? FindSmoothScrollViewer(Visual? v)
    {
        while (v != null)
        {
            if (v is Avalonia.Controls.Primitives.ScrollBar)
                return null;
            if (v is ScrollViewer sv && (sv == Skrolbar1 || sv == GroupsScrollViewer))
                return sv;
            v = v.GetVisualParent();
        }
        return null;
    }

    /// <summary>
    /// Просит следующий кадр отрисовки. В отличие от DispatcherTimer, колбэк
    /// вызывается ровно один раз на кадр, поэтому движение не дёргается.
    /// </summary>
    private void RequestSmoothScrollFrame()
    {
        if (_smoothScrollFrameRequested) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            _smoothScrollTargets.Clear();
            _smoothScrollLastY.Clear();
            ResumeBackgroundAfterScroll();
            return;
        }

        _smoothScrollFrameRequested = true;
        topLevel.RequestAnimationFrame(OnSmoothScrollFrame);
    }

    private void OnSmoothScrollFrame(TimeSpan now)
    {
        _smoothScrollFrameRequested = false;

        // Шаг считаем от реального времени кадра, а не от «идеальных» 16 мс:
        // тогда скорость движения не зависит от частоты кадров.
        double dt = _smoothScrollLastFrame == default
            ? 1.0 / 60.0
            : (now - _smoothScrollLastFrame).TotalSeconds;
        _smoothScrollLastFrame = now;

        if (dt <= 0) dt = 1.0 / 60.0;
        if (dt > 0.1) dt = 0.1; // после подвисания не прыгаем через пол-экрана

        double factor = 1.0 - Math.Exp(-dt / SmoothScrollTimeConstant);

        bool any = false;
        foreach (var viewer in _smoothScrollTargets.Keys.ToList())
        {
            if (viewer == null || !viewer.IsVisible)
            {
                _smoothScrollTargets.Remove(viewer);
                _smoothScrollLastY.Remove(viewer);
                continue;
            }

            double target = _smoothScrollTargets[viewer];
            double curY = viewer.Offset.Y;

            // Кто-то прокрутил вручную (ползунок, клавиатура) — прекращаем анимацию.
            if (_smoothScrollLastY.TryGetValue(viewer, out var lastY) &&
                Math.Abs(curY - lastY) > SmoothScrollExternalDelta)
            {
                _smoothScrollTargets.Remove(viewer);
                _smoothScrollLastY.Remove(viewer);
                continue;
            }

            double diff = target - curY;
            if (Math.Abs(diff) < 0.5)
            {
                // Доводим до цели без «хвоста» в полпикселя.
                if (Math.Abs(diff) > 0.01)
                    viewer.Offset = new Vector(viewer.Offset.X, target);

                _smoothScrollTargets.Remove(viewer);
                _smoothScrollLastY.Remove(viewer);
                continue;
            }

            double step = diff * factor;
            if (Math.Abs(step) < SmoothScrollMinStep)
                step = Math.Sign(diff) * SmoothScrollMinStep;

            double newY = Math.Clamp(curY + step, 0, viewer.ScrollBarMaximum.Y);
            viewer.Offset = new Vector(viewer.Offset.X, newY);
            _smoothScrollLastY[viewer] = newY;
            any = true;
        }

        if (any)
        {
            RequestSmoothScrollFrame();
        }
        else
        {
            _smoothScrollLastFrame = default;
            ResumeBackgroundAfterScroll();
        }
    }
}
