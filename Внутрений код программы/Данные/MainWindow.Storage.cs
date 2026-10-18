using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Animation; // Для анимации
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity; // Дл
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace TinaPASS.Main;

public partial class MainWindow
{
    private GifHelper? _gifHelper;
    // ============ ОСТАНОВКА АНИМАЦИИ GIF ============
    private void StopGifAnimation()
    {
        if (_gifCancellationToken != null)
        {
            _gifCancellationToken.Cancel();
            _gifCancellationToken.Dispose();
            _gifCancellationToken = null;
        }
    }
    private async void LoadGifImage(string gifPath)
    {
        try
        {
            // Читаем GIF в память
            byte[] gifData = File.ReadAllBytes(gifPath);
            
            // Создаем декодер
            using (var stream = new MemoryStream(gifData))
            {
                //  ИСПОЛЬЗУЕМ Bitmap ДЛЯ ЗАГРУЗКИ GIF
                var bitmap = new Bitmap(stream);
                
                // Показываем первый кадр
                GroupIcon.Source = bitmap;
                GroupIcon.IsVisible = true;
                if (GroupIconInitial != null) GroupIconInitial.IsVisible = false;
                
                if (GroupIconBorder != null)
                {
                    GroupIconBorder.Background = Brushes.Transparent;
                    GroupIconBorder.BorderBrush = null;
                    GroupIconBorder.BorderThickness = new Thickness(0);
                }
                
                //  ЕСЛИ НУЖНА АНИМАЦИЯ - ИСПОЛЬЗУЕМ ПРОСТОЙ ТАЙМЕР
                // (Avalonia не поддерживает анимацию GIF из коробки)
                // Можно использовать DispatcherTimer для имитации анимации
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки GIF: {ex.Message}");
            SetGrayBackground();
        }
    }
    // ============ УДАЛЕНИЕ ИКОНКИ ГРУППЫ ============
private void OnRemoveIconClick(object? sender, RoutedEventArgs e)
{
    if (string.IsNullOrEmpty(currentGroup) || currentGroup == "Без группы")
    {
        ShowError("Нельзя удалить фото для 'Без группы'!");
        return;
    }

    // Подтверждение удаления
    ShowConfirmation($"Удалить изображение для группы \"{currentGroup}\"?", () =>
    {
        try
        {
            // Удаляем из словаря
            if (groupImages.ContainsKey(currentGroup))
            {
                // Удаляем физический файл
                string? imagePath = groupImages[currentGroup];
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    try
                    {
                        File.Delete(imagePath);
                        Console.WriteLine($" Удален файл: {imagePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка удаления файла: {ex.Message}");
                    }
                }
                
                groupImages.Remove(currentGroup);
            }

            // Очищаем иконку в UI — показываем букву группы
            SetGrayBackground();

            // Скрываем кнопку удаления

            // Обновляем кнопку группы в списке
            //    UpdateGroupButtonImage(currentGroup, null);

            // Сохраняем изменения
            SaveAllData();
            
            ShowSuccessFlash($"{FluentIcons.CheckMark} Изображение группы \"{currentGroup}\" удалено!");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка удаления: {ex.Message}");
        }
    });
}
    // ============ ЗАГРУЗКА ИКОНКИ ГРУППЫ ============
    private async void LoadGroupIcon(string groupName)
    {
        if (GroupIcon == null) return;

        // В «Без группы» фото не показываем вообще (и отменяем фоновую загрузку)
        if (string.IsNullOrEmpty(groupName) || groupName == "Без группы")
        {
            HideGroupIcon();
            return;
        }

        if (_gifHelper == null)
            _gifHelper = new GifHelper(GroupIcon);
    
        if (groupImages.TryGetValue(groupName, out string? imagePath) && !string.IsNullOrEmpty(imagePath))
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    await _gifHelper.LoadImageAsync(imagePath);
                
                    if (GroupIconBorder != null)
                    {
                        GroupIconBorder.Background = Brushes.Transparent;
                        GroupIconBorder.BorderBrush = null;
                        GroupIconBorder.BorderThickness = new Thickness(0);
                        GroupIconBorder.IsVisible = true;
                    }

                    if (GroupIconInitial != null) GroupIconInitial.IsVisible = false;

                    //  ПОКАЗЫВАЕМ КНОПКУ УДАЛЕНИ
                
                    return;
                }
            }
            catch
            {
                SetGrayBackground();
                return;
            }
        }

        SetGrayBackground();
    }

    
    // ============ СКРЫТИЕ ИКОНКИ ГРУППЫ («Без группы») ============
    private void HideGroupIcon()
    {
        // Останавливаем анимацию и отменяем незавершённые загрузки,
        // иначе завершившийся в фоне GIF снова покажет картинку
        _gifHelper?.CancelAndClear();

        if (GroupIcon != null)
        {
            GroupIcon.IsVisible = false;
            GroupIcon.Source = null;
        }

        if (GroupIconBorder != null)
            GroupIconBorder.IsVisible = false;

        if (GroupIconInitial != null)
            GroupIconInitial.IsVisible = false;

        if (GroupNameEditor != null)
            GroupNameEditor.IsVisible = false;
    }

    private void SetGrayBackground()
    {
        if (GroupIcon != null)
        {
            GroupIcon.IsVisible = false;
            GroupIcon.Source = null;
        }

        if (GroupIconInitial != null)
        {
            GroupIconInitial.Text = GetGroupInitial(currentGroup);
            GroupIconInitial.IsVisible = true;
        }

        if (GroupIconBorder != null)
        {
            GroupIconBorder.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            GroupIconBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            GroupIconBorder.BorderThickness = new Thickness(1);
            GroupIconBorder.IsVisible = true;
        }
    }

    // ============ КОПИРОВАНИЕ ФОТО В ПАПКУ ПРИЛОЖЕНИЯ ============
    private static readonly Dictionary<string, string> _imageHashCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private string CopyImageToAppData(string sourcePath, string groupName)
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TinaPASS", "Images");

        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        // Если файл с таким же содержимым уже есть в папке приложения -
        // не плодим копию, а переиспользуем существующий
        string sourceHash = ComputeFileHash(sourcePath);
        if (!string.IsNullOrEmpty(sourceHash))
        {
            string? existing = FindImageByHash(appDataPath, sourceHash);
            if (!string.IsNullOrEmpty(existing))
            {
                Console.WriteLine($" CopyImageToAppData: дубликат найден, переиспользуем {existing}");
                return existing;
            }
        }

        string extension = Path.GetExtension(sourcePath);
        string fileName = $"{groupName}_{DateTime.Now.Ticks}{extension}";
        string destPath = Path.Combine(appDataPath, fileName);

        File.Copy(sourcePath, destPath, true);
        if (!string.IsNullOrEmpty(sourceHash))
            _imageHashCache[sourceHash] = destPath;
        return destPath;
    }

    private static string ComputeFileHash(string path)
    {
        try
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(fs));
        }
        catch
        {
            return "";
        }
    }

    private string? FindImageByHash(string appDataPath, string hash)
    {
        if (_imageHashCache.TryGetValue(hash, out var known) && File.Exists(known))
            return known;

        foreach (string file in Directory.GetFiles(appDataPath, "*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (string.Equals(ComputeFileHash(file), hash, StringComparison.OrdinalIgnoreCase))
                {
                    _imageHashCache[hash] = file;
                    return file;
                }
            }
            catch { }
        }
        return null;
    }

    // ============ УДАЛЕНИЕ ДУБЛИКАТОВ ИЗОБРАЖЕНИЙ ============
    private async void DeduplicateImages()
    {
        try
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TinaPASS", "Images");
            if (!Directory.Exists(appDataPath)) return;

            var result = await Task.Run(() =>
            {
                var files = Directory.GetFiles(appDataPath, "*", SearchOption.TopDirectoryOnly);
                if (files.Length < 2) return (remap: new Dictionary<string, string>(), toDelete: new List<string>());

                var snapshot = groupImages.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

                // Хэш содержимого -> файл-представитель
                var repByHash = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    string hash = ComputeFileHash(file);
                    if (!string.IsNullOrEmpty(hash))
                        repByHash.TryAdd(hash, file);
                }

                // Группы, чьи файлы продублированы -> перевешиваем на представителя
                var remap = new Dictionary<string, string>();
                foreach (var group in snapshot.Keys)
                {
                    var path = snapshot[group];
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                    string hash = ComputeFileHash(path);
                    if (repByHash.TryGetValue(hash, out var rep) &&
                        !string.Equals(rep, path, StringComparison.OrdinalIgnoreCase))
                        remap[group] = rep;
                }

                // После ремапа - какие файлы реально нужны
                var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in snapshot.Keys)
                {
                    var path = remap.TryGetValue(group, out var rep) ? rep : snapshot[group];
                    if (!string.IsNullOrEmpty(path)) keep.Add(path);
                }

                // Хэши нужных файлов
                var usedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in keep)
                {
                    string hash = ComputeFileHash(file);
                    if (!string.IsNullOrEmpty(hash)) usedHashes.Add(hash);
                }

                // Неиспользуемые файлы, чьё содержимое продублировано - под удаление
                var toDelete = new List<string>();
                foreach (var file in files)
                {
                    if (keep.Contains(file)) continue;
                    string hash = ComputeFileHash(file);
                    if (!string.IsNullOrEmpty(hash) && usedHashes.Contains(hash))
                        toDelete.Add(file);
                }
                return (remap, toDelete);
            }).ConfigureAwait(true);

            bool anyChanged = false;
            foreach (var kv in result.remap)
            {
                if (!string.Equals(groupImages[kv.Key], kv.Value, StringComparison.OrdinalIgnoreCase))
                {
                    groupImages[kv.Key] = kv.Value;
                    anyChanged = true;
                    Console.WriteLine($" Дедупликация: группа '{kv.Key}' -> {Path.GetFileName(kv.Value)}");
                }
            }
            foreach (var file in result.toDelete)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($" Удалён дубликат: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Ошибка удаления {file}: {ex.Message}");
                }
            }
            if (anyChanged)
            {
                SaveAllData();
                Console.WriteLine($" Дедупликация завершена: перевязано групп - {result.remap.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Ошибка дедупликации: {ex.Message}");
        }
    }

    // ============ ЗАКРЫТИЕ ОКНА ============
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        Console.WriteLine("Окно закрывается, сохраняем данные...");
        _dbService?.Dispose();
        Thread.Sleep(100); // даём время освободить файл
    }
}