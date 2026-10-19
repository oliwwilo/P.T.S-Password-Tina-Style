using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TinaPASS.Main;

public partial class MainWindow
{
    private void OpenRegistrationButton_Click(object? sender, RoutedEventArgs e)
    {
        editingPassword = null;
        SwitshiControl.SetEditMode(null);
        ShowSwitshiPanel();
    }

    private void OnPasswordSaved(object? sender, PasswordEntry password)
    {
        if (editingPassword == null)
        {
            password.GroupId = currentGroup;
            currentPasswords.Add(password);
        }
        else
        {
            editingPassword.Title = password.Title;
            editingPassword.Email = password.Email;
            editingPassword.Login = password.Login;
            editingPassword.Password = password.Password;
        }

        allPasswords[currentGroup] = new List<PasswordEntry>(currentPasswords);
        UpdatePasswordsList();
        SaveAllData();
        CloseAllPanels();
        ShowSuccessFlash($"✅ Пароль \"{password.Title}\" сохранен!");
    }

    private void OnEditPasswordClick(object? sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var password = button?.Tag as PasswordEntry;
        if (password != null)
        {
            OnPasswordCardClick(password);
        }
    }

    private async void OnCopyPasswordClick(object? sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var password = button?.Tag as PasswordEntry;
        if (password == null) return;

        string text = string.IsNullOrWhiteSpace(password.Password)
            ? password.Login
            : password.Password;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                await topLevel.Clipboard.SetTextAsync(text);
                ShowSuccessFlash(text.Length <= 30 ? $"✅ Скопировано: {text}" : "✅ Пароль скопирован!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка копирования: {ex.Message}");
        }
    }

    private void OnPasswordCardClick(PasswordEntry password)
    {
        editingPassword = password;
        SwitshiControl.SetEditMode(password);
        ShowSwitshiPanel();
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var password = button?.Tag as PasswordEntry;

        if (password == null) return;

        ShowConfirmation($"Удалить \"{password.Title}\"?", () =>
        {
            currentPasswords.Remove(password);
            allPasswords[currentGroup] = new List<PasswordEntry>(currentPasswords);
            UpdatePasswordsList();
            SaveAllData();
            ShowSuccessFlash($"✅ Пароль \"{password.Title}\" удален!");
        });
    }

    private void OnClearAllClick(object? sender, RoutedEventArgs e)
    {
        if (currentGroup != "Без группы")
        {
            ShowError($"{FluentIcons.Warning} Очистить можно только группу 'Без группы'!");
            return;
        }

        if (currentPasswords.Count == 0)
        {
            ShowError($"{FluentIcons.Warning} В группе нет паролей!");
            return;
        }

        int count = currentPasswords.Count;
        ShowConfirmation($"Очистить все {count} паролей?", () =>
        {
            currentPasswords.Clear();
            allPasswords[currentGroup] = new List<PasswordEntry>();
            UpdatePasswordsList();
            SaveAllData();
            ShowSuccessFlash($"{FluentIcons.CheckMark} Удалено паролей: {count}");
        });
    }

    private void OnGroupNameChanged(object? sender, TextChangedEventArgs e)
    {
        if (GroupNameEditor == null) return;

        string newName = GroupNameEditor.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(newName) || newName == currentGroup) return;

        if (currentGroup == "Без группы")
        {
            ShowError("Нельзя переименовать системную группу!");
            GroupNameEditor.Text = currentGroup;
            return;
        }

        if (allPasswords.ContainsKey(newName))
        {
            ShowError($"Группа \"{newName}\" уже существует!");
            GroupNameEditor.Text = currentGroup;
            return;
        }

        var passwords = allPasswords[currentGroup];
        var image = groupImages.ContainsKey(currentGroup) ? groupImages[currentGroup] : null;

        allPasswords.Remove(currentGroup);
        allPasswords[newName] = passwords;

        if (image != null)
        {
            groupImages.Remove(currentGroup);
            groupImages[newName] = image;
        }

        // Обновляем кнопку в левой панели
        foreach (var container in GroupsPanel.Children)
        {
            if (container is Border border && border.Tag?.ToString() == currentGroup)
            {
                border.Tag = newName;
                if (border.Child is Grid innerGrid)
                {
                    foreach (var child in innerGrid.Children)
                    {
                        if (child is TextBlock text)
                        {
                            text.Text = newName;
                            break;
                        }
                    }
                }
                break;
            }
        }

        string oldName = currentGroup;
        currentGroup = newName;
        SaveAllData();
        ShowSuccessFlash($"✅ Группа переименована в \"{newName}\"");
    }

    private void OnGroupNameEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            AddPassword?.Focus();
    }

    private void UpdatePasswordsList()
    {
        if (PasswordsContainer == null) return;
        PasswordsContainer.Children.Clear();

        if (currentPasswords.Count == 0)
        {
            var emptyText = FluentIcons.Build(
                currentGroup == "Без группы"
                    ? $"{FluentIcons.Folder} Системная папка\nЗдесь будут отображаться пароли без группы"
                    : $"{FluentIcons.Inbox} Нет сохраненных паролей",
                14,
                Brushes.White,
                TextWrapping.Wrap);
            emptyText.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            emptyText.Margin = new Thickness(0, 20, 0, 0);
            emptyText.TextAlignment = Avalonia.Media.TextAlignment.Center;
            PasswordsContainer.Children.Add(emptyText);
            return;
        }

        int number = 1;
        foreach (var password in currentPasswords)
        {
            var card = CreatePasswordCard(password, number);
            if (card != null)
            {
                PasswordsContainer.Children.Add(card);
                number++;
            }
        }
    }

    private Border? CreatePasswordCard(PasswordEntry password, int number)
    {
        try
        {
            if (password == null) return null;

            string title = string.IsNullOrWhiteSpace(password.Title) ? "Без названия" : password.Title;
            string rawPassword = password.Password ?? "";
            string login = password.Login ?? "";
            string email = password.Email ?? "";
            string description = password.Description ?? "";

            var card = new Border
            {
                Background = Brush.Parse("#33000000"),
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10),
                Tag = password,
                Transitions = new Transitions
                {
                    new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(120) },
                    new DoubleTransition { Property = Layoutable.HeightProperty, Duration = TimeSpan.FromMilliseconds(140) }
                },
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            card.PointerPressed += (s, e) =>
            {
                if (IsPressOnButton(e.Source, card)) return;

                if (!_isSelectionMode)
                {
                    _dragStartPoint = e.GetPosition(DragLayer);
                    _dragGrabOffset = e.GetPosition(card);
                    _dragPassword = password;
                    _isDraggingPassword = false;
                }
            };

            card.PointerMoved += (s, e) =>
            {
                if (IsPressOnButton(e.Source, card)) return;

                if (!_isSelectionMode && e.GetCurrentPoint(card).Properties.IsLeftButtonPressed)
                {
                    var position = e.GetPosition(DragLayer);
                    if (_isDraggingPassword)
                    {
                        UpdatePasswordDrag(e);
                    }
                    else
                    {
                        double dx = position.X - _dragStartPoint.X;
                        double dy = position.Y - _dragStartPoint.Y;
                        if (Math.Sqrt(dx * dx + dy * dy) > 8)
                        {
                            BeginPasswordDrag(card, password, number, e);
                            UpdatePasswordDrag(e);
                        }
                    }
                }
            };

            card.PointerReleased += (s, e) =>
            {
                if (IsPressOnButton(e.Source, card)) return;

                if (!_isSelectionMode)
                {
                    if (_isDraggingPassword)
                        FinishPasswordDrag(e);
                    else
                        OnPasswordCardClick(password);
                }
            };

            card.PointerCaptureLost += (s, e) =>
            {
                if (_isDraggingPassword)
                    CancelPasswordDrag();
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                Margin = new Thickness(18, 12, 18, 12)
            };

            var numberBadge = new Border
            {
                Background = Brush.Parse("#1A22C55E"),
                BorderBrush = Brush.Parse("#86EFAC"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            numberBadge.Child = new TextBlock
            {
                Text = number.ToString(),
                FontSize = 13,
                FontWeight = FontWeight.Medium,
                Foreground = Brush.Parse("#86EFAC"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            if (!string.IsNullOrWhiteSpace(login))
            {
                var loginText = FluentIcons.Build($"{FluentIcons.Contact} Логин: {login}", 12, Brushes.White);
                loginText.Margin = new Thickness(0, 3, 0, 0);
                loginText.Tag = "login";
                info.Children.Add(loginText);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailText = FluentIcons.Build($"{FluentIcons.Mail} Почта: {new string('●', email.Length)}", 12, Brushes.White);
                emailText.Margin = new Thickness(0, 3, 0, 0);
                emailText.Tag = "email";
                info.Children.Add(emailText);
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                var descText = FluentIcons.Build($"{FluentIcons.Document} {description}", 12, Brushes.White);
                descText.Margin = new Thickness(0, 3, 0, 0);
                descText.TextWrapping = TextWrapping.Wrap;
                descText.Tag = "desc";
                info.Children.Add(descText);
            }

            var passwordText = FluentIcons.Build(
                string.IsNullOrEmpty(rawPassword)
                    ? $"{FluentIcons.Lock} Пароль не указан"
                    : $"{FluentIcons.Lock} Пароль: {new string('●', rawPassword.Length)}",
                12, Brushes.White);
            passwordText.Margin = new Thickness(0, 3, 0, 0);
            passwordText.Tag = password;
            info.Children.Add(passwordText);

            var deleteButton = new Button
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(10),
                Width = 36,
                Height = 36,
                Tag = password,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            deleteButton.Click += OnDeleteClick;
            deleteButton.Styles.Add(new Style(x => x.OfType<Button>())
            {
                Setters =
                {
                    new Setter(Button.ForegroundProperty, Brush.Parse("#CFCFCF")),
                    new Setter(Button.TransitionsProperty, new Transitions
                    {
                        new BrushTransition
                        {
                            Property = Button.BackgroundProperty,
                            Duration = TimeSpan.FromMilliseconds(150)
                        },
                        new BrushTransition
                        {
                            Property = Button.ForegroundProperty,
                            Duration = TimeSpan.FromMilliseconds(150)
                        }
                    })
                }
            });
            deleteButton.Styles.Add(new Style(x => x.OfType<Button>().Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Brush.Parse("#1AEF4444")),
                    new Setter(Button.ForegroundProperty, Brush.Parse("#FCA5A5"))
                }
            });
            deleteButton.Styles.Add(new Style(x => x.OfType<Button>().Class(":pressed"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Brush.Parse("#33EF4444")),
                    new Setter(Button.ForegroundProperty, Brush.Parse("#FCA5A5"))
                }
            });
            var path = new Avalonia.Controls.Shapes.Path
            {
                Data = Avalonia.Media.Geometry.Parse("M10 11v6 M14 11v6 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6 M3 6h18 M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"),
                Fill = Brushes.Transparent,
                Stroke = deleteButton.Foreground,
                StrokeThickness = 2,
                StrokeLineCap = PenLineCap.Round,
                StrokeJoin = PenLineJoin.Round
            };
            path.Bind(Avalonia.Controls.Shapes.Path.StrokeProperty, deleteButton.GetObservable(Button.ForegroundProperty));
            var icon = new Viewbox
            {
                Width = 20,
                Height = 20,
                Child = path
            };
            deleteButton.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(-2, 8, 0, 8),
                Children = { icon }
            };

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            actions.Children.Add(deleteButton);

            grid.Children.Add(numberBadge);
            grid.Children.Add(info);
            grid.Children.Add(actions);
            Grid.SetColumn(numberBadge, 0);
            Grid.SetColumn(info, 1);
            Grid.SetColumn(actions, 2);

            card.Child = grid;
            return card;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка создания карточки: {ex.Message}");
            return null;
        }
    }

    private static bool IsPressOnButton(object? source, Control card)
    {
        var node = source as Control;
        while (node != null && node != card)
        {
            if (node is Button) return true;
            node = node.Parent as Control;
        }
        return false;
    }

    private void UpdateCardNumbers()
    {
        int number = 1;
        foreach (var child in PasswordsContainer.Children)
        {
            if (child is Border { Child: Grid grid })
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is Border { Child: TextBlock text, Width: 28, Height: 28 })
                    {
                        text.Text = number.ToString();
                        break;
                    }
                }
                number++;
            }
        }
    }
}
