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
    // ============ ИМПОРТ ============
    private void ShowImportProgress(string message = "Импорт...")
    {
        if (_importOverlay == null)
        {
            _importOverlay = new Border
            {
                Background = Brush.Parse("#80000000"),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Padding = new Thickness(30, 20, 30, 20),
                Width = 400,
                Height = 220
            };

            var stack = new StackPanel { Spacing = 10 };
            var title = new TextBlock { Text = message, Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeight.SemiBold };
            _importProgress = new ProgressBar { Width = 340, Height = 20, IsIndeterminate = false, Minimum = 0, Maximum = 100, Value = 0 };
            _importPercentText = new TextBlock { Text = "0%", Foreground = Brushes.White, FontSize = 14, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            _importLogText = new TextBlock { Text = "", Foreground = Brush.Parse("#CCCCCC"), FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxHeight = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            _importSpeedText = new TextBlock
            {
                Text = "Скорость: 0 записей/сек",
                Foreground = Brush.Parse("#AAAAAA"),
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            stack.Children.Add(_importProgress);
            stack.Children.Add(_importPercentText);
            stack.Children.Add(_importSpeedText);   // < новое
            stack.Children.Add(_importLogText);
            _importOverlay.Child = stack;
        }
        else
        {
            _importOverlay.IsVisible = true;
            if (_importProgress != null) _importProgress.Value = 0;
            if (_importPercentText != null) _importPercentText.Text = "0%";
            if (_importLogText != null) _importLogText.Text = "";
        }
       
        
        var parent = this.Content as Panel;
        if (parent != null && !parent.Children.Contains(_importOverlay))
            parent.Children.Add(_importOverlay);
        _importOverlay.IsVisible = true;
    }
    private void HideImportProgress()
    {
        if (_importOverlay != null)
            _importOverlay.IsVisible = false;
    }
    private void UpdateImportProgress(int percent, string? logMessage = null, double? speed = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_importProgress != null) _importProgress.Value = percent;
            if (_importPercentText != null) _importPercentText.Text = $"{percent}%";
            if (_importSpeedText != null && speed.HasValue)
                _importSpeedText.Text = $"Скорость: {speed.Value:F1} записей/сек";
            if (_importLogText != null && logMessage != null)
            {
                _importLogText.Text = logMessage + "\n" + _importLogText.Text;
                if (_importLogText.Text.Split('\n').Length > 10)
                    _importLogText.Text = string.Join("\n", _importLogText.Text.Split('\n').Take(10));
            }
        });
    }
private void OnImportClick(object? sender, RoutedEventArgs e)
{
    Console.WriteLine(" Нажата кнопка импорта");

    if (currentGroup != "Без группы")
    {
        ShowError($"{FluentIcons.Warning} Импорт доступен только в группе 'Без группы'!");
        return;
    }

    ShowImportConditions();
}

// ============ ОКНО УСЛОВИЙ ИМПОРТА ============
private void ShowImportConditions()
{
    var overlay = this.FindControl<Border>("ImportOverlay");
    if (overlay != null)
    {
        overlay.IsVisible = true;
        FadeInPanel(overlay);
    }

    var panel = this.FindControl<ImportOverlay>("ImportOverlayControl");
    if (panel != null)
    {
        panel.IsVisible = true;
        PopInPanel(panel);
    }
}

private async void OnImportContinue(object? sender, EventArgs e)
{
    var overlay = this.FindControl<Border>("ImportOverlay");
    if (overlay != null)
        PanelFader.FadeOut(overlay);

    await PickAndImportFileAsync();
}

// Клик по фону оверлея импорта — закрываем окно условий
private void OnImportOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (e.Source == sender && sender is Border overlay)
        PanelFader.FadeOut(overlay);
}

private async Task PickAndImportFileAsync()
{
    var topLevel = TopLevel.GetTopLevel(this);
    if (topLevel == null) return;

    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Выберите текстовый файл с паролями",
        AllowMultiple = false,
        FileTypeFilter = new[]
        {
            new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt", "*.csv", "*.log", "*.pass", "*.md" } },
            new FilePickerFileType("Все файлы") { Patterns = new[] { "*.*" } }
        }
    });

    if (files.Count == 0) return;
    string filePath = files[0].Path.LocalPath;

    if (!File.Exists(filePath))
    {
        ShowError($"{FluentIcons.Warning} Файл не найден!");
        return;
    }

    ShowImportProgress("Импорт паролей...");

    try
    {
        var imported = await PasswordImporter.ImportFromFileAsync(
            filePath,
            onProgress: (int percent) => UpdateImportProgress(percent, null, null),
            onLineParsed: msg => UpdateImportProgress(0, msg, null)
        );

        Console.WriteLine($" Импортировано записей: {imported.Count}");

        if (imported.Any())
        {
            if (!allPasswords.ContainsKey("Без группы"))
                allPasswords["Без группы"] = new List<PasswordEntry>();

            allPasswords["Без группы"].AddRange(imported);
            Console.WriteLine($" В 'Без группы' теперь {allPasswords["Без группы"].Count} паролей");

            // Аналог ImportFromDictionary — обработаем добавление данных из БД в nogroup_passwords
            SaveAllData();

            // Обновляем UI
            currentPasswords = new List<PasswordEntry>(allPasswords["Без группы"]);
            UpdatePasswordsList();

            ShowSuccessFlash($"{FluentIcons.CheckMark} Импортировано {imported.Count} паролей!");
        }
        else
        {
            ShowError($"{FluentIcons.Warning} Не найдено ни одного пароля в файле.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Ошибка импорта: {ex.Message}");
        ShowError($"{FluentIcons.Warning} Ошибка импорта: {ex.Message}");
    }
    finally
    {
        HideImportProgress();
    }
} 
}
