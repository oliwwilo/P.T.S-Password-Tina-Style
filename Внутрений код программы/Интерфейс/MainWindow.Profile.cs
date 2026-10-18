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
    // ============ Профиль ============
    private void OnProfileRequested(object? sender, EventArgs e) => OpenProfileWindow();

    private void OpenProfileWindow()
    {
        var profileWindow = new ProfileWindow("", "", _isAuthenticated, () =>
        {
            OnDrawerLogoutRequested(this, EventArgs.Empty);
        });

        profileWindow.ShowDialog(this);
    }

    public void UpdateAvatar(string avatarPath) => DrawerPanelControl.SetAvatar(avatarPath);

    // ============ Аватарка и данные ============
    private string LoadLocalAvatarPath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "TinaPASS", "Avatars");
    
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    
        string path = Path.Combine(dir, "avatar.jpg");
        return File.Exists(path) ? path : "";
    }

}
