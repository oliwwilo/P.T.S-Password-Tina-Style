using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TinaPASS.Main;

public partial class MainWindow
{
    // ============ ОБНОВЛЕНИЕ ВИДИМОСТИ КНОПОК ============

    private void UpdateImportButtonsVisibility()
    {
        bool isNoGroup = currentGroup == "Без группы";
        if (ImportButton != null) ImportButton.IsVisible = isNoGroup;
        if (_clearAllButton != null) _clearAllButton.IsVisible = isNoGroup;
    }

    // ============ ОБНОВЛЕНИЕ ВСЕХ ГРУПП ============

    // Обычное обновление — инкрементальное: добавляем/удаляем/переставляем
    // только изменившиеся кнопки, не пересоздавая всю панель.
    public void RefreshAllGroups() => ReconcileGroups(forceFull: false);

    // Полный пересбор панели — использовать только при входе/выходе из аккаунта.
    public void RebuildAllGroups() => ReconcileGroups(forceFull: true);

    private void ReconcileGroups(bool forceFull)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // ---- СИНХРОНИЗАЦИЯ: добавляем все группы из allPasswords в groupOrder ----
            foreach (var name in allPasswords.Keys.ToList())
            {
                if (!groupOrder.Contains(name) && name != "Без группы")
                {
                    groupOrder.Add(name);
                    Console.WriteLine($" Группа '{name}' добавлена в groupOrder");
                }
            }

            // ---- Удаляем из groupOrder те, которых нет в allPasswords ----
            groupOrder = groupOrder
                .Where(name => allPasswords.ContainsKey(name) || name == "Без группы")
                .ToList();

            // ---- "Без группы" всегда первая ----
            if (groupOrder.Contains("Без группы"))
                groupOrder.Remove("Без группы");
            groupOrder.Insert(0, "Без группы");

            var orderedGroups = groupOrder.Where(g => g != "Без группы").ToList();

            if (forceFull)
            {
                // ---- Полный пересбор ----
                foreach (var child in GroupsPanel.Children.ToList())
                {
                    if (child is Border b && b.Tag is string n)
                        DisposeGroupButton(b, n);
                }
                GroupsPanel.Children.Clear();
                _groupPhotoBorders.Clear();
                _groupNameTexts.Clear();

                foreach (var groupName in orderedGroups)
                    AppendGroupButton(groupName);
            }
            else
            {
                // ---- Инкрементальная сверка ----
                var desired = new HashSet<string>(orderedGroups);

                // Удаляем кнопки групп, которых больше нет
                for (int i = GroupsPanel.Children.Count - 1; i >= 0; i--)
                {
                    if (GroupsPanel.Children[i] is Border b && b.Tag is string n && !desired.Contains(n))
                    {
                        DisposeGroupButton(b, n);
                        GroupsPanel.Children.RemoveAt(i);
                    }
                }

                // Добавляем новые и выставляем нужный порядок
                for (int idx = 0; idx < orderedGroups.Count; idx++)
                {
                    var groupName = orderedGroups[idx];
                    int existing = FindGroupButtonIndex(groupName);
                    if (existing < 0)
                    {
                        var container = BuildGroupButton(groupName);
                        if (container != null)
                            GroupsPanel.Children.Insert(Math.Min(idx, GroupsPanel.Children.Count), container);
                    }
                    else if (existing != idx)
                    {
                        GroupsPanel.Children.Move(existing, Math.Min(idx, GroupsPanel.Children.Count - 1));
                    }
                }
            }

            // ---- Переключаемся на текущую группу или первую ----
            if (orderedGroups.Any())
            {
                string targetGroup;
                if (!string.IsNullOrEmpty(currentGroup) && currentGroup != "Без группы" && allPasswords.ContainsKey(currentGroup))
                    targetGroup = currentGroup;
                else
                    targetGroup = orderedGroups.First();

                SwitchToGroup(targetGroup);
            }
            else
            {
                SwitchToNoGroup();
            }

            HighlightAllGroups();
            ApplyGroupNamesState();
            UpdateImportButtonsVisibility();
        });
    }

    private void AppendGroupButton(string groupName)
    {
        var container = BuildGroupButton(groupName);
        if (container != null)
            GroupsPanel.Children.Add(container);
    }

    private int FindGroupButtonIndex(string groupName)
    {
        for (int i = 0; i < GroupsPanel.Children.Count; i++)
        {
            if (GroupsPanel.Children[i] is Border b && b.Tag?.ToString() == groupName)
                return i;
        }
        return -1;
    }

    private void DisposeGroupButton(Border border, string groupName)
    {
        if (_groupGifHelpers.TryGetValue(groupName, out var helper))
        {
            helper.Dispose();
            _groupGifHelpers.Remove(groupName);
        }
        else
        {
            DisposeStaticSource(border.Child);
        }
        _groupPhotoBorders.Remove(groupName);
        _groupNameTexts.Remove(groupName);
    }
}
