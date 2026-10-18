using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace TinaPASS.Main;

public partial class DrawerPanel : UserControl
{
    public event EventHandler? CloseRequested;
    public event EventHandler? LoginRequested;
    public event EventHandler? LogoutRequested;
    public event EventHandler? SyncRequested;
    public event EventHandler<bool>? SaveModeChanged;
    public event EventHandler? AvatarClick;
    public event EventHandler? LicenceRequested;
    public event EventHandler? ProfileRequested;
    

    public DrawerPanel()
    {
        InitializeComponent();
    }

    public void SetLoggedIn(bool isLoggedIn, string? userName = null, string? userEmail = null)
    {
        if (isLoggedIn)
        {
            LoginButton.Content = "Профиль";

            LogoutButton.IsVisible = true;

            LicenceButton.IsVisible = true;

            // Устанавливаем email пользователя
            if (!string.IsNullOrEmpty(userEmail))
            {
                UserNameText.Text = userEmail.Split('@')[0];
                UserEmailText.Text = userEmail;
            }
            else
            {
                UserNameText.Text = "Пользователь";
                UserEmailText.Text = "Авторизован";
            }
        }
        else
        {
            LoginButton.Content = "Войти";

            LogoutButton.IsVisible = false;

            LicenceButton.IsVisible = false;

            // Сбрасываем информацию
            UserNameText.Text = "Не авторизован";
            UserEmailText.Text = "Войдите в аккаунт";
        }
    }

    public void SetAvatar(string? imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                var bitmap = new Bitmap(imagePath);
                AvatarImage.Source = bitmap;
                AvatarImage.IsVisible = true;
                AvatarText.IsVisible = false;
                return;
            }
            catch
            {
            }
        }

        // Если нет аватарки - показываем иконку
        AvatarImage.IsVisible = false;
        AvatarText.IsVisible = true;
        AvatarText.Text = "👤";
    }

    public void SetUserInfo(string? email, string? avatarPath)
    {
        if (!string.IsNullOrEmpty(email))
        {
            UserNameText.Text = email.Split('@')[0];
            UserEmailText.Text = email;
        }

        SetAvatar(avatarPath);
    }
    public event EventHandler? ThemeRequested;

// Добавьте метод
    private void OnThemeClick(object? sender, RoutedEventArgs e)
    {
        // Настройки отключены
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        // Проверяем - если уже авторизован, открываем профиль
        if (LoginButton.Content.ToString() == "Профиль")
        {
            ProfileRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            LoginRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnAvatarClick(object? sender, PointerPressedEventArgs e)
    {
        AvatarClick?.Invoke(this, EventArgs.Empty);
    }

    private void OnLicenceClick(object? sender, RoutedEventArgs e)
    {
        LicenceRequested?.Invoke(this, EventArgs.Empty);
    }

    private const string TelegramUrl = "https://t.me/+XW1PZarXnqFlNWRi";

    private void OnTelegramClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(TelegramUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка открытия Telegram: {ex.Message}");
        }
    }

    private const string BoostyUrl = "https://boosty.to/medicalhouse/donate";

    private const string YouTubeUrl = "https://www.youtube.com/@KLSP-878";

    private void OnYouTubeClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(YouTubeUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка открытия YouTube: {ex.Message}");
        }
    }

    private void OnBoostyClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(BoostyUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка открытия Boosty: {ex.Message}");
        }
    }

    private void OnTelegramEnter(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (TelegramTooltip != null) TelegramTooltip.Opacity = 1;
    }

    private void OnTelegramLeave(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (TelegramTooltip != null) TelegramTooltip.Opacity = 0;
    }

    private void OnYouTubeEnter(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (YouTubeTooltip != null) YouTubeTooltip.Opacity = 1;
    }

    private void OnYouTubeLeave(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (YouTubeTooltip != null) YouTubeTooltip.Opacity = 0;
    }

    private void OnBoostyEnter(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (BoostyTooltip != null) BoostyTooltip.Opacity = 1;
    }

    private void OnBoostyLeave(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (BoostyTooltip != null) BoostyTooltip.Opacity = 0;
    }
}
