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
    public void ApplyThemeToUI(Theme theme)
    {
        try
        {
            Console.WriteLine($" Обновление UI для темы: {theme.Name}");
        
            // Обновляем фон
            this.Background = theme.BackgroundBrush;
        
            // Обновляем GroupIconBorder
            if (GroupIconBorder != null && !GroupIcon.IsVisible)
            {
                GroupIconBorder.Background = theme.SurfaceBrush;
                GroupIconBorder.BorderBrush = theme.BorderBrush;
            }
        
            // Обновляем DrawerContainer
            var drawerContainer = this.FindControl<Border>("DrawerContainer");
            if (drawerContainer != null)
            {
                drawerContainer.Background = theme.SurfaceBrush;
                drawerContainer.BorderBrush = theme.BorderBrush;
            }
        
            // Обновляем заголовок
            var nadpis = this.FindControl<TextBlock>("Nadpis");
            if (nadpis != null) nadpis.Foreground = theme.AccentBrush;
        
            var smenaslo = this.FindControl<TextBlock>("smenaslo");
            if (smenaslo != null) smenaslo.Foreground = theme.AccentBrush;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка обновления UI: {ex.Message}");
        }
    }
    private void StartStarBackground()
    {
        var starCanvas = this.FindControl<Canvas>("StarCanvas");
        if (starCanvas != null)
        {
            // Инициализируем фоновые эффекты
            BackgroundEffectManager.Instance.Initialize(starCanvas);
        
            // Загружаем сохраненный эффект или эффект по умолчанию
            string savedEffect = LoadSavedEffect();
            BackgroundEffectManager.Instance.SetEffect(savedEffect);
        
            // При изменении размера окна перерисовываем эффекты
            this.SizeChanged += (s, e) =>
            {
                BackgroundEffectManager.Instance.Resize(this.Bounds.Width, this.Bounds.Height);
            };

            // Пока окно перемещают по экрану, фон замирает — иначе окно
            // отстаёт от курсора при быстром движении.
            this.PositionChanged += (s, e) => OnWindowMoved();

            // Ловим самое начало перетаскивания (WM_ENTERSIZEMOVE), чтобы
            // поставить фон на паузу до первого сдвига окна. Иначе при
            // резком рывке один тяжёлый кадр успевает уйти в отрисовку
            // и окно дёргается в самый первый момент.
            this.Opened += (s, e) => AttachMoveHooks();

            // Ставим анимацию фона на паузу только когда окно свёрнуто или скрыто
            // (при обычной потере фокуса анимация продолжается) — экономит CPU.
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == WindowStateProperty || e.Property == IsVisibleProperty)
                    UpdateBackgroundAnimation();
            };
        }
    }

    private bool _backgroundPaused;
    private bool _backgroundPausedForScroll;
    private bool _backgroundPausedForMove;
    private DispatcherTimer? _moveSettleTimer;

    private bool BackgroundShouldRun =>
        IsVisible
        && WindowState != WindowState.Minimized
        && !_backgroundPausedForScroll
        && !_backgroundPausedForMove;

    private void UpdateBackgroundAnimation()
    {
        bool shouldRun = BackgroundShouldRun;

        if (shouldRun && _backgroundPaused)
        {
            BackgroundEffectManager.Instance.Resume();
            _backgroundPaused = false;
        }
        else if (!shouldRun && !_backgroundPaused)
        {
            BackgroundEffectManager.Instance.Pause();
            _backgroundPaused = true;
        }
    }

    // Пока прокручиваем списки, фон замирает — так прокрутка получает больше кадров.
    private void PauseBackgroundForScroll()
    {
        if (_backgroundPausedForScroll) return;

        _backgroundPausedForScroll = true;
        UpdateBackgroundAnimation();
    }

    private void ResumeBackgroundAfterScroll()
    {
        if (!_backgroundPausedForScroll) return;

        _backgroundPausedForScroll = false;
        UpdateBackgroundAnimation();
    }

    // Пока окно тащат по экрану, фон тоже замирает: перерисовка тяжёлого
    // градиента на UI-потоке не даёт окну успевать за курсором.
    private void PauseBackgroundForMove()
    {
        if (_backgroundPausedForMove) return;

        _backgroundPausedForMove = true;
        UpdateBackgroundAnimation();
    }

    private void ResumeBackgroundAfterMove()
    {
        if (_moveSettleTimer == null)
        {
            _moveSettleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
            _moveSettleTimer.Tick += (s, e) =>
            {
                _moveSettleTimer!.Stop();
                _backgroundPausedForMove = false;
                UpdateBackgroundAnimation();
            };
        }

        _moveSettleTimer.Stop();
        _moveSettleTimer.Start();
    }

    private void OnWindowMoved()
    {
        PauseBackgroundForMove();
        ResumeBackgroundAfterMove();
    }

    // Хук WndProc: точное начало/конец системного перетаскивания окна.
    private Win32Properties.CustomWndProcHookCallback? _moveWndProcHook;

    private void AttachMoveHooks()
    {
        if (!OperatingSystem.IsWindows() || _moveWndProcHook != null) return;

        const uint WM_ENTERSIZEMOVE = 0x0231;
        const uint WM_EXITSIZEMOVE = 0x0232;

        _moveWndProcHook = (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            if (msg == WM_ENTERSIZEMOVE)
            {
                PauseBackgroundForMove();
            }
            else if (msg == WM_EXITSIZEMOVE)
            {
                ResumeBackgroundAfterMove();
            }

            return IntPtr.Zero;
        };

        try
        {
            Win32Properties.AddWndProcHookCallback(this, _moveWndProcHook);
        }
        catch
        {
            // Не на Windows или платформа без Win32 — остаётся пауза по PositionChanged.
            _moveWndProcHook = null;
        }
    }

    
    private string LoadSavedEffect()
    {
        try
        {
            string appDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TinaPASS");
        
            string effectFile = System.IO.Path.Combine(appDataPath, "effect.txt");
        
            if (System.IO.File.Exists(effectFile))
            {
                string effectName = System.IO.File.ReadAllText(effectFile);
                if (BackgroundEffectManager.Instance.EffectTypes.ContainsKey(effectName))
                    return effectName;
            }
        }
        catch { }
        return "Яндекс музыка";
    }

    public void SaveEffect(string effectName)
    {
        try
        {
            string appDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TinaPASS");
        
            if (!System.IO.Directory.Exists(appDataPath))
                System.IO.Directory.CreateDirectory(appDataPath);
        
            string effectFile = System.IO.Path.Combine(appDataPath, "effect.txt");
            System.IO.File.WriteAllText(effectFile, effectName);
        }
        catch { }
    }
    private async void OnThemeRequested(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine(" Открытие панели выбора темы");

            await CloseDrawer();

            var themeSelector = this.FindControl<ThemeSelector>("ThemeSelectorControl");
            if (themeSelector != null)
            {
                themeSelector.IsVisible = true;
                PopInPanel(themeSelector);
            }

            var overlay = this.FindControl<Border>("ThemeOverlay");
            if (overlay != null)
            {
                FadeInPanel(overlay);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка открытия темы: {ex.Message}");
        }
    }

    private async void OnLicenceRequested(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine(" Открытие панели лицензии");

            await CloseDrawer();

            var overlay = this.FindControl<Border>("LicenseOverlay");
            if (overlay != null)
            {
                FadeInPanel(overlay);
            }

            var license = this.FindControl<LicenseOverlay>("LicenseOverlayControl");
            if (license != null)
            {
                license.IsVisible = true;
                PopInPanel(license);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка открытия лицензии: {ex.Message}");
        }
    }

    // Клик по фону оверлея темы — закрываем настройки
    private void OnThemeOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source == sender && sender is Border overlay)
            PanelFader.FadeOut(overlay);
    }

    // Клик по фону оверлея лицензии — закрываем соглашение
    private void OnLicenseOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source == sender && sender is Border overlay)
            PanelFader.FadeOut(overlay);
    }
}
