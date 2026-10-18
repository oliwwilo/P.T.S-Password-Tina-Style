using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class LoginOverlay : UserControl
{
    public event EventHandler<bool>? LoginCompleted;
    public event EventHandler<RoutedEventArgs>? RegisterRequested;
    public event EventHandler? GuestRequested;

    public LoginOverlay()
    {
        InitializeComponent();
    }

    public void Show()
    {
        EmailBox.Text = "";
        PasswordBox.Text = "";
        ErrorMessageText.IsVisible = false;
        LoadingSpinner.IsVisible = false;
    }

    public void Hide()
    {
        PanelFader.FadeOut(this);
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        RegisterRequested?.Invoke(this, e);
        Hide();
    }

    private void OnGuestClick(object? sender, RoutedEventArgs e)
    {
        GuestRequested?.Invoke(this, EventArgs.Empty);
        Hide();
    }

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        string email = EmailBox.Text?.Trim() ?? "";
        string password = PasswordBox.Text?.Trim() ?? "";

        ErrorMessageText.IsVisible = false;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            await ShowError("Введите корректный email!");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await ShowError("Введите пароль!");
            return;
        }

        LoginCompleted?.Invoke(this, true);
        Hide();
    }

    private async Task ShowError(string message)
    {
        FluentIcons.Set(ErrorMessageText, $"{FluentIcons.Warning} {message}");
        ErrorMessageText.IsVisible = true;
        await Task.Delay(4000);
        ErrorMessageText.IsVisible = false;
    }
}