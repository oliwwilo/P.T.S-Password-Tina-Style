using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace TinaPASS.Main;

public partial class App : Application
{
    public static Theme CurrentTheme { get; private set; } = new Theme();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            
            // Применяем тему по умолчанию
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplyTheme(Theme theme)
    {
        try
        {
            CurrentTheme = theme;
        
            Console.WriteLine($" Применение темы: {theme.Name}");
        
            // Обновляем ресурсы
            Resources["BackgroundBrush"] = theme.BackgroundBrush;
            Resources["SurfaceBrush"] = theme.SurfaceBrush;
            Resources["SurfaceLightBrush"] = theme.SurfaceLightBrush;
            Resources["PrimaryBrush"] = theme.PrimaryBrush;
            Resources["PrimaryDarkBrush"] = theme.PrimaryDarkBrush;
            Resources["TextBrush"] = theme.TextBrush;
            Resources["TextSecondaryBrush"] = theme.TextSecondaryBrush;
            Resources["AccentBrush"] = theme.AccentBrush;
            Resources["SuccessBrush"] = theme.SuccessBrush;
            Resources["BorderBrush"] = theme.BorderBrush;
        
            // Обновляем все окна
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var window in desktop.Windows)
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ApplyThemeToUI(theme);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка применения темы: {ex.Message}");
        }
    }
}