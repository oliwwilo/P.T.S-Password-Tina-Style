using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace TinaPASS.Main;

public partial class ImportOverlay : UserControl
{
    public event EventHandler? ContinueRequested;

    public ImportOverlay()
    {
        InitializeComponent();
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        ContinueRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        HideOverlay();
    }

    public void HideOverlay()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow mainWindow)
            {
                var overlay = mainWindow.FindControl<Border>("ImportOverlay");
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
            Console.WriteLine($" Ошибка при закрытии окна импорта: {ex.Message}");
        }
    }
}
