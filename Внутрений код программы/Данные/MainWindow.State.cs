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
    // ============ Показ главного экрана ============
    // Лёгкая подготовка — выполняется сразу в конструкторе
    private void PrepareMainScreen()
    {
        MainContent.IsEnabled = true;
        Overlay.IsVisible = false;
        LoginOverlayControl.Hide();
        Debug.WriteLine(" Главный экран подготовлен");
    }

    // Окно показывается сразу с экраном загрузки, затем строится UI и загрузка скрывается
    private async Task InitializeAsync()
    {
        // Даём окну отрисоваться вместе с экраном загрузки
        await Task.Delay(50);
        LoadMainScreenData();
        PanelFader.FadeOut(LoadingOverlay, 20, 14);
    }

    // Тяжёлая загрузка данных и построение UI
    private void LoadMainScreenData()
    {
        LoadLocalData();  // <- внутри вызывает RefreshAllGroups
        DeduplicateImages();
        Debug.WriteLine(" Данные главного экрана загружены");
    }

    // ============ Сохранение данных ============
    private async void SaveAllData()
    {
        if (allPasswords == null || allPasswords.Count == 0 ||
            (allPasswords.Count == 1 && allPasswords.ContainsKey("Без группы") && allPasswords["Без группы"].Count == 0))
        {
            Console.WriteLine(" SaveAllData: пропущено (пустые данные)");
            return;
        }

        if (_isSaving) return;
        lock (_saveLock)
        {
            if (_isSaving) return;
            _isSaving = true;
        }

        try
        {
            // ---- Синхронизируем groupOrder с allPasswords ----
            var allGroupNames = allPasswords.Keys.ToList();
            foreach (var name in allGroupNames)
            {
                if (!groupOrder.Contains(name) && name != "Без группы")
                    groupOrder.Add(name);
            }
            groupOrder = groupOrder.Where(name => allPasswords.ContainsKey(name) || name == "Без группы").ToList();
            if (groupOrder.Contains("Без группы"))
                groupOrder.Remove("Без группы");
            groupOrder.Insert(0, "Без группы");

            Console.WriteLine($" Сохраняем порядок групп: {string.Join(", ", groupOrder)}");

            NormalizeIds();
            NormalizePasswordNumbers();

            await Task.Run(() => _dbService.SaveAllIncremental(allPasswords, groupImages, groupOrder));
            Console.WriteLine(" Данные сохранены в LiteDB");
            LogState("SaveAllData (после сохранения)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка сохранения: {ex.Message}");
            ShowError($"{FluentIcons.Warning} Ошибка сохранения: {ex.Message}");
        }
        finally
        {
            lock (_saveLock) { _isSaving = false; }
        }
        Console.WriteLine($" SaveAllData: groupOrder = {string.Join(", ", groupOrder)}");
    }
    private void LogState(string context)
    {
        Console.WriteLine($"===== СОСТОЯНИЕ [{context}] =====");
        Console.WriteLine($"  currentGroup: {currentGroup}");
        Console.WriteLine($"  groupOrder: {string.Join(", ", groupOrder ?? new List<string>())}");
       
        Console.WriteLine($"  allPasswords.Count: {allPasswords?.Count ?? 0}");
        Console.WriteLine($"  currentPasswords.Count: {currentPasswords?.Count ?? 0}");
        Console.WriteLine($"  _isSaving: {_isSaving}");
        Console.WriteLine($"  _isMovingPasswords: {_isMovingPasswords}");
        Console.WriteLine("========================================");
    }

    

   
    private void NormalizeIds()
    {
        var seenIds = new HashSet<Guid>();
        bool duplicateFound = false;

        foreach (var group in allPasswords.Values)
        {
            foreach (var pwd in group)
            {
                if (pwd.Id == Guid.Empty || !seenIds.Add(pwd.Id))
                {
                    pwd.Id = Guid.NewGuid();
                    seenIds.Add(pwd.Id);
                    duplicateFound = true;
                    Console.WriteLine($" NormalizeIds: присвоен новый Id для '{pwd.Title}'");
                }
            }
        }

        if (duplicateFound)
            Console.WriteLine(" NormalizeIds: исправлены дублирующиеся Id");
        else
            Console.WriteLine(" NormalizeIds: все Id уникальны");
    }

    // Проставляет последовательные номера паролям в каждой группе,
    // чтобы при загрузке порядок карточек сохранялся.
    private void NormalizePasswordNumbers()
    {
        foreach (var group in allPasswords.Values)
        {
            for (int i = 0; i < group.Count; i++)
                group[i].Number = i;
        }
    }
}
