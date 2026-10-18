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
    private void OnSmoothPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var viewer = FindSmoothScrollViewer(e.Source as Visual);
        if (viewer == null) return;

        double maxY = viewer.ScrollBarMaximum.Y;
        if (maxY <= 0) return;

        if (_smoothScrollTargets.TryGetValue(viewer, out var target))
            target += -e.Delta.Y * 50.0;
        else
            target = viewer.Offset.Y - e.Delta.Y * 50.0;

        _smoothScrollTargets[viewer] = Math.Max(0, Math.Min(target, maxY));
        _smoothScrollLastY[viewer] = viewer.Offset.Y;
        e.Handled = true;

        EnsureSmoothScrollTimer();
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

    private void EnsureSmoothScrollTimer()
    {
        if (_smoothScrollTimer == null)
        {
            _smoothScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(8) };
            _smoothScrollTimer.Tick += OnSmoothScrollTick;
            _smoothScrollTimer.Start();
        }
    }

    private void OnSmoothScrollTick(object? sender, EventArgs e)
    {
        bool any = false;
        foreach (var viewer in _smoothScrollTargets.Keys.ToList())
        {
            if (viewer == null || viewer.IsVisible == false)
            {
                _smoothScrollTargets.Remove(viewer);
                continue;
            }

            double target = _smoothScrollTargets[viewer];
            double curY = viewer.Offset.Y;

            if (_smoothScrollLastY.TryGetValue(viewer, out var lastY) && Math.Abs(curY - lastY) > 2.0)
                target = curY;

            double diff = target - curY;
            if (Math.Abs(diff) < 0.5)
            {
                _smoothScrollTargets.Remove(viewer);
                continue;
            }

            curY += diff * 0.2;
            viewer.Offset = new Vector(viewer.Offset.X, curY);
            _smoothScrollLastY[viewer] = curY;
            any = true;
        }

        if (!any)
        {
            _smoothScrollTimer.Stop();
            _smoothScrollTimer.Tick -= OnSmoothScrollTick;
            _smoothScrollTimer = null;
        }
    }

}
