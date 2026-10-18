using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class MainWindow
{
   private void LoadLocalData()
{
    var (loadedPasswords, loadedImages, loadedOrder) = _dbService.ExportToDictionary();
    allPasswords = loadedPasswords ?? new Dictionary<string, List<PasswordEntry>>();
    groupImages = loadedImages ?? new Dictionary<string, string>();
    groupOrder = loadedOrder ?? new List<string>();

    Console.WriteLine($" LoadLocalData: загружено групп {allPasswords.Count}, order: {string.Join(", ", groupOrder)}");

    // ---- СИНХРОНИЗАЦИЯ: добавляем все группы из allPasswords в groupOrder ----
    foreach (var name in allPasswords.Keys.ToList())
    {
        if (!groupOrder.Contains(name) && name != "Без группы")
        {
            groupOrder.Add(name);
            Console.WriteLine($" Группа '{name}' добавлена в groupOrder из allPasswords");
        }
    }

    // ---- СИНХРОНИЗАЦИЯ: восстанавливаем группы из order в allPasswords ----
    foreach (var name in groupOrder.ToList())
    {
        if (!allPasswords.ContainsKey(name) && name != "Без группы")
        {
            allPasswords[name] = new List<PasswordEntry>();
            Console.WriteLine($" Группа '{name}' восстановлена из order в allPasswords");
        }
    }

    // ---- Удаляем из groupOrder те, которых нет в allPasswords ----
    var toRemove = groupOrder.Where(name => !allPasswords.ContainsKey(name) && name != "Без группы").ToList();
    foreach (var name in toRemove)
    {
        groupOrder.Remove(name);
        Console.WriteLine($" Группа '{name}' удалена из groupOrder (нет в allPasswords)");
    }

    // ---- "Без группы" всегда первая ----
    if (groupOrder.Contains("Без группы"))
        groupOrder.Remove("Без группы");
    groupOrder.Insert(0, "Без группы");

    if (!allPasswords.ContainsKey("Без группы"))
        allPasswords["Без группы"] = new List<PasswordEntry>();

    // ---- Заполняем currentPasswords для "Без группы" ----
    currentPasswords.Clear();
    if (allPasswords.TryGetValue("Без группы", out var list))
    {
        foreach (var pwd in list)
            currentPasswords.Add(pwd);
    }

    RefreshAllGroups();
}
    // ============ ВХОД (ЛОКАЛЬНЫЙ РЕЖИМ) ============
    private async void OnLoginCompleted(object? sender, bool isAuthenticated)
    {
        if (!isAuthenticated) return;

        try
        {
            _isAuthenticated = true;

            LoadLocalData();

            if (DrawerPanelControl != null)
                DrawerPanelControl.SetLoggedIn(true);

            await CloseLoginAndShowMain();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка: {ex.Message}");
            ShowError($"Ошибка: {ex.Message}");
        }
    }

    // ============ ЗАКРЫТИЕ ОКНА ВХОДА ============
    private async Task CloseLoginAndShowMain()
    {
        Debug.WriteLine(" ЗАКРЫТИЕ ОКНА ВХОДА");
        FadeOutPanel(Overlay);
        LoginOverlayControl.Hide();
        MainContent.IsEnabled = true;
        Debug.WriteLine(" Главный экран открыт");
    }

    // ============ ВЫХОД ============
    private async void OnDrawerLogoutRequested(object? sender, EventArgs e)
    {
        try
        {
            _isAuthenticated = false;

            allPasswords.Clear();
            groupImages.Clear();
            currentPasswords.Clear();
            currentGroup = "";

            GroupsPanel.Children.Clear();
            if (GroupNameEditor != null) GroupNameEditor.Text = "";
            if (DeleteGroupButton != null) DeleteGroupButton.IsVisible = false;
            if (GroupIcon != null) GroupIcon.IsVisible = false;
            if (GroupIconInitial != null) GroupIconInitial.IsVisible = false;

            ShowLogin();
            await CloseDrawer();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    // ============ ГОСТЕВОЙ РЕЖИМ ============
    private async void OnGuestRequested(object? sender, EventArgs e)
    {
        Debug.WriteLine(" ГОСТЕВОЙ РЕЖИМ");

        _isAuthenticated = false;

        LoadLocalData();

        if (DrawerPanelControl != null)
            DrawerPanelControl.SetLoggedIn(false);

        await CloseLoginAndShowMain();
        ShowSuccessFlash($"{FluentIcons.Contact} Вы вошли как гость (локальные данные)");
    }

    // ============ РЕГИСТРАЦИЯ ============
    private void OnRegisterCompleted(object? sender, RegisterData data)
    {
        ShowSuccessFlash($"{FluentIcons.CheckMark} Добро пожаловать, {data.Email}!");
        RegisterOverlayControl.Hide();
        LoginOverlayControl.Show();
        PopInPanel(LoginOverlayControl);
    }
}