using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace TinaPASS.Main;

public partial class ThemeSelector : UserControl
{
    // Выбранный (ещё не применённый) / применённый фоновый эффект
    private string _pendingEffect = "Океан";
    private string _appliedEffect = "Океан";

    public ThemeSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            // Текущий (применённый) эффект — с него начинаем предпросмотр
            _appliedEffect = BackgroundEffectManager.Instance.GetCurrentEffectName();
            _pendingEffect = _appliedEffect;

            UpdateEffectChecks();
            UpdatePreview();
            MeasureApplyHeight();
            UpdateApplyVisible();

            Console.WriteLine($" Текущий эффект: {_appliedEffect}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
        }
    }

    private void OnEffectClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is string effectName)
            {
                Console.WriteLine($"Смена эффекта на: {effectName}");

                // Только предпросмотр — к приложению применяется по кнопке "Применить"
                _pendingEffect = effectName;
                UpdateEffectChecks();
                UpdatePreview();
                UpdateApplyVisible();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при смене эффекта: {ex.Message}");
            ShowNotification($"{FluentIcons.Warning} Ошибка: {ex.Message}");
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        // Отмена — откатываем предпросмотр к применённым настройкам
        _pendingEffect = _appliedEffect;
        UpdateApplyVisible();
        CloseSelector();
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            BackgroundEffectManager.Instance.SetEffect(_pendingEffect);
            _appliedEffect = _pendingEffect;
            UpdateApplyVisible();

            ShowNotification($"{FluentIcons.CheckMark} Настройки применены!");
            Console.WriteLine($" Применено: эффект={_appliedEffect}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка применения настроек: {ex.Message}");
            ShowNotification($"{FluentIcons.Warning} Ошибка: {ex.Message}");
        }
    }

    private void CloseSelector()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow mainWindow)
            {
                var overlay = mainWindow.FindControl<Border>("ThemeOverlay");
                if (overlay != null)
                {
                    PanelFader.FadeOut(overlay);
                    return;
                }
            }

            this.IsVisible = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка при закрытии: {ex.Message}");
        }
    }

    private void ShowNotification(string message)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ShowSuccessFlash(message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка уведомления: {ex.Message}");
        }
    }

    private void UpdateEffectChecks()
    {
        try
        {
            // Скрываем все галочки
            if (WaveOceanCheck != null) WaveOceanCheck.IsVisible = false;
            if (WaveSunsetCheck != null) WaveSunsetCheck.IsVisible = false;
            if (WaveNeonCheck != null) WaveNeonCheck.IsVisible = false;
            if (WaveMoonCheck != null) WaveMoonCheck.IsVisible = false;
            if (WaveSteppeCheck != null) WaveSteppeCheck.IsVisible = false;
            if (GradientCheck != null) GradientCheck.IsVisible = false;
            if (Gradient2Check != null) Gradient2Check.IsVisible = false;
            if (WaveFigmaCheck != null) WaveFigmaCheck.IsVisible = false;

            // Показываем нужную
            switch (_pendingEffect)
            {
                case "Океан": if (WaveOceanCheck != null) WaveOceanCheck.IsVisible = true; break;
                case "Закат": if (WaveSunsetCheck != null) WaveSunsetCheck.IsVisible = true; break;
                case "Неоновые волны": if (WaveNeonCheck != null) WaveNeonCheck.IsVisible = true; break;
                case "Лунная ночь": if (WaveMoonCheck != null) WaveMoonCheck.IsVisible = true; break;
                case "Степь": if (WaveSteppeCheck != null) WaveSteppeCheck.IsVisible = true; break;
                case "Градиент": if (GradientCheck != null) GradientCheck.IsVisible = true; break;
                case "Градиент 2": if (Gradient2Check != null) Gradient2Check.IsVisible = true; break;
                case "Figma (неон)": if (WaveFigmaCheck != null) WaveFigmaCheck.IsVisible = true; break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления галочек эффекта: {ex.Message}");
        }
    }

    private void MeasureApplyHeight()
    {
        try
        {
            ApplyBtn.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double h = ApplyBtn.DesiredSize.Height + 10;
            if (h > 20) _applyHeight = h;
        }
        catch { }
    }

    // Кнопка "Применить" плавно появляется только когда выбор отличается от применённого
    private double _applyHeight = 54;

    private void UpdateApplyVisible()
    {
        if (ApplyWrapper == null) return;

        if (_pendingEffect != _appliedEffect)
        {
            ApplyWrapper.IsHitTestVisible = true;
            ApplyWrapper.Opacity = 1;
            ApplyWrapper.Height = _applyHeight;
        }
        else
        {
            ApplyWrapper.IsHitTestVisible = false;
            ApplyWrapper.Opacity = 0;
            ApplyWrapper.Height = 0;
        }
    }

    // ============ ПРЕДПРОСМОТР ============

    private void UpdatePreview()
    {
        try
        {
            // Фон — упрощённый образец выбранного фонового эффекта
            if (PreviewBackground != null)
                PreviewBackground.Background = GetEffectPreviewBrush(_pendingEffect);

            // Оформление — цвета фиксированной (текущей) темы
            var theme = ThemeManager.Instance.CurrentTheme;
            if (theme != null)
            {
                if (PreviewSidebar != null)
                    PreviewSidebar.Background = WithAlpha(theme.Surface, 0xD0);
                if (PreviewTitleBar != null)
                    PreviewTitleBar.Background = WithAlpha(theme.Surface, 0x99);
                if (PreviewGroupActive != null)
                    PreviewGroupActive.Background = theme.PrimaryBrush;

                var cardBrush = WithAlpha(theme.SurfaceLight, 0xD8);
                if (PreviewCard1 != null) PreviewCard1.Background = cardBrush;
                if (PreviewCard2 != null) PreviewCard2.Background = cardBrush;
                if (PreviewCard3 != null) PreviewCard3.Background = cardBrush;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления предпросмотра: {ex.Message}");
        }
    }

    private static SolidColorBrush WithAlpha(Color color, byte alpha)
        => new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));

    private static IBrush GetEffectPreviewBrush(string effect)
    {
        // Упрощённые градиенты-образцы для фоновых эффектов
        Color[] colors = effect switch
        {
            "Океан" => new[] { Color.FromRgb(0x0A,0x19,0x2D), Color.FromRgb(0x12,0x3A,0x5E), Color.FromRgb(0x1E,0x60,0x91) },
            "Закат" => new[] { Color.FromRgb(0x1A,0x0B,0x2E), Color.FromRgb(0x7B,0x2D,0x8E), Color.FromRgb(0xFF,0x6B,0x4A) },
            "Неоновые волны" => new[] { Color.FromRgb(0x00,0x10,0x18), Color.FromRgb(0x00,0x3A,0x4D), Color.FromRgb(0x00,0xE5,0xFF) },
            "Лунная ночь" => new[] { Color.FromRgb(0x0B,0x10,0x26), Color.FromRgb(0x1B,0x2A,0x4A), Color.FromRgb(0xB0,0xC4,0xDE) },
            "Степь" => new[] { Color.FromRgb(0x1C,0x2A,0x18), Color.FromRgb(0x3E,0x5B,0x32), Color.FromRgb(0x66,0xBB,0x6A) },
            "Градиент" => new[] { Color.FromRgb(0x0F,0x20,0x27), Color.FromRgb(0x20,0x3A,0x43), Color.FromRgb(0x2C,0x53,0x64) },
            "Градиент 2" => new[] { Color.FromRgb(0x0B,0x0F,0x1A), Color.FromRgb(0x1B,0x2A,0x4A), Color.FromRgb(0x6F,0xBF,0xFF) },
            "Figma (неон)" => new[] { Color.FromRgb(0x1A,0x0A,0x14), Color.FromRgb(0xFE,0x01,0xA4), Color.FromRgb(0x69,0xDB,0xFE) },
            "Яндекс музыка" => new[] { Color.FromRgb(0x00,0x00,0x00), Color.FromRgb(0x18,0x18,0x18), Color.FromRgb(0x1D,0xB9,0x54) },
            _ => new[] { Color.FromRgb(0x10,0x14,0x1A), Color.FromRgb(0x22,0x2A,0x36), Color.FromRgb(0x33,0x3D,0x4D) }
        };

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(colors[0], 0));
        brush.GradientStops.Add(new GradientStop(colors[1], 0.5));
        brush.GradientStops.Add(new GradientStop(colors[2], 1));
        return brush;
    }
}
