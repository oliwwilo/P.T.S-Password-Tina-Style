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
    // ============ ОКНО ВХОДА ============
    private async void ShowLogin()
    {
        Debug.WriteLine(" ПОКАЗЫВАЕМ ОКНО ВХОДА");
        MainContent.IsEnabled = false;
        allPasswords.Clear();
        groupImages.Clear();
        currentPasswords.Clear();
        currentGroup = "";
        GroupsPanel.Children.Clear();
        if (GroupNameEditor != null) GroupNameEditor.Text = "";
        if (DeleteGroupButton != null) DeleteGroupButton.IsVisible = false;
        if (GroupIcon != null) GroupIcon.IsVisible = false;
        if (GroupIconInitial != null) GroupIconInitial.IsVisible = false;
        Overlay.IsVisible = true;
        LoginOverlayControl.Show();
        FadeInPanel(Overlay);
        PopInPanel(LoginOverlayControl);
    }

    private void ShowRegisterOverlay() => RegisterOverlayControl.Show();

    private void OnRegisterRequested(object? sender, RoutedEventArgs e)
    {
        LoginOverlayControl.Hide();
        Overlay.IsVisible = true;
        ShowRegisterOverlay();
        PopInPanel(RegisterOverlayControl);
    }

    private void OnBackToLogin(object? sender, EventArgs e)
    {
        RegisterOverlayControl.Hide();
        Overlay.IsVisible = true;
        LoginOverlayControl.Show();
        PopInPanel(LoginOverlayControl);
    }

}
