using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class MainWindow
{
    // ============ СОЗДАНИЕ ГРУППЫ ============

    private void OnAddGroupClick(object? sender, RoutedEventArgs e) => ShowCreateGroupPanel();

    private void OnGroupCreated(object? sender, GroupData groupData)
    {
        LogState($"OnGroupCreated (начало для {groupData.Name})");
        if (allPasswords.ContainsKey(groupData.Name))
        {
            ShowError($"Группа с именем \"{groupData.Name}\" уже существует!");
            return;
        }

        if (!string.IsNullOrEmpty(groupData.ImagePath))
        {
            string destPath = CopyImageToAppData(groupData.ImagePath, groupData.Name);
            groupImages[groupData.Name] = destPath;
        }

        // Добавляем группу в словарь
        allPasswords[groupData.Name] = new List<PasswordEntry>();

        // ---- ВАЖНО: добавляем группу в КОНЕЦ списка (после всех существующих) ----
        if (!groupOrder.Contains(groupData.Name))
        {
            // Добавляем в конец (перед "Без группы", если она не первая)
            // Или просто в конец списка
            groupOrder.Add(groupData.Name);
            Console.WriteLine($" Группа '{groupData.Name}' добавлена в groupOrder в конец");
        }

        // Создаём кнопку и переключаемся
        AddGroupButton(groupData.Name, true);

        if (NoGroupSwitch != null) NoGroupSwitch.IsChecked = false;
        if (DeleteGroupButton != null) DeleteGroupButton.IsVisible = true;

        // Сохраняем все данные (включая порядок)
        SaveAllData();
        CloseAllPanels();
        LoadGroupIcon(groupData.Name);
        ShowSuccessFlash($"{FluentIcons.CheckMark} Группа \"{groupData.Name}\" создана!");
        LogState($"OnGroupCreated (конец для {groupData.Name})");
    }

    // ============ УДАЛЕНИЕ ГРУППЫ ============

    private void OnDeleteGroupClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup == "Без группы")
        {
            ShowError("Нельзя удалить эту группу!");
            return;
        }

        string deletingGroup = currentGroup;
        int passwordCount = allPasswords.TryGetValue(deletingGroup, out var list) ? list.Count : 0;

        ShowConfirmation($"Удалить группу \"{deletingGroup}\" и все {passwordCount} паролей?", () =>
        {
            try
            {
                allPasswords.Remove(deletingGroup);
                groupImages.Remove(deletingGroup);
                groupOrder?.Remove(deletingGroup);
                _dbService.DeleteGroup(deletingGroup);

                bool wasGifPowered = _groupGifHelpers.ContainsKey(deletingGroup);
                if (_groupGifHelpers.TryGetValue(deletingGroup, out var gifHelper))
                {
                    gifHelper.Dispose();
                    _groupGifHelpers.Remove(deletingGroup);
                }
                if (!wasGifPowered)
                {
                    foreach (var child in GroupsPanel.Children)
                    {
                        if (child is Border border && border.Tag?.ToString() == deletingGroup)
                            DisposeStaticSource(border.Child);
                    }
                }
                _groupPhotoBorders.Remove(deletingGroup);

                Border? groupToRemove = null;
                foreach (var child in GroupsPanel.Children)
                {
                    if (child is Border border && border.Tag?.ToString() == deletingGroup)
                    {
                        groupToRemove = border;
                        break;
                    }
                }
                if (groupToRemove != null)
                    GroupsPanel.Children.Remove(groupToRemove);

                var remaining = allPasswords.Keys.Where(g => g != "Без группы").ToList();
                if (remaining.Any()) SwitchToGroup(remaining.First());
                else SwitchToNoGroup();

                SaveAllData();
                ShowSuccessFlash($"{FluentIcons.CheckMark} Группа \"{deletingGroup}\" удалена!");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        });
    }

    // ============ ДОБАВЛЕНИЕ КНОПКИ ГРУППЫ ============

   private void AddGroupButton(string groupName, bool switchToThisGroup)
{
    Console.WriteLine($" AddGroupButton вызван для {groupName}, switchToThisGroup={switchToThisGroup}");

    if (!groupOrder.Contains(groupName))
    {
        int idx = groupOrder.IndexOf("Без группы");
        if (idx >= 0)
            groupOrder.Insert(idx + 1, groupName);
        else
            groupOrder.Add(groupName);
    }

    var container = BuildGroupButton(groupName);
    if (container == null) return;

    if (switchToThisGroup) SwitchToGroup(groupName);

    GroupsPanel.Children.Add(container);
    LogState($"AddGroupButton (конец для {groupName})");
}

