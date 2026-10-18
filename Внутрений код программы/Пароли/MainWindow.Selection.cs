using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace TinaPASS.Main;

public partial class MainWindow
{
    // ============ УПРАВЛЕНИЕ ВЫДЕЛЕНИЕМ ============
    private async void UpdatePasswordsScrollViewer()
{
    await Task.Delay(300); // Ждём завершения рендеринга

    if (Skrolbar1 == null || PasswordsContainer == null) return;

    PasswordsContainer.InvalidateMeasure();
    PasswordsContainer.InvalidateArrange();
    Skrolbar1.InvalidateMeasure();
    Skrolbar1.InvalidateArrange();

    var contentHeight = PasswordsContainer.Bounds.Height;
    var viewport = Skrolbar1.Viewport.Height;

    Console.WriteLine($" Пароли: контент={contentHeight}px, viewport={viewport}px");

    if (contentHeight > viewport && viewport > 0)
    {
        Skrolbar1.Offset = new Vector(0, contentHeight - viewport + 10);
        Console.WriteLine($" Прокрутка паролей: {Skrolbar1.Offset.Y}px");
    }

    Skrolbar1.InvalidateVisual();

    // Повторный вызов через 200 мс для надёжности
    await Task.Delay(200);
    if (Skrolbar1 == null || PasswordsContainer == null) return;

    contentHeight = PasswordsContainer.Bounds.Height;
    viewport = Skrolbar1.Viewport.Height;

    if (contentHeight > viewport && viewport > 0)
    {
        Skrolbar1.Offset = new Vector(0, contentHeight - viewport + 10);
        Console.WriteLine($" Повторная прокрутка паролей: {Skrolbar1.Offset.Y}px");
    }
}
    private void UpdateSelectionUI()
    {
        PerformanceLogger.Start("UpdateSelectionUI");
        bool hasSelection = _selectedPasswords.Any();
        _isSelectionMode = hasSelection;

        if (SelectionInfoText != null)
        {
            if (hasSelection)
            {
                FluentIcons.Set(SelectionInfoText, $"{FluentIcons.CheckMark} Выбрано: {_selectedPasswords.Count}  Нажмите на группу для переноса");
                SelectionInfoText.IsVisible = true;
            }
            else
            {
                SelectionInfoText.Text = "";
                SelectionInfoText.IsVisible = false;
            }
        }

        var sw = Stopwatch.StartNew();
        int processed = 0;
        foreach (var child in PasswordsContainer.Children)
        {
            if (child is Border card && card.Tag is PasswordEntry pwd)
            {
                if (_selectedPasswords.Contains(pwd))
                {
                    card.Background = Brush.Parse("#3369DBFE");
                    card.BorderBrush = null;
                    card.BorderThickness = new Thickness(0);
                }
                else
                {
                    card.Background = Brush.Parse("#55000000");
                    card.BorderBrush = null;
                    card.BorderThickness = new Thickness(0);
                }
                processed++;
            }
        }
        sw.Stop();
        PerformanceLogger.Log($"Обновлено {processed} карточек за {sw.ElapsedMilliseconds} мс");
        PerformanceLogger.Finish($"выделено: {_selectedPasswords.Count}");
    }
private void ClearSelection()
{
    PerformanceLogger.Start("ClearSelection");
    _selectedPasswords.Clear();
    _isSelectionMode = false;

    if (SelectionInfoText != null)
    {
        SelectionInfoText.IsVisible = false;
        SelectionInfoText.Text = "";
    }

    var sw = Stopwatch.StartNew();
    int processed = 0;
    foreach (var child in PasswordsContainer.Children)
    {
        if (child is Border card)
        {
            card.Background = Brush.Parse("#55000000");
            card.BorderBrush = null;
            card.BorderThickness = new Thickness(0);

            processed++;
        }
    }
    sw.Stop();
    PerformanceLogger.Log($"Обработано {processed} карточек за {sw.ElapsedMilliseconds} мс");
    PerformanceLogger.Finish();
}
}
