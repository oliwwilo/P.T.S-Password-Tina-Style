using Avalonia;
using Avalonia.Animation;
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
    // ============ ПЕРЕТАСКИВАНИЕ ПАРОЛЯ: «ВЫТЯГИВАНИЕ» КАРТОЧКИ / ПЕРЕСТАНОВКА / ПЕРЕНОС В ГРУППУ ============

    private Point _dragLastPointInList;             // относительно PasswordsContainer (контент списка)
    private Point _dragLastPointInListViewport;     // относительно Skrolbar1 (вьюпорт)
    private Border? _dragCard;                      // карточка, из которой «вытянули» пароль
    private PasswordEntry? _dragPassword;
    private Border? _dragPreview;                   // летящая копия карточки на DragLayer
    private Control? _dragSlotContent;             // контент, спрятанный из исходной карточки
    private Point _dragGrabOffset;                  // точка захвата внутри карточки
    private double _dragOriginalWidth;
    private double _dragOriginalHeight;
    private Border? _dragInsertTarget;              // карточка, перед которой вставим

    private void BeginPasswordDrag(Border card, PasswordEntry password, int number, PointerEventArgs e)
    {
        _isDraggingPassword = true;
        _dragCard = card;
        _dragPassword = password;
        _dragSlotContent = card.Child;
        _dragOriginalWidth = card.Bounds.Width;
        _dragOriginalHeight = card.Bounds.Height;

        // «Вытягиваем» карточку: прячем контент, слот плавно схлопывается, список смыкается
        if (_dragOriginalHeight > 0)
        {
            card.Height = _dragOriginalHeight;
            card.Child = null;
            card.Height = 0;
        }
        else
        {
            card.Child = null;
        }

        // Летящая копия на верхнем слое
        _dragPreview = CreatePasswordCard(password, number);
        if (_dragPreview != null)
        {
            _dragPreview.Width = _dragOriginalWidth;
            _dragPreview.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            _dragPreview.Margin = new Thickness(0);
            _dragPreview.IsHitTestVisible = false;
            _dragPreview.Opacity = 0.95;
            DragLayer.Children.Add(_dragPreview);
            PositionDragPreview(e.GetPosition(DragLayer));
        }
    }

    private void UpdatePasswordDrag(PointerEventArgs e)
    {
        _dragLastPointViewport = e.GetPosition(GroupsScrollViewer);
        _dragLastPointInList = e.GetPosition(PasswordsContainer);
        _dragLastPointInListViewport = e.GetPosition(Skrolbar1);

        UpdateDragAutoScroll();

        if (_dragPreview != null)
            PositionDragPreview(e.GetPosition(DragLayer));

        UpdateDragTarget();

        if (_dragTargetGroup == null)
            UpdatePasswordInsertTarget(_dragLastPointInList.Y);
        else
            ClearPasswordInsertTarget();
    }

    private void PositionDragPreview(Point pointOnLayer)
    {
        if (_dragPreview == null) return;
        Canvas.SetLeft(_dragPreview, pointOnLayer.X - _dragGrabOffset.X);
        Canvas.SetTop(_dragPreview, pointOnLayer.Y - _dragGrabOffset.Y);
    }

    private void UpdateDragAutoScroll()
    {
        const double edgeZone = 40;

        double gy = _dragLastPointViewport.Y;
        double ly = _dragLastPointInListViewport.Y;

        bool groupZone = gy < edgeZone || gy > GroupsScrollViewer.Viewport.Height - edgeZone;
        bool listZone = ly < edgeZone || ly > Skrolbar1.Viewport.Height - edgeZone;

        if (!groupZone && !listZone)
        {
            _dragScrollTimer?.Stop();
            return;
        }

        if (_dragScrollTimer == null)
        {
            _dragScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15)
            };
            _dragScrollTimer.Tick += OnDragScrollTick;
        }

        if (!_dragScrollTimer.IsEnabled)
            _dragScrollTimer.Start();
    }

    private void OnDragScrollTick(object? sender, EventArgs e)
    {
        if (!_isDraggingPassword)
        {
            _dragScrollTimer?.Stop();
            return;
        }

        const double edgeZone = 40;
        const double step = 8;

        double gy = _dragLastPointViewport.Y;

        if (gy < edgeZone)
        {
            double newY = Math.Max(0, GroupsScrollViewer.Offset.Y - step);
            GroupsScrollViewer.Offset = GroupsScrollViewer.Offset.WithY(newY);
        }
        else if (gy > GroupsScrollViewer.Viewport.Height - edgeZone)
        {
            double newY = GroupsScrollViewer.Offset.Y + step;
            GroupsScrollViewer.Offset = GroupsScrollViewer.Offset.WithY(newY);
        }

        double ly = _dragLastPointInListViewport.Y;

        if (ly < edgeZone && Skrolbar1.Offset.Y > 0)
        {
            double newY = Math.Max(0, Skrolbar1.Offset.Y - step);
            Skrolbar1.Offset = Skrolbar1.Offset.WithY(newY);
        }
        else if (ly > Skrolbar1.Viewport.Height - edgeZone)
        {
            double newY = Skrolbar1.Offset.Y + step;
            Skrolbar1.Offset = Skrolbar1.Offset.WithY(newY);
        }

        UpdateDragTarget();

        if (_dragTargetGroup == null)
            UpdatePasswordInsertTarget(_dragLastPointInList.Y);
        else
            ClearPasswordInsertTarget();
    }

    // ---- Поиск места вставки (без визуальной линии) ----

    private void UpdatePasswordInsertTarget(double pointY)
    {
        Border? target = null;
        foreach (var child in PasswordsContainer.Children)
        {
            if (child is not Border border) continue;
            if (border == _dragCard) continue;
            if (border.Tag is not PasswordEntry) continue;

            double mid = border.Bounds.Y + border.Bounds.Height / 2;
            if (pointY < mid) { target = border; break; }
        }

        _dragInsertTarget = target;
    }

    private void ClearPasswordInsertTarget()
    {
        _dragInsertTarget = null;
    }

    private void RestoreDragCard()
    {
        if (_dragCard == null) return;
        if (_dragOriginalHeight > 0)
        {
            _dragCard.Child = _dragSlotContent;
            _dragCard.Height = _dragOriginalHeight;
        }
        else
        {
            _dragCard.Child = _dragSlotContent;
        }
        _dragCard.IsHitTestVisible = true;
        _dragCard.Opacity = 1;
    }

    private void FinishPasswordDrag(PointerEventArgs e)
    {
        if (_dragCard == null || _dragPassword == null) { CancelPasswordDrag(); return; }

        string? targetGroup = GetGroupNameAtPoint(e.GetPosition(GroupsScrollViewer));
        var insertTarget = _dragInsertTarget; // сохраняем ДО сброса

        ClearPasswordInsertTarget();
        ResetPasswordDragHighlight();

        if (!string.IsNullOrEmpty(targetGroup) && targetGroup != currentGroup)
        {
            // Перенос в группу: превью плавно гаснет
            var password = _dragPassword;
            var dragCard = _dragCard;
            var slotContent = _dragSlotContent;
            double origHeight = _dragOriginalHeight;
            _isDraggingPassword = false;
            _dragCard = null; _dragPassword = null; _dragSlotContent = null;

            FadeOutDragPreview(() =>
            {
                if (dragCard.Child == null)
                {
                    dragCard.Child = slotContent;
                    if (origHeight > 0) dragCard.Height = origHeight;
                    dragCard.IsHitTestVisible = true;
                    dragCard.Opacity = 1;
                }
                MoveSinglePasswordToGroup(password, targetGroup);
            });
            return;
        }

        // Перестановка в списке: превью плавно перелетает в слот и «вливается» в список
        var listCard = _dragCard;
        var listPassword = _dragPassword;
        var listContent = _dragSlotContent;
        double listHeight = _dragOriginalHeight;
        _isDraggingPassword = false;
        _dragCard = null; _dragPassword = null; _dragSlotContent = null; _dragInsertTarget = null;

        double slotY = CalcInsertSlotY(insertTarget, listCard);
        var origin = PasswordsContainer.TranslatePoint(new Point(0, slotY), DragLayer);
        double targetX = origin?.X ?? 0;
        double targetY = origin?.Y ?? 0;

        FlyPreviewIntoSlot(targetX, targetY, () =>
        {
            PasswordsContainer.Children.Remove(listCard);
            int insertAt = PasswordsContainer.Children.Count;
            if (insertTarget != null)
            {
                int ti = PasswordsContainer.Children.IndexOf(insertTarget);
                if (ti >= 0) insertAt = ti;
            }
            PasswordsContainer.Children.Insert(Math.Min(insertAt, PasswordsContainer.Children.Count), listCard);

            listCard.Child = listContent;
            if (listHeight > 0) listCard.Height = listHeight;
            listCard.IsHitTestVisible = true;
            listCard.Opacity = 1;

            ReorderPasswordInGroup(listPassword, insertTarget?.Tag as PasswordEntry);
            UpdateCardNumbers();
        });
    }

    private double CalcInsertSlotY(Border? insertTarget, Border dragCard)
    {
        if (insertTarget != null)
            return insertTarget.Bounds.Y;

        Border? last = null;
        foreach (var child in PasswordsContainer.Children)
        {
            if (child is not Border border) continue;
            if (border == dragCard) continue;
            if (border.Tag is not PasswordEntry) continue;
            last = border;
        }

        if (last != null)
            return last.Bounds.Y + last.Bounds.Height + last.Margin.Bottom;

        return 0;
    }

    private void FlyPreviewIntoSlot(double x, double y, Action onDone)
    {
        if (_dragPreview == null) { SafeFinishDrag(onDone); return; }

        var preview = _dragPreview;
        double fromX = Canvas.GetLeft(preview);
        double fromY = Canvas.GetTop(preview);
        double fromOpacity = preview.Opacity;
        const double durationMs = 220;
        const double fadeTo = 0.55;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        EventHandler handler = null!;
        handler = (_, _) =>
        {
            double t = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / durationMs);
            double ease = 1 - Math.Pow(1 - t, 3);
            Canvas.SetLeft(preview, fromX + (x - fromX) * ease);
            Canvas.SetTop(preview, fromY + (y - fromY) * ease);
            preview.Opacity = fromOpacity + (fadeTo - fromOpacity) * ease;

            if (t >= 1.0)
            {
                timer.Tick -= handler;
                stopwatch.Stop();
                DragLayer.Children.Remove(preview);
                _dragPreview = null;
                SafeFinishDrag(onDone);
            }
        };
        timer.Tick += handler;
        timer.Start();
    }

    private void FadeOutDragPreview(Action onDone)
    {
        if (_dragPreview == null) { SafeFinishDrag(onDone); return; }

        var preview = _dragPreview;
        double fromOpacity = preview.Opacity;
        const double durationMs = 150;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        EventHandler handler = null!;
        handler = (_, _) =>
        {
            double t = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / durationMs);
            preview.Opacity = fromOpacity * (1 - t);

            if (t >= 1.0)
            {
                timer.Tick -= handler;
                stopwatch.Stop();
                DragLayer.Children.Remove(preview);
                _dragPreview = null;
                SafeFinishDrag(onDone);
            }
        };
        timer.Tick += handler;
        timer.Start();
    }

    private void SafeFinishDrag(Action onDone)
    {
        try
        {
            onDone();
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка завершения перетаскивания: {ex.Message}");
        }
    }

    private void CancelPasswordDrag()
    {
        if (!_isDraggingPassword) return;

        ClearPasswordInsertTarget();
        ResetPasswordDragHighlight();
        if (_dragPreview != null) { DragLayer.Children.Remove(_dragPreview); _dragPreview = null; }

        RestoreDragCard();

        _isDraggingPassword = false;
        _dragCard = null;
        _dragPassword = null;
        _dragSlotContent = null;
    }

    private void ReorderPasswordInGroup(PasswordEntry password, PasswordEntry? target)
    {
        try
        {
            if (!allPasswords.ContainsKey(currentGroup)) return;

            var fullList = allPasswords[currentGroup];
            int from = fullList.IndexOf(password);
            if (from < 0) return;

            fullList.Remove(password);

            int to;
            if (target != null)
            {
                int t = fullList.IndexOf(target);
                to = t < 0 ? fullList.Count : t;
            }
            else
            {
                to = fullList.Count;
            }

            if (to == from)
            {
                fullList.Insert(from, password);
                return;
            }

            fullList.Insert(to, password);

            // Синхронизируем currentPasswords (как ApplySearchFilter, но без перерисовки списка)
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                currentPasswords = new List<PasswordEntry>(fullList);
            }
            else
            {
                currentPasswords = fullList
                    .Where(p => !string.IsNullOrEmpty(p.Title) &&
                                p.Title.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            SaveAllData();
            ShowSuccessFlash($"{FluentIcons.Sync} Порядок паролей сохранён");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка перестановки паролей: {ex.Message}");
        }
    }

    private Point GetDragContentPoint()
    {
        return _dragLastPointViewport;
    }

    private void UpdateDragTarget()
    {
        Point point = GetDragContentPoint();
        string? targetGroup = GetGroupNameAtPoint(point);

        if (_dragTargetGroup == targetGroup) return;

        ResetPasswordDragHighlight();

        _dragTargetGroup = targetGroup;

        if (string.IsNullOrEmpty(targetGroup)) return;

        foreach (var child in GroupsPanel.Children)
        {
            if (child is Border border && border.Tag?.ToString() == targetGroup)
            {
                _dragTargetBorder = border;
                border.Opacity = 0.85;
                border.BorderBrush = Brush.Parse("#69DBFE");
                border.BorderThickness = new Thickness(2);
                break;
            }
        }
    }

    private void ResetPasswordDragHighlight()
    {
        _dragScrollTimer?.Stop();
        if (_dragTargetBorder != null)
        {
            _dragTargetBorder.Opacity = 1;
            _dragTargetBorder.BorderBrush = null;
            _dragTargetBorder.BorderThickness = new Thickness(0);
            _dragTargetBorder = null;
        }
        _dragTargetGroup = null;
    }

    private string? GetGroupNameAtPoint(Point point)
    {
        foreach (var child in GroupsPanel.Children)
        {
            if (child is not Border border) continue;
            if (border.Tag is not string groupName) continue;

            var pos = border.TranslatePoint(new Point(0, 0), GroupsScrollViewer);
            if (!pos.HasValue) continue;

            double left = pos.Value.X;
            double right = left + border.Bounds.Width;
            double top = pos.Value.Y;
            double bottom = top + border.Bounds.Height;

            if (point.X >= left && point.X <= right &&
                point.Y >= top && point.Y <= bottom)
                return groupName;
        }
        return null;
    }

    private void MoveSinglePasswordToGroup(PasswordEntry password, string targetGroupName)
    {
        if (password == null) return;
        if (targetGroupName == currentGroup) return;

        string sourceGroup = currentGroup;

        if (allPasswords.ContainsKey(sourceGroup))
            allPasswords[sourceGroup].Remove(password);
        currentPasswords.Remove(password);

        if (!allPasswords.ContainsKey(targetGroupName))
            allPasswords[targetGroupName] = new List<PasswordEntry>();
        allPasswords[targetGroupName].Add(password);

        SaveAllData();
        ApplySearchFilter();
        UpdatePasswordsList();

        ShowSuccessFlash($"{FluentIcons.CheckMark} Пароль \"{password.Title}\" перемещён в \"{targetGroupName}\"");
    }

    // ============ ПЕРЕМЕЩЕНИЕ ВЫДЕЛЕННЫХ ПАРОЛЕЙ ============

  private async void MoveSelectedPasswordsToGroup(string targetGroupName)
{
    if (!_selectedPasswords.Any()) return;
    if (_isMovingPasswords) { ShowToast($"{FluentIcons.Sync} Идет перемещение, подождите..."); return; }
    if (targetGroupName == currentGroup) { ShowToast($"{FluentIcons.Info} Пароли уже в этой группе"); ClearSelection(); return; }

    if (!allPasswords.ContainsKey(targetGroupName))
        allPasswords[targetGroupName] = new List<PasswordEntry>();

    try
    {
        _isMovingPasswords = true;
        int count = _selectedPasswords.Count;
        string sourceGroup = currentGroup;
        var passwordsToMove = _selectedPasswords.ToList();

        // ---- 1. Обновляем структуры в памяти ----
        foreach (var password in passwordsToMove)
        {
            // Удаляем из исходной группы
            if (allPasswords.ContainsKey(sourceGroup))
            {
                allPasswords[sourceGroup].Remove(password);
                currentPasswords.Remove(password);
            }

            // Добавляем в целевую группу
            allPasswords[targetGroupName].Add(password);
        }

        // ---- 2. Сохраняем изменения в БД (только один раз!) ----
        // Вместо удаления/вставки по одному паролю – просто пересохраняем все данные.
        // Это проще и быстрее, чем делать много операций с БД.
        // Но если паролей очень много, можно оптимизировать, но пока оставим так.
        SaveAllData(); // это перезапишет всю БД, но у нас уже есть транзакции и повторные попытки

        // ---- 3. Переключаемся на целевую группу (без сохранения) ----
        // Для этого нужно убедиться, что SwitchToGroup НЕ вызывает SaveAllData().
        // Если он вызывает – уберите этот вызов.
        ClearSelection();
        SwitchToGroup(targetGroupName); // внутри обновит UI, но не должен сохранять
        UpdatePasswordsList();
        // ---- 4. Уведомление ----
        string fromGroup = sourceGroup == "Без группы" ? "Без группы" : sourceGroup;
        string toGroup = targetGroupName == "Без группы" ? "Без группы" : targetGroupName;
        ShowSuccessFlash($"{FluentIcons.CheckMark} {count} паролей перемещено из \"{fromGroup}\" в \"{toGroup}\"");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при перемещении паролей: {ex.Message}");
        ShowError($"Ошибка перемещения: {ex.Message}");
    }
    finally
    {
        _isMovingPasswords = false;
    }
}

    

}
