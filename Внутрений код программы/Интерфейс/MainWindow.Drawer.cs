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
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace TinaPASS.Main;

public partial class MainWindow : Window
{
    // ============ DRAWER (боковое меню) ============
    private void OnDrawerLoginRequested(object? sender, EventArgs e)
    {
        // Плавно закрываем меню, но окно входа показываем сразу (без ожидания)
        _ = CloseDrawer();
        ShowLogin();
    }

    private void OnDrawerSaveModeChanged(object? sender, bool isCloudMode)
    {
        ShowSuccessFlash(isCloudMode ? $"{FluentIcons.Cloud} Облачный режим" : $"{FluentIcons.Save} Локальный режим");
    }

    private async void OnDrawerAvatarClick(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите аватарку",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Изображения")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.ico", "*.webp", "*.tga", "*.tif", "*.tiff" }
                }
            }
        });

        if (files.Count > 0)
        {
            DrawerPanelControl.SetAvatar(files[0].Path.LocalPath);
            ShowSuccessFlash($"{FluentIcons.CheckMark} Аватарка обновлена!");
        }

        await CloseDrawer();
    }

    private const double DrawerBlurRadius = 20.0;
    private const double DrawerOpenHeight = 400;
    private const double DrawerAnimMs = 250;

    private async void OnMenuButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!_isDrawerOpen) await OpenDrawer();
        else await CloseDrawer();
    }

    private void SetupDrawerAnimation()
    {
        var ease = new CubicEaseOut();
        var duration = TimeSpan.FromMilliseconds(DrawerAnimMs);

        DrawerContainer.Transitions = new Transitions
        {
            new DoubleTransition { Property = Layoutable.HeightProperty, Duration = duration, Easing = ease }
        };

        DrawerBackdrop.Transitions = new Transitions
        {
            new DoubleTransition { Property = Layoutable.HeightProperty, Duration = duration, Easing = ease },
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = duration, Easing = ease }
        };

        DrawerBackdrop.Opacity = 0;
    }

    private async Task OpenDrawer()
    {
        _isDrawerOpen = true;
        DrawerContainer.IsVisible = true;
        DrawerBackdrop.IsVisible = true;
        DrawerClickCatcher.IsVisible = true;
        ApplyMainBlur(DrawerBlurRadius);

        // Даём кадр, чтобы панель была в дереве с нулевой высотой,
        // и только затем меняем значения — Transitions плавно проиграют переход.
        await Task.Yield();

        DrawerContainer.Height = DrawerOpenHeight;
        DrawerBackdrop.Height = DrawerOpenHeight;
        DrawerBackdrop.Opacity = 0.5;
    }

    private async Task CloseDrawer()
    {
        if (!_isDrawerOpen && !DrawerContainer.IsVisible) return;

        DrawerContainer.Height = 0;
        DrawerBackdrop.Height = 0;
        DrawerBackdrop.Opacity = 0;

        await Task.Delay((int)DrawerAnimMs);

        DrawerContainer.IsVisible = false;
        DrawerBackdrop.IsVisible = false;
        DrawerClickCatcher.IsVisible = false;
        _isDrawerOpen = false;
        ApplyMainBlur(0);
    }

    private void ApplyMainBlur(double radius)
    {
        // Один эффект на весь контент: размытие идёт по общей поверхности,
        // поэтому нет жёстких «квадратов» вокруг отдельных элементов.
        MainContent.Effect = radius > 0 ? new BlurEffect { Radius = radius } : null;
    }

    private void OnDrawerClickCatcherPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        _ = CloseDrawer();
    }

    private async void OnArrowPointerEnter(object? sender, PointerEventArgs e)
    {
        for (int i = 0; i <= 10; i++)
        {
            MenuLine.Margin = new Thickness(0, i / 2.0, 0, 0);
            await Task.Delay(15);
        }
    }

    private async void OnArrowPointerLeave(object? sender, PointerEventArgs e)
    {
        for (int i = 10; i >= 0; i--)
        {
            MenuLine.Margin = new Thickness(0, i / 2.0, 0, 0);
            await Task.Delay(15);
        }
    }
}
