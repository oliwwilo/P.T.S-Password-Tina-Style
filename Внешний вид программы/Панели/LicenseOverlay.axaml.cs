using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace TinaPASS.Main;

public partial class LicenseOverlay : UserControl
{
    public LicenseOverlay()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow mainWindow)
            {
                var overlay = mainWindow.FindControl<Border>("LicenseOverlay");
                if (overlay != null)
                {
                    PanelFader.FadeOut(overlay);
                    return;
                }
            }

            IsVisible = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка при закрытии лицензии: {ex.Message}");
        }
    }
}
