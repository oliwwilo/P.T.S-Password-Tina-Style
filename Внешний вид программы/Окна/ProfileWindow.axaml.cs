using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class ProfileWindow : Window
{
    private string _userEmail;
    private string _userId;
    private Action _onLogout;

    public ProfileWindow(string email, string userId, bool isAuthenticated, Action onLogout)
    {
        InitializeComponent();

        _userEmail = email;
        _userId = userId;
        _onLogout = onLogout;

        LoadData();
        LoadAvatar();
    }

    private void LoadData()
    {
        EmailText.Text = _userEmail ?? "Не указан";
        IdText.Text = _userId ?? "Не указан";
        SyncModeText.Text = "Локальный";
    }

    private void LoadAvatar()
    {
        string avatarPath = GetLocalAvatarPath();
        if (!string.IsNullOrEmpty(avatarPath) && File.Exists(avatarPath))
        {
            try
            {
                if (AvatarImage.Source is Bitmap oldBitmap)
                {
                    AvatarImage.Source = null;
                    oldBitmap.Dispose();
                }
                var bitmap = new Bitmap(avatarPath);
                AvatarImage.Source = bitmap;
                AvatarImage.IsVisible = true;
                AvatarText.IsVisible = false;
            }
            catch
            {
            }
        }
    }

    private string GetLocalAvatarPath()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinaPASS",
            "Avatars");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "avatar.jpg");
        return File.Exists(path) ? path : "";
    }

    // Копирование email в буфер обмена
    private async void OnEmailCopyClick(object? sender, PointerPressedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_userEmail))
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await topLevel.Clipboard.SetTextAsync(_userEmail);

                    // Отправляем сообщение в главное окно
                    var mainWindow =
                        (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.
                            IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;
                    mainWindow?.ShowSuccessFlash($"{FluentIcons.Copy} Email скопирован!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка копирования: {ex.Message}");
            }
        }
    }

    // Простое уведомление
    private async void ShowToast(string message)
    {
        var toast = new TextBlock
        {
            FontSize = 13,
            Foreground = Brush.Parse("#4CAF50"),
            Background = Brush.Parse("#2D2D2D"),
            Padding = new Thickness(15, 8, 15, 8),
        };
        FluentIcons.Set(toast, message);

        var parent = this.Content as Panel;
        if (parent != null)
        {
            toast.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            toast.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 0, 30);

            parent.Children.Add(toast);

            await Task.Delay(2000);
            parent.Children.Remove(toast);
        }
    }

    private async void OnAvatarClick(object? sender, PointerPressedEventArgs e)
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
            string destPath = SaveAvatar(files[0].Path.LocalPath);
            LoadAvatar();

            var mainWindow =
                (Application.Current?.ApplicationLifetime as
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow as MainWindow;
            mainWindow?.UpdateAvatar(destPath);

            ShowToast($"{FluentIcons.CheckMark} Аватарка обновлена!");
        }
    }

    private string SaveAvatar(string sourcePath)
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinaPASS",
            "Avatars");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string destPath = Path.Combine(dir, "avatar.jpg");
        File.Copy(sourcePath, destPath, true);
        return destPath;
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        _onLogout?.Invoke();
        Close();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}