// Строит визуальную кнопку группы, не добавляя её в панель
private Border? BuildGroupButton(string groupName)
{
    if (string.IsNullOrEmpty(groupName) || groupName == "Без группы" || groupName == "без группы")
        return null;

    Border container = new Border
    {
        Width = 80,
        Height = 60,
        Background = Brush.Parse("Transparent"),
        CornerRadius = new CornerRadius(10),
        ClipToBounds = true,
        Margin = new Thickness(0),
        Tag = groupName
    };

    Grid innerGrid = new Grid();

    Border greenBar = new Border
    {
        Width = 5,
        Height = 30,
        Background = Brush.Parse("Transparent"),
        CornerRadius = new CornerRadius(11),
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        Margin = new Thickness(5, 0, 0, 0)
    };

    Border photoBorder = new Border
    {
        Width = 50,
        Height = 50,
        CornerRadius = new CornerRadius(13),
        ClipToBounds = true,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        Margin = new Thickness(20, 0, 0, 0),
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
    };
    _groupPhotoBorders[groupName] = photoBorder;

    bool hasImage = false;
    if (groupImages.TryGetValue(groupName, out string? imagePath) && !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
    {
        try
        {
            var image = new Image
            {
                Stretch = Stretch.UniformToFill,
                Width = 50,
                Height = 50
            };
            photoBorder.Child = image;

            if (GifHelper.IsGifFile(imagePath))
            {
                if (_groupGifHelpers.TryGetValue(groupName, out var oldHelper))
                    oldHelper.Dispose();
                var gifHelper = new GifHelper(image);
                _groupGifHelpers[groupName] = gifHelper;
                gifHelper.LoadImageAsync(imagePath);
            }
            else
            {
                image.Source = new Bitmap(imagePath);
            }

            hasImage = true;
        }
        catch { hasImage = false; }
    }

    if (!hasImage)
        ApplyGroupPhotoPlaceholder(photoBorder, groupName);

    TextBlock nameText = new TextBlock
    {
        Text = groupName,
        FontFamily = new FontFamily("Inter"),
        FontWeight = FontWeight.Bold,
        FontSize = 14,
        Foreground = Brushes.White,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        Margin = new Thickness(80, 0, 0, 0),
        IsVisible = _groupNamesVisible,
        Opacity = _groupNamesVisible ? 1 : 0
    };

    _groupNameTexts[groupName] = nameText;

    innerGrid.Children.Add(greenBar);
    innerGrid.Children.Add(photoBorder);
    innerGrid.Children.Add(nameText);

    container.Child = innerGrid;

    double cardWidth = _groupNamesVisible && !double.IsNaN(GroupsScrollViewer.Width)
        ? Math.Max(GroupsScrollViewer.Width, GroupsWideWidth)
        : (double.IsNaN(GroupsScrollViewer.Width) ? container.Width : GroupsScrollViewer.Width);
    container.Width = cardWidth;

    ToolTip.SetTip(container, _groupNamesVisible ? null : groupName);
    ToolTip.SetShowDelay(container, 0);
    ToolTip.SetPlacement(container, PlacementMode.Right);
    ToolTip.SetHorizontalOffset(container, 6);
    ToolTip.SetVerticalOffset(container, -3);

    // Подписка на события перетаскивания
    container.PointerPressed += OnGroupPointerPressed;
    container.PointerMoved += OnGroupPointerMoved;
    container.PointerReleased += OnGroupPointerReleased;

    return container;
}

}
