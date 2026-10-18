using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class RegisterOverlay : UserControl
{
    public event EventHandler? BackRequested;
    public event EventHandler<RegisterData>? RegisterCompleted;
    public event EventHandler<string>? VerificationSent;

    public RegisterOverlay()
    {
        InitializeComponent();
    }

    public void Show()
    {
        IsVisible = true;
        EmailBox.Text = "";
        PasswordBox.Text = "";
        ConfirmPasswordBox.Text = "";
        ErrorMessageText.IsVisible = false;
        SuccessMessageText.IsVisible = false;
    }

    public void Hide()
    {
        PanelFader.FadeOut(this);
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        Hide();
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        string email = EmailBox.Text?.Trim() ?? "";
        string password = PasswordBox.Text?.Trim() ?? "";
        string confirm = ConfirmPasswordBox.Text?.Trim() ?? "";

        ErrorMessageText.IsVisible = false;
        SuccessMessageText.IsVisible = false;

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

        if (password != confirm)
        {
            await ShowError("Пароли не совпадают!");
            return;
        }

        if (password.Length < 6)
        {
            await ShowError("Пароль должен быть не менее 6 символов!");
            return;
        }

        RegisterButton.IsEnabled = false;
        RegisterButton.Content = "Регистрация...";

        try
        {
            FluentIcons.Set(SuccessMessageText, $"{FluentIcons.CheckMark} Регистрация завершена!");
            SuccessMessageText.IsVisible = true;
            SuccessMessageText.Foreground = Brush.Parse("#4CAF50");

            await Task.Delay(1200);

            RegisterCompleted?.Invoke(this, new RegisterData
            {
                Email = email,
                Password = password,
                NeedVerification = false
            });
            Hide();
        }
        catch (Exception ex)
        {
            await ShowError(ex.Message);
        }
        finally
        {
            RegisterButton.IsEnabled = true;
            RegisterButton.Content = "Зарегистрироваться";
        }
    }

    private async Task ShowError(string message)
    {
        FluentIcons.Set(ErrorMessageText, $"{FluentIcons.Warning} {message}");
        ErrorMessageText.IsVisible = true;
        await Task.Delay(3000);
        ErrorMessageText.IsVisible = false;
    }
}

public class RegisterData
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool NeedVerification { get; set; } = false;
}