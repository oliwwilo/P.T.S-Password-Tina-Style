using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class PassCardRed : UserControl
{
    public event EventHandler<PasswordEntry>? PasswordSaved;
    public event EventHandler? Cancelled;
    private PasswordEntry? _editingPassword;

    public PassCardRed() => InitializeComponent();

    public string Title => TitleBox?.Text?.Trim() ?? "";
    public string Email => EmailBox?.Text?.Trim() ?? "";
    public string Login => LoginBox?.Text?.Trim() ?? "";
    public string Password => PasswordBox?.Text?.Trim() ?? "";
    public string Description => DescriptionBox?.Text?.Trim() ?? ""; // 

    public void ClearFields()
    {
        TitleBox.Text = "";
        EmailBox.Text = "";
        LoginBox.Text = "";
        PasswordBox.Text = "";
        DescriptionBox.Text = "";
        _editingPassword = null;
    }

    private void OnGeneratePasswordClick(object? sender, RoutedEventArgs e)
    {
        string password = GeneratePassword(12);
        PasswordBox.Text = password;
        ShowFlashMessage($"{FluentIcons.CheckMark} Пароль сгенерирован!");
    }

    private async void OnCopyEmailClick(object? sender, RoutedEventArgs e)
    {
        string email = EmailBox?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(email))
        {
            ShowFlashMessage($"{FluentIcons.Warning} Нечего копировать - почта пуста.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard == null)
        {
            ShowFlashMessage($"{FluentIcons.Warning} Буфер обмена недоступен.");
            return;
        }

        await topLevel.Clipboard.SetTextAsync(email);
        ShowFlashMessage($"{FluentIcons.CheckMark} Почта скопирована в буфер!");
    }

    private async void OnCopyLoginClick(object? sender, RoutedEventArgs e)
    {
        string login = LoginBox?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(login))
        {
            ShowFlashMessage($"{FluentIcons.Warning} Нечего копировать - логин пуст.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard == null)
        {
            ShowFlashMessage($"{FluentIcons.Warning} Буфер обмена недоступен.");
            return;
        }

        await topLevel.Clipboard.SetTextAsync(login);
        ShowFlashMessage($"{FluentIcons.CheckMark} Логин скопирован в буфер!");
    }

    private async void OnCopyPasswordClick(object? sender, RoutedEventArgs e)
    {
        string password = PasswordBox?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(password))
        {
            ShowFlashMessage($"{FluentIcons.Warning} Нечего копировать - пароль пуст.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard == null)
        {
            ShowFlashMessage($"{FluentIcons.Warning} Буфер обмена недоступен.");
            return;
        }

        await topLevel.Clipboard.SetTextAsync(password);
        ShowFlashMessage($"{FluentIcons.CheckMark} Пароль скопирован в буфер!");
    }

    private string GeneratePassword(int length)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_-+=<>?";

        string all = upper + lower + digits + special;
        var random = new Random();
        var password = new StringBuilder();

        password.Append(upper[random.Next(upper.Length)]);
        password.Append(lower[random.Next(lower.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(special[random.Next(special.Length)]);

        for (int i = 4; i < length; i++)
        {
            password.Append(all[random.Next(all.Length)]);
        }

        return ShuffleString(password.ToString());
    }

    private string ShuffleString(string input)
    {
        char[] array = input.ToCharArray();
        var random = new Random();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return new string(array);
    }

    public void SetEditMode(PasswordEntry? password)
    {
        _editingPassword = password;

        if (password != null)
        {
            TitleText.Text = "Редактировать пароль";
            TitleBox.Text = password.Title ?? "";
            EmailBox.Text = password.Email ?? "";
            LoginBox.Text = password.Login ?? "";
            PasswordBox.Text = password.Password ?? "";
            DescriptionBox.Text = password.Description ?? "";
        }
        else
        {
            TitleText.Text = "Новый пароль";
            TitleBox.Text = "";
            EmailBox.Text = "";
            LoginBox.Text = "";
            PasswordBox.Text = "";
            DescriptionBox.Text = "";
        }
    
        // Сброс цвета фона (обновляется через динамическую кисть темы)
    }
    public void Show() => IsVisible = true;
    public void Hide() => PanelFader.FadeOut(this);

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        string title = TitleBox?.Text?.Trim() ?? "";
        string email = EmailBox?.Text?.Trim() ?? "";
        string login = LoginBox?.Text?.Trim() ?? "";
        string password = PasswordBox?.Text?.Trim() ?? "";
        string description = DescriptionBox?.Text?.Trim() ?? ""; // 

        if (string.IsNullOrWhiteSpace(title))
        {
            TitleBox.Background = Brush.Parse("#33FF4444");
            ShowError($"{FluentIcons.Warning} Введите название!");
            return;
        }

        var passwordEntry = new PasswordEntry
        {
            Title = title,
            Email = email,
            Login = LoginBox?.Text?.Trim() ?? "",
            Password = PasswordBox?.Text?.Trim() ?? "",
            Description = DescriptionBox?.Text?.Trim() ?? ""
        };

        PasswordSaved?.Invoke(this, passwordEntry);
        Hide();

        var mainWindow = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        (mainWindow?.MainWindow as MainWindow)?.ShowSuccessFlash($"{FluentIcons.CheckMark} Пароль \"{title}\" сохранен!");
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e) => OnCancelClick(sender, new RoutedEventArgs());

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
        Hide();
    }

    private async void ShowError(string message)
    {
        var parent = this.Parent as Panel;
        if (parent == null) return;

        var flashCanvas = new Canvas
        {
            Background = null,
            Width = this.Bounds.Width,
            Height = this.Bounds.Height,
            IsHitTestVisible = false
        };

        var flashBorder = new Border
        {
            Background = Brush.Parse("#FF4444"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20, 10, 20, 10),
            Opacity = 0
        };

        var label = FluentIcons.Build(message, 14, Brushes.White, TextWrapping.Wrap);
        label.FontWeight = FontWeight.SemiBold;
        flashBorder.Child = label;

        flashBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var left = (this.Bounds.Width - flashBorder.DesiredSize.Width) / 2;
        var top = this.Bounds.Height - 80;

        Canvas.SetLeft(flashBorder, left);
        Canvas.SetTop(flashBorder, top);
        flashCanvas.Children.Add(flashBorder);

        parent.Children.Add(flashCanvas);

        for (int i = 0; i <= 10; i++)
        {
            flashBorder.Opacity = i * 0.1;
            await Task.Delay(10);
        }

        await Task.Delay(1500);

        for (int i = 10; i >= 0; i--)
        {
            flashBorder.Opacity = i * 0.1;
            await Task.Delay(10);
        }

        parent.Children.Remove(flashCanvas);
    }

    private async void ShowFlashMessage(string message)
    {
        var parent = this.Parent as Panel;
        if (parent == null) return;

        var flashCanvas = new Canvas
        {
            Background = null,
            Width = this.Bounds.Width,
            Height = this.Bounds.Height,
            IsHitTestVisible = false
        };

        var flashBorder = new Border
        {
            Background = Brush.Parse("#4CAF50"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20, 10, 20, 10),
            Opacity = 0
        };

        var label = FluentIcons.Build(message, 14, Brushes.White, TextWrapping.Wrap);
        label.FontWeight = FontWeight.SemiBold;
        flashBorder.Child = label;

        flashBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var left = (this.Bounds.Width - flashBorder.DesiredSize.Width) / 2;
        var top = this.Bounds.Height - 80;

        Canvas.SetLeft(flashBorder, left);
        Canvas.SetTop(flashBorder, top);
        flashCanvas.Children.Add(flashBorder);

        parent.Children.Add(flashCanvas);

        for (int i = 0; i <= 10; i++)
        {
            flashBorder.Opacity = i * 0.1;
            await Task.Delay(10);
        }

        await Task.Delay(1500);

        for (int i = 10; i >= 0; i--)
        {
            flashBorder.Opacity = i * 0.1;
            await Task.Delay(10);
        }

        parent.Children.Remove(flashCanvas);
    }
}