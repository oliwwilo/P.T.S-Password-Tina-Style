using Avalonia.Input;
using System;
using Avalonia.Controls;

namespace TinaPASS.Main;

public partial class MainWindow
{
    private void CloseAllPanels()
    {
        FadeOutPanel(CreatePanel);
        FadeOutPanel(SwitshiControl);
        FadeOutPanel(Overlay);
    }
    
    private void ShowCreateGroupPanel()
    {
        CloseAllPanels();
        CreatePanel.Show();
        FadeInPanel(CreatePanel);
        Overlay.IsVisible = false;
    }
    

    private void ShowSwitshiPanel()
    {
        CloseAllPanels();
        FadeInPanel(SwitshiControl);

        Overlay.IsVisible = false;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            //  ЗАКРЫВАЕМ ВСЕ ПАНЕЛИ
            CloseAllPanels();
            ClearSelection();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            //  НЕ ДАЕМ ENTER ВЫЗЫВАТЬ НЕЖЕЛАТЕЛЬНЫЕ ДЕЙСТВИЯ
            if (sender is TextBox)
            {
                e.Handled = true;
            }
        }
    }

    private void OnPanelCancelled(object? sender, EventArgs e) => CloseAllPanels();
    private void OnPasswordPanelCancelled(object? sender, EventArgs e) => CloseAllPanels();
}