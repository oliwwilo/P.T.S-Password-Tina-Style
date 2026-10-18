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
    // ============ МЕТОДЫ ПЕРЕТАСКИВАНИЯ ГРУПП ============

    private void OnGroupPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.Tag is not string groupName) return;
        if (groupName == "Без группы") return;

        draggedGroup = border;
        dragStartPoint = e.GetPosition(GroupsPanel);
        isDraggingGroup = true;
        dragTargetIndex = -1;
        _groupHeight = border.Bounds.Height;
        var startPos = draggedGroup.TranslatePoint(new Point(0, 0), GroupsPanel);
        _dragStartY = startPos?.Y ?? 0;

        // Визуальный эффект
        border.Opacity = 0.8;
        border.RenderTransform = new ScaleTransform(1.05, 1.05);
        border.ZIndex = 100;
    }

    private void OnGroupPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDraggingGroup || draggedGroup == null) return;

        var currentPoint = e.GetPosition(GroupsPanel);
        var delta = currentPoint - dragStartPoint;
        if (Math.Abs(delta.Y) < 5) return;

        if (draggedGroup.RenderTransform is TranslateTransform transform)
        {
            transform.X = 0;
            transform.Y += delta.Y;
        }
        else
        {
            draggedGroup.RenderTransform = new TranslateTransform(0, delta.Y);
        }
        dragStartPoint = currentPoint;

        int newIndex = GetTargetIndexOverGroup(GroupsPanel, currentPoint, draggedGroup);
        if (newIndex != -1 && newIndex != GetCurrentIndex(draggedGroup))
        {
            dragTargetIndex = newIndex;
            ShiftGroups(GroupsPanel, GetCurrentIndex(draggedGroup), newIndex, draggedGroup);
        }
    }

    private async void OnGroupPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isDraggingGroup || draggedGroup == null) return;

        string groupName = draggedGroup.Tag?.ToString() ?? "";
        ResetAllGroups();

        if (dragTargetIndex != -1 && dragTargetIndex != GetCurrentIndex(draggedGroup))
        {
            MoveGroupToIndex(draggedGroup, dragTargetIndex);
        }
        else
        {
            // Клик без перемещения (переключение или перемещение паролей)
            if (!string.IsNullOrEmpty(groupName) && groupName != "Без группы")
            {
                if (_selectedPasswords.Any())
                    MoveSelectedPasswordsToGroup(groupName);
                else
                    SwitchToGroup(groupName);
            }
        }

        // Сброс визуальных эффектов
        draggedGroup.Opacity = 1;
        draggedGroup.RenderTransform = null;
        draggedGroup.ZIndex = 0;
        draggedGroup = null;
        isDraggingGroup = false;
        dragTargetIndex = -1;
    }


    // ---- Вспомогательные методы для перетаскивания ----

    private void ResetAllGroups()
    {
        foreach (var child in GroupsPanel.Children)
        {
            if (child is Border b)
            {
                b.RenderTransform = null;
                b.Opacity = 1;
            }
        }
    }
    

   

    private int GetCurrentIndex(Border item)
    {
        return GroupsPanel.Children.IndexOf(item);
    }

   private int GetTargetIndexOverGroup(StackPanel panel, Point point, Border? draggedItem)
{
    if (draggedItem == null) return -1;
    var draggedPos = draggedItem.TranslatePoint(new Point(0, 0), panel);
    if (!draggedPos.HasValue) return -1;

    double draggedTop = draggedPos.Value.Y;
    double draggedBottom = draggedPos.Value.Y + _groupHeight;
    double groupSize = _groupHeight;

    bool movingDown = draggedTop > _dragStartY + 2;
    bool movingUp = draggedTop < _dragStartY - 2;

    double maxOverlap = 0;
    int targetIndex = -1;
    bool insertBefore = true;

    for (int i = 0; i < panel.Children.Count; i++)
    {
        var child = panel.Children[i];
        if (child == draggedItem) continue;

        var pos = child.TranslatePoint(new Point(0, 0), panel);
        if (!pos.HasValue) continue;

        double childTop = pos.Value.Y - MARGIN_TOP;
        double childBottom = pos.Value.Y + child.Bounds.Height + MARGIN_BOTTOM;
        double childCenter = (childTop + childBottom) / 2;

        double overlapTop = Math.Max(draggedTop, childTop);
        double overlapBottom = Math.Min(draggedBottom, childBottom);
        double overlap = Math.Max(0, overlapBottom - overlapTop);
        double overlapPercent = overlap / groupSize;

        if (overlapPercent > THRESHOLD && overlap > maxOverlap)
        {
            maxOverlap = overlap;
            targetIndex = i;
            if (movingDown) insertBefore = false;
            else if (movingUp) insertBefore = true;
            else
            {
                double overlapCenter = (overlapTop + overlapBottom) / 2;
                insertBefore = overlapCenter < childCenter;
            }
        }
    }

    if (targetIndex != -1)
        return insertBefore ? targetIndex : targetIndex + 1;

    // Если не нашли по перекрытию, ищем по позиции курсора
    double cursorY = point.Y;
    for (int i = 0; i < panel.Children.Count; i++)
    {
        var child = panel.Children[i];
        if (child == draggedItem) continue;
        var pos = child.TranslatePoint(new Point(0, 0), panel);
        if (!pos.HasValue) continue;
        double childTop = pos.Value.Y - MARGIN_TOP;
        double childBottom = pos.Value.Y + child.Bounds.Height + MARGIN_BOTTOM;
        double childCenter = (childTop + childBottom) / 2;
        if (cursorY >= childTop && cursorY <= childBottom)
            return cursorY < childCenter ? i : i + 1;
    }

    // Если курсор выше всех – вставить в начало
    if (panel.Children.Count > 0)
    {
        var first = panel.Children[0];
        var firstPos = first.TranslatePoint(new Point(0, 0), panel);
        if (firstPos.HasValue && cursorY < firstPos.Value.Y - MARGIN_TOP)
            return 0;
    }
    // Если курсор ниже всех – вставить в конец
    if (panel.Children.Count > 0)
    {
        var last = panel.Children[^1];
        var lastPos = last.TranslatePoint(new Point(0, 0), panel);
        if (lastPos.HasValue && cursorY > lastPos.Value.Y + last.Bounds.Height + MARGIN_BOTTOM)
            return panel.Children.Count;
    }
    return -1;
}


