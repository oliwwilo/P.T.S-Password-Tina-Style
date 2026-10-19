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
    // ---- Поля для перетаскивания (Drag-and-Drop) ----
    private bool isDraggingGroup = false;
    private Border? draggedGroup = null;
    private Point dragStartPoint;
    private int dragTargetIndex = -1;
    private double _groupHeight;
    private double _dragStartY;
    private const double MARGIN_TOP = 8;
    private const double MARGIN_BOTTOM = 8;
    private const double THRESHOLD = 0.1;

    // ---- Остальные поля ----
    private bool _isMovingPasswords = false;
    private string _lastSelectedGroup = "";
    private Dictionary<string, Border> _groupPhotoBorders = new();

    private static void DisposeStaticSource(Control? control)
    {
        if (control is Image image && image.Source is Bitmap bitmap)
        {
            image.Source = null;
            bitmap.Dispose();
        }
    }
    private Dictionary<string, GifHelper> _groupGifHelpers = new();

    // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============

    private void UpdateUIControls(string groupName = "", bool isDeleteVisible = false, bool isIconVisible = false)
    {
        try
        {
            if (GroupNameEditor != null) GroupNameEditor.Text = groupName;
            if (DeleteGroupButton != null) DeleteGroupButton.IsVisible = isDeleteVisible;
            if (GroupIcon != null) GroupIcon.IsVisible = isIconVisible;
            if (GroupNameEditor != null)
                GroupNameEditor.IsEnabled = groupName != "Без группы" && !string.IsNullOrEmpty(groupName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка UpdateUIControls: {ex.Message}");
        }
    }

    private void UpdateGroupOrderFromUI()
    {
        var newOrder = new List<string>();
        foreach (var child in GroupsPanel.Children)
        {
            if (child is Border border && border.Tag is string name && name != "Без группы")
                newOrder.Add(name);
        }
        if (!newOrder.Contains("Без группы"))
            newOrder.Insert(0, "Без группы");
        groupOrder = newOrder;
    }

    // ============ ПОКАЗ/СКРЫТИЕ НАЗВАНИЙ ГРУПП ============

    private bool _groupNamesVisible = false;
    private Dictionary<string, TextBlock> _groupNameTexts = new();

    // Базовые (обычные) значения считываются из XAML при первом нажатии,
    // поэтому раскрытие всегда возвращает всё ровно в исходный размер/позиции.
    private bool _groupsBaseCaptured = false;
    private double _groupsNormalWidth = 100;
    private const double GroupsWideWidth = 260;
    private Thickness _baseSmenaslo;
    private Thickness _baseSearchBar;

    private void CaptureGroupsBase()
    {
        if (_groupsBaseCaptured) return;
        if (GroupsScrollViewer != null)
        {
            _groupsNormalWidth = double.IsNaN(GroupsScrollViewer.Width) ? GroupsScrollViewer.Bounds.Width : GroupsScrollViewer.Width;
            if (_groupsNormalWidth <= 0) _groupsNormalWidth = 100;
        }
        if (smenaslo != null) _baseSmenaslo = smenaslo.Margin;
        if (SearchBar != null) _baseSearchBar = SearchBar.Margin;
        _groupsBaseCaptured = true;
    }

    private async void OnToggleGroupsClick(object? sender, RoutedEventArgs e)
    {
        CaptureGroupsBase();

        _groupNamesVisible = !_groupNamesVisible;
        SaveGroupNamesState(_groupNamesVisible);
        UpdateGroupToggleText();

        double target = _groupNamesVisible ? GroupsWideWidth : _groupsNormalWidth;
        double from = double.IsNaN(GroupsScrollViewer.Width) ? _groupsNormalWidth : GroupsScrollViewer.Width;

        ApplyGroupTooltips();

        await Task.WhenAll(
            AnimateGroupsListWidth(from, target),
            AnimateGroupNames(_groupNamesVisible));
    }

    // ===== Сохранение состояния «названия групп показаны/скрыты» =====

    private static string GroupNamesStatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TinaPASS", "groupnames.txt");

    private static bool LoadGroupNamesState()
    {
        try
        {
            var p = GroupNamesStatePath;
            if (File.Exists(p)) return File.ReadAllText(p).Trim() == "1";
        }
        catch { }
        return false;
    }

    private static void SaveGroupNamesState(bool visible)
    {
        try
        {
            var dir = Path.GetDirectoryName(GroupNamesStatePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(GroupNamesStatePath, visible ? "1" : "0");
        }
        catch { }
    }

    // Применяет сохранённое состояние без анимации (при запуске / после пересборки групп)
    private void ApplyGroupNamesState()
    {
        CaptureGroupsBase();
        double target = _groupNamesVisible ? GroupsWideWidth : _groupsNormalWidth;
        GroupsScrollViewer.Width = target;
        ApplyGroupCardsWidth(target);
        ApplyPasswordAreaShift(target - _groupsNormalWidth);
        UpdateGroupToggleText();
        ApplyGroupTooltips();
    }

    // Подсказка-«табличка» работает только когда названия групп скрыты
    private void ApplyGroupTooltips()
    {
        foreach (var child in GroupsPanel.Children)
        {
            if (child is Border b && b.Tag is string name)
                ToolTip.SetTip(b, _groupNamesVisible ? null : name);
        }
    }

    // Иконка кнопки-глазика: названия скрыты — закрытый глаз, показаны — открытый
    private void UpdateGroupToggleText()
    {
        if (EyeOpenIcon != null) EyeOpenIcon.IsVisible = _groupNamesVisible;
        if (EyeClosedIcon != null) EyeClosedIcon.IsVisible = !_groupNamesVisible;
    }

    private async Task AnimateGroupsListWidth(double from, double to, int steps = 18, int delayMs = 8)
    {
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double eased = 1 - Math.Pow(1 - t, 3);
            double w = from + (to - from) * eased;
            GroupsScrollViewer.Width = w;
            ApplyGroupCardsWidth(w);
            ApplyPasswordAreaShift(w - _groupsNormalWidth);
            await Task.Delay(delayMs);
        }
        GroupsScrollViewer.Width = to;
        ApplyGroupCardsWidth(to);
        ApplyPasswordAreaShift(to - _groupsNormalWidth);
    }

    // Ширина карточек групп меняется вместе со списком, чтобы названия влезали
    private void ApplyGroupCardsWidth(double width)
    {
        foreach (var child in GroupsPanel.Children)
        {
            if (child is Border b)
                b.Width = width;
        }
    }

    // Сдвигает область паролей (заголовок, поиск, значок и линию) вслед за шириной списка групп
    private void ApplyPasswordAreaShift(double delta)
    {
        if (smenaslo != null)
            smenaslo.Margin = new Thickness(_baseSmenaslo.Left + delta, _baseSmenaslo.Top, _baseSmenaslo.Right, _baseSmenaslo.Bottom);
        if (SearchBar != null)
            SearchBar.Margin = new Thickness(_baseSearchBar.Left + delta, _baseSearchBar.Top, _baseSearchBar.Right, _baseSearchBar.Bottom);
    }

    private async Task AnimateGroupNames(bool visible, int steps = 10, int delayMs = 12)
    {
        var texts = _groupNameTexts.Values.ToList();
        foreach (var t in texts)
        {
            if (t == null) continue;
            t.IsVisible = true;
            t.Opacity = visible ? 0 : 1;
        }

        for (int i = 1; i <= steps; i++)
        {
            double progress = (double)i / steps;
            double eased = 1 - Math.Pow(1 - progress, 3);
            foreach (var t in texts)
            {
                if (t == null) continue;
                t.Opacity = visible ? eased : 1 - eased;
            }
            await Task.Delay(delayMs);
        }

        foreach (var t in texts)
        {
            if (t == null) continue;
            t.Opacity = visible ? 1 : 0;
            if (!visible) t.IsVisible = false;
        }
    }

}
