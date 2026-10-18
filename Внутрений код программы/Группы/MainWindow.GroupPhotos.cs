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
    // ============ КЛИК ПО ИКОНКЕ ГРУППЫ ============

    private void InitGroupIconDragDrop()
    {
        if (GroupIconBorder != null)
        {
            DragDrop.SetAllowDrop(GroupIconBorder, true);
            GroupIconBorder.AddHandler(DragDrop.DragOverEvent, OnGroupIconDragOver);
            GroupIconBorder.AddHandler(DragDrop.DropEvent, OnGroupIconDrop);
        }
    }

    private void OnGroupIconDragOver(object? sender, DragEventArgs e)
    {
        if (HasImageSource(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            if (GroupIconHint != null)
            {
                GroupIconHint.Text = "Перетаскивай сюда";
                GroupIconHint.IsVisible = true;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private static bool HasImageSource(DragEventArgs e)
    {
        var file = e.DataTransfer.TryGetFile();
        string? filePath = file?.Path?.LocalPath;
        if (!string.IsNullOrEmpty(filePath) && IsImageFile(filePath))
            return true;

        if (e.DataTransfer.TryGetBitmap() != null)
            return true;

        string? text = e.DataTransfer.TryGetText();
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "http" || uri.Scheme == "https"))
            return true;

        string? urlFile = file?.Path?.AbsoluteUri;
        return Uri.TryCreate(urlFile, UriKind.Absolute, out var uri2) &&
               (uri2.Scheme == "http" || uri2.Scheme == "https");
    }

    private static bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".ico" or ".webp" or ".tga" or ".tif" or ".tiff";
    }

    private async void OnGroupIconDrop(object? sender, DragEventArgs e)
    {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup == "Без группы")
        {
            ShowError("Нельзя изменить фото для 'Без группы'!");
            return;
        }

        if (GroupIconHint != null)
            GroupIconHint.IsVisible = false;

        string? path = null;

        var file = e.DataTransfer.TryGetFile();
        string? filePath = file?.Path?.LocalPath;
        if (!string.IsNullOrEmpty(filePath) && IsImageFile(filePath))
        {
            path = filePath;
        }
        else if (e.DataTransfer.TryGetBitmap() is Bitmap bitmap)
        {
            path = SaveBitmapToDisk(bitmap);
            bitmap.Dispose();
        }
        else
        {
            string? text = e.DataTransfer.TryGetText();
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                path = await DownloadImageAsync(uri);
            }
            else
            {
                string? urlFile = file?.Path?.AbsoluteUri;
                if (Uri.TryCreate(urlFile, UriKind.Absolute, out var uri2) &&
                    (uri2.Scheme == "http" || uri2.Scheme == "https"))
                {
                    path = await DownloadImageAsync(uri2);
                }
            }
        }

        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        await ApplyGroupPhotoAsync(path);
    }

    private async Task ApplyGroupPhotoAsync(string newImagePath)
    {
        if (GroupIcon == null) return;

        // Не даём ставить фото, если пользователь уже переключился на «Без группы»
        if (string.IsNullOrEmpty(currentGroup) || currentGroup == "Без группы")
        {
            ShowError("Нельзя изменить фото для 'Без группы'!");
            return;
        }

        try
        {
            if (_gifHelper == null)
                _gifHelper = new GifHelper(GroupIcon);

            await _gifHelper.LoadImageAsync(newImagePath);

            if (GroupIconBorder != null)
            {
                GroupIconBorder.Background = Brushes.Transparent;
                GroupIconBorder.BorderBrush = null;
                GroupIconBorder.BorderThickness = new Thickness(0);
            }

            string destPath = CopyImageToAppData(newImagePath, currentGroup);
            groupImages[currentGroup] = destPath;
            UpdateGroupButtonImage(currentGroup, destPath);
            SaveAllData();

            ShowSuccessFlash($"{FluentIcons.CheckMark} Фото группы \"{currentGroup}\" обновлено!");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка загрузки: {ex.Message}");
        }
    }

    private async void OnGroupIconClick(object? sender, PointerPressedEventArgs e)
    {
        if (string.IsNullOrEmpty(currentGroup) || currentGroup == "Без группы")
        {
            ShowError("Нельзя изменить фото для 'Без группы'!");
            return;
        }

        if (GroupIcon == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите фото для группы",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Изображения")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.ico", "*.webp", "*.tga", "*.tif", "*.tiff" }
                }
            }
        });

        if (files.Count > 0)
        {
            await ApplyGroupPhotoAsync(files[0].Path.LocalPath);
        }
    }

    private static string SaveBitmapToDisk(Bitmap bitmap)
    {
        string dir = Path.Combine(Path.GetTempPath(), "TinaPASS");
        Directory.CreateDirectory(dir);
        string name = $"drop-{DateTime.Now:yyyyMMddHHmmssfff}.png";
        string path = Path.Combine(dir, name);
        bitmap.Save(path);
        return path;
    }

    private static async Task<string?> DownloadImageAsync(Uri uri)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TinaPASS");
            byte[] bytes = await client.GetByteArrayAsync(uri).ConfigureAwait(true);

            string ext = Path.GetExtension(new Uri(uri.ToString().Split('?')[0]).AbsolutePath).ToLower();
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".ico" or ".webp" or ".tga" or ".tif" or ".tiff")) return null;

            string dir = Path.Combine(Path.GetTempPath(), "TinaPASS");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"drop-{DateTime.Now:yyyyMMddHHmmssfff}{ext}");

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                await fs.WriteAsync(bytes).ConfigureAwait(true);

            return path;
        }
        catch
        {
            return null;
        }
    }

    // ============ ОБНОВЛЕНИЕ ФОТО В ГРУППЕ ============

    private async void UpdateGroupButtonImage(string groupName, string? imagePath)
    {
        if (_groupPhotoBorders.TryGetValue(groupName, out var photoBorder))
        {
            try
            {
                bool oldWasGifPowered = _groupGifHelpers.ContainsKey(groupName);
                if (!oldWasGifPowered)
                    DisposeStaticSource(photoBorder.Child);

                if (_groupGifHelpers.TryGetValue(groupName, out var oldHelper))
                {
                    oldHelper.Dispose();
                    _groupGifHelpers.Remove(groupName);
                }

                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    photoBorder.ClipToBounds = true;
                    var image = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        Width = 50,
                        Height = 50
                    };
                    photoBorder.Child = image;

                    if (GifHelper.IsGifFile(imagePath))
                    {
                        var gifHelper = new GifHelper(image);
                        _groupGifHelpers[groupName] = gifHelper;
                        await gifHelper.LoadImageAsync(imagePath);
                    }
                    else
                    {
                        image.Source = new Bitmap(imagePath);
                    }
                }
                else
                {
                    ApplyGroupPhotoPlaceholder(photoBorder, groupName);
                }
            }
            catch
            {
                ApplyGroupPhotoPlaceholder(photoBorder, groupName);
            }
        }
    }

    // Если у группы нет фото — показываем первую букву названия
    private static void ApplyGroupPhotoPlaceholder(Border photoBorder, string groupName)
    {
        DisposeStaticSource(photoBorder.Child);
        photoBorder.Background = new SolidColorBrush(Color.FromRgb(40, 14, 44));
        photoBorder.Child = new TextBlock
        {
            Text = GetGroupInitial(groupName),
            FontFamily = new FontFamily("Inter"),
            FontWeight = FontWeight.Bold,
            FontSize = 22,
            Foreground = Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
    }

    private static string GetGroupInitial(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return "?";
        var trimmed = groupName.TrimStart();
        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(trimmed);
        return enumerator.MoveNext() ? enumerator.GetTextElement().ToUpperInvariant() : "?";
    }
}
