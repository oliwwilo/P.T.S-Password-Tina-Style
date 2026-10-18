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
    // ============ ПЕРЕКЛЮЧАТЕЛЬ "БЕЗ ГРУППЫ" ============

    private void OnNoGroupSwitchChanged(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (NoGroupSwitch.IsChecked == true)
            {
                SwitchToNoGroup();
            }
            else
            {
                var groups = allPasswords.Keys.Where(g => g != "Без группы" && !string.IsNullOrWhiteSpace(g)).ToList();
                if (groups.Any())
                {
                    SwitchToGroup(groups.First());
                }
                else if (!string.IsNullOrEmpty(_lastSelectedGroup) && allPasswords.ContainsKey(_lastSelectedGroup))
                {
                    SwitchToGroup(_lastSelectedGroup);
                }
                else
                {
                    NoGroupSwitch.IsChecked = true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка OnNoGroupSwitchChanged: {ex.Message}");
        }
    }

    // ============ ПЕРЕКЛЮЧЕНИЕ ГРУПП ============

    private void SwitchToNoGroup()
    {
        if (_isMovingPasswords)
        {
            Console.WriteLine(" Блокировка: идет перемещение паролей");
            UpdatePasswordsList();
            return;
        }

        // Сразу скрываем поле переименования и фото группы — не ждём загрузки из БД
        // (и отменяем фоновую загрузку GIF, чтобы картинка не «всплыла» после скрытия)
        HideGroupIcon();

        if (!string.IsNullOrEmpty(currentGroup) && currentGroup != "Без группы")
        {
            if (allPasswords.ContainsKey(currentGroup))
                allPasswords[currentGroup] = new List<PasswordEntry>(currentPasswords);
        }

        if (!allPasswords.ContainsKey("Без группы"))
        {
            var docs = _dbService.GetNoGroupPasswords();
            var list = new List<PasswordEntry>();
            foreach (var doc in docs)
            {
                var entry = new PasswordEntry
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    Email = doc.Email,
                    Login = doc.Login,
                    Description = doc.Description,
                    Number = doc.Number,
                    IsSelected = doc.IsSelected
                };
                entry.SetEncryptedPassword(doc.Password);
                list.Add(entry);
            }
            if (docs.Count > 1)
                list = list.OrderBy(x => x.Number).ToList();
            allPasswords["Без группы"] = list;
        }

        currentGroup = "Без группы";
        currentPasswords = new List<PasswordEntry>(allPasswords["Без группы"]);

        ImportButton = this.FindControl<Button>("ImportButton");
        if (ImportButton != null)
            ImportButton.IsVisible = true; // по умолчанию скрыта
        _clearAllButton = this.FindControl<Button>("ClearAllButton");
        if (_clearAllButton != null)
            _clearAllButton.IsVisible = true; // по умолчанию скрыта
        UpdateUIControls("Без группы", isDeleteVisible: false, isIconVisible: false);
        if (NoGroupSwitch != null)
            NoGroupSwitch.IsChecked = true;
// Сбрасываем поиск
        _searchText = "";
        if (SearchBox != null)
            SearchBox.Text = "";
        if (ClearSearchButton != null)
            ClearSearchButton.IsVisible = false;
        UpdatePasswordsList();
        UpdateImportButtonsVisibility();
        HighlightAllGroups();
        ClearSelection();

        Console.WriteLine($" Переключено на 'Без группы'");
    }

    
   
    private void SwitchToGroup(string groupName)
    {
        LogState($"SwitchToGroup (начало для {groupName})");
        if (_isMovingPasswords)
        {
            Console.WriteLine(" Блокировка: идет перемещение паролей");
            return;
        }

        if (groupName == "Без группы")
        {
            SwitchToNoGroup();
            return;
        }

        if (!string.IsNullOrEmpty(currentGroup) && currentGroup != "Без группы")
        {
            if (allPasswords.ContainsKey(currentGroup))
                allPasswords[currentGroup] = new List<PasswordEntry>(currentPasswords);
        }
// Сбрасываем поиск
        _searchText = "";
        if (SearchBox != null)
            SearchBox.Text = "";
        if (ClearSearchButton != null)
            ClearSearchButton.IsVisible = false;
        if (!allPasswords.ContainsKey(groupName))
        {
            var docs = _dbService.GetPasswordsByGroup(groupName);
            var list = new List<PasswordEntry>();
            foreach (var doc in docs)
            {
                var entry = new PasswordEntry
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    Email = doc.Email,
                    Login = doc.Login,
                    Description = doc.Description,
                    Number = doc.Number,
                    IsSelected = doc.IsSelected
                };
                entry.SetEncryptedPassword(doc.Password);
                list.Add(entry);
            }
            if (docs.Count > 1)
                list = list.OrderBy(x => x.Number).ToList();
            allPasswords[groupName] = list;
        }

        currentGroup = groupName;
        _lastSelectedGroup = groupName;
        currentPasswords = new List<PasswordEntry>(allPasswords[groupName]);

        if (GroupIcon != null)
            GroupIcon.IsVisible = true;
        if (GroupIconBorder != null)
            GroupIconBorder.IsVisible = true;
        if (GroupNameEditor != null)
        {
            GroupNameEditor.IsVisible = true;
            GroupNameEditor.Text = groupName;
            GroupNameEditor.IsEnabled = true;
        }

        UpdateUIControls(groupName, isDeleteVisible: true);
        if (NoGroupSwitch != null)
            NoGroupSwitch.IsChecked = false;

        LoadGroupIcon(groupName);
        UpdatePasswordsList();
        UpdateImportButtonsVisibility();
        HighlightAllGroups();
        ClearSelection();
        LogState($"SwitchToGroup (конец для {groupName})");
        Console.WriteLine($" Переключено на группу: {groupName}");
    }

    // ============ ПОДСВЕТКА ВЫБРАННОЙ ГРУППЫ ============

    private async void HighlightAllGroups(bool animate = true)
    {
        CancelGroupHighlight();
        bool isNoGroup = string.IsNullOrEmpty(currentGroup) || currentGroup == "Без группы";
        var children = GroupsPanel.Children.ToList();

        foreach (var container in children)
        {
            if (container is Border border && border.Child is Grid innerGrid)
            {
                foreach (var child in innerGrid.Children)
                {
                    if (child is Border greenBar && greenBar.Width == 5)
                    {
                        bool isActive = !isNoGroup && border.Tag?.ToString() == currentGroup;

                        if (animate && isActive)
                        {
                            await AnimateGroupHighlight(greenBar);
                        }
                        else
                        {
                            greenBar.Background = isActive ? HighlightGradientBrush : Brushes.Transparent;
                            greenBar.Opacity = isActive ? 1 : 1;
                            greenBar.BoxShadow = new BoxShadows();
                        }
                    }
                }
            }
        }
    }

}