private async void MoveGroupToIndex(Border item, int targetIndex)
{
    
    if (item == null) return;
    int currentIndex = GroupsPanel.Children.IndexOf(item);
    if (currentIndex == -1) return;
    string groupName = item.Tag?.ToString() ?? "";
    if (string.IsNullOrEmpty(groupName) || groupName == "Без группы") return;

    if (targetIndex > currentIndex) targetIndex--;

    // 1. Перемещаем в UI
    GroupsPanel.Children.RemoveAt(currentIndex);
    if (targetIndex < GroupsPanel.Children.Count)
        GroupsPanel.Children.Insert(targetIndex, item);
    else
        GroupsPanel.Children.Add(item);

    // 2. Обновляем groupOrder (порядок в памяти)
    var newOrder = new List<string>();
    foreach (var child in GroupsPanel.Children)
    {
        if (child is Border border && border.Tag is string name && name != "Без группы")
            newOrder.Add(name);
    }
    if (!newOrder.Contains("Без группы"))
        newOrder.Insert(0, "Без группы");
    groupOrder = newOrder;

    Console.WriteLine($" MoveGroupToIndex: groupOrder = {string.Join(", ", groupOrder)}");

    // 3. Если переместили текущую группу – обновляем currentPasswords
    if (currentGroup == groupName && allPasswords.ContainsKey(groupName))
    {
        currentPasswords = new List<PasswordEntry>(allPasswords[groupName]);
        UpdatePasswordsList();
    }

    NormalizeIds();

    // 4. Сохраняем изменения (включая порядок) в БД
    // Небольшая задержка, чтобы UI успел обновиться
    await Task.Delay(50);
    SaveAllData();
    HighlightAllGroups(false);
    ShowSuccessFlash($"{FluentIcons.CheckMark} Группа \"{groupName}\" перемещена");
}

}
