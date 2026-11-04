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
    private string _searchText = "";
    private bool _isResettingSearch;

    /// <summary>
    /// Полный список паролей текущей группы — без учёта фильтра поиска.
    /// Важно: currentPasswords может быть отфильтрован поиском,
    /// поэтому сохранять его в allPasswords нельзя — иначе пароли теряются.
    /// </summary>
    private List<PasswordEntry> GetFullCurrentGroupList()
    {
        if (!allPasswords.TryGetValue(currentGroup, out var full) || full == null)
        {
            full = new List<PasswordEntry>();
            allPasswords[currentGroup] = full;
        }
        return full;
    }

    /// <summary>
    /// Полностью очищает поле поиска и снимает фильтр.
    /// </summary>
    private void ResetSearch()
    {
        _searchText = "";

        // Не перерисовываем список из TextChanged: вызывающий код сам обновит его
        // (и сможет сделать это с анимацией, без двойной перестройки).
        _isResettingSearch = true;
        try
        {
            if (SearchBox != null && SearchBox.Text != "")
                SearchBox.Text = "";
        }
        finally
        {
            _isResettingSearch = false;
        }

        if (ClearSearchButton != null)
            ClearSearchButton.IsVisible = false;

        UpdateSearchPlaceholder();
    }

    private void UpdateSearchPlaceholder()
    {
        if (SearchPlaceholderOverlay == null) return;

        bool hasText = SearchBox != null && !string.IsNullOrEmpty(SearchBox.Text);
        bool focused = SearchBox != null && SearchBox.IsKeyboardFocusWithin;
        SearchPlaceholderOverlay.Opacity = (!hasText && !focused) ? 1 : 0;
    }
    private void OnSearchGotFocus(object? sender, FocusChangedEventArgs e)
    {
        UpdateSearchPlaceholder();
    }
    private void OnSearchLostFocus(object? sender, RoutedEventArgs e)
    {
        UpdateSearchPlaceholder();
    }
    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Клик по фону (вне выдвижного меню) — закрываем меню
        if (_isDrawerOpen &&
            e.Source is Control clicked &&
            !IsWithin(clicked, DrawerContainer) &&
            !IsWithin(clicked, MenuButton))
        {
            _ = CloseDrawer();
        }

        if (SearchBox == null || !SearchBox.IsKeyboardFocusWithin)
        {
            // Клик вне поля переименования группы — завершаем переименование (снимаем фокус)
            if (GroupNameEditor != null && GroupNameEditor.IsKeyboardFocusWithin &&
                e.Source is Control renameSource &&
                !IsWithin(renameSource, GroupNameEditor))
            {
                TopLevel.GetTopLevel(GroupNameEditor)?.FocusManager?.Focus(null, NavigationMethod.Unspecified, KeyModifiers.None);
            }
            return;
        }

        // Клик по полю поиска, кнопке очистки или иконке лупы — фокус не трогаем
        if (e.Source is Control source &&
            (IsWithin(source, SearchBox) ||
             IsWithin(source, ClearSearchButton) ||
             IsWithin(source, SearchIconBorder)))
            return;

        // Клик по фону — снимаем фокус, чтобы вернулась подпись "Поиск"
        TopLevel.GetTopLevel(SearchBox)?.FocusManager?.Focus(null, NavigationMethod.Unspecified, KeyModifiers.None);
    }
    private static bool IsWithin(Control? node, Control? root)
    {
        while (node != null)
        {
            if (node == root) return true;
            node = node.Parent as Control;
        }
        return false;
    }
    private void ApplySearchFilter(bool animateList = false)
    {
        if (PasswordsContainer == null) return;

        // Все пароли текущей группы
        var all = allPasswords.TryGetValue(currentGroup, out var list) ? list : new List<PasswordEntry>();

        // Если поиск пустой – показываем все
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            currentPasswords = new List<PasswordEntry>(all);
            UpdatePasswordsList(animateList);
            return;
        }

        // Фильтруем ТОЛЬКО по названию (первые буквы)
        var filtered = new List<PasswordEntry>();
        foreach (var pwd in all)
        {
            if (!string.IsNullOrEmpty(pwd.Title) && 
                pwd.Title.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(pwd);
            }
        }

        currentPasswords = filtered;
        UpdatePasswordsList(animateList);
    }
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isResettingSearch) return;

        if (sender is TextBox textBox)
        {
            _searchText = textBox.Text?.Trim() ?? "";
            ApplySearchFilter();
        
            // Показываем кнопку очистки, если есть
            if (ClearSearchButton != null)
                ClearSearchButton.IsVisible = !string.IsNullOrEmpty(_searchText);

            UpdateSearchPlaceholder();
        }
    }
    private void OnClearSearchClick(object? sender, RoutedEventArgs e)
    {
        if (SearchBox != null)
            SearchBox.Text = "";
        _searchText = "";
        ApplySearchFilter();
        if (ClearSearchButton != null)
            ClearSearchButton.IsVisible = false;
        UpdateSearchPlaceholder();
    }
}
