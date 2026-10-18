using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace TinaPASS.Main
{
    public class GifHelper : IDisposable
    {
        private readonly Image _targetImage;
        private DispatcherTimer? _timer;
        private List<Bitmap>? _frames;
        private List<int>? _frameDelays;
        private int _currentFrameIndex;
        private bool _isDisposed;
        private bool _holdPause;

        /// <summary>
        /// Поколение загрузки: помогает отбрасывать устаревшие асинхронные загрузки.
        /// Каждая новая загрузка увеличивает счётчик, а завершение старой загрузки
        /// проверяет своё поколение и не трогает целевую картинку, если оно устарело.
        /// </summary>
        private int _loadGeneration;

        /// <summary>
        /// Пауза (мс) после полного прохода всех кадров, перед началом нового цикла.
        /// 0 = проигрывать непрерывно (по умолчанию).
        /// </summary>
        public double AnimationPauseMs { get; set; }

        // ---------- Кэш расшифрованных кадров ----------
        private static readonly object _cacheLock = new object();
        private static readonly Dictionary<string, CachedGif> _frameCache = new Dictionary<string, CachedGif>();
        private const int MaxCacheEntries = 16;
        private const int MaxGifSide = 256;
        private const int SkipOversizeSide = 2048;

        /// <summary>
        /// Кэш-запись: кадры + задержки + счётчик ссылок
        /// </summary>
        private class CachedGif
        {
            public string Key = "";
            public List<Bitmap> Frames = new List<Bitmap>();
            public List<int> Delays = new List<int>();
            public int RefCount;
            public DateTime LastAccess;
        }

        private CachedGif? _cachedRef;

        public GifHelper(Image targetImage)
        {
            _targetImage = targetImage ?? throw new ArgumentNullException(nameof(targetImage));
        }

        public Image TargetImage => _targetImage;

        /// <summary>
        /// Загружает изображение (GIF или статическое)
        /// </summary>
        public async Task LoadImageAsync(string imagePath)
        {
            // Новая загрузка делает предыдущие устаревшими
            int generation = ++_loadGeneration;

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                Clear();
                return;
            }

            try
            {
                string extension = Path.GetExtension(imagePath).ToLower();

                if (extension == ".gif")
                {
                    await LoadGifAsync(imagePath, generation).ConfigureAwait(true);
                }
                else
                {
                    await LoadStaticImageAsync(imagePath, generation).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                Clear();
            }
        }

        /// <summary>
        /// Загружает GIF анимацию: кадры декодируются в фоне через нативный SKCodec,
        /// а всё, что касается UI и таймера, выполняется на UI-потоке.
        /// Уже расшифрованные кадры берутся из кэша — переключение на группу мгновенное.
        /// </summary>
        private async Task LoadGifAsync(string gifPath, int generation)
        {
            Clear();

            string cacheKey = BuildCacheKey(gifPath);
            CachedGif? cached = null;

            try
            {
                // Быстрый путь: кадры уже в кэше
                lock (_cacheLock)
                {
                    if (_frameCache.TryGetValue(cacheKey, out cached))
                    {
                        cached.RefCount++;
                        cached.LastAccess = DateTime.Now;
                        _cachedRef = cached;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка кэша GIF: {ex.Message}");
                cached = null;
            }

            // Медленный путь: декодируем в фоне и кладём в кэш
            if (cached == null)
            {
                try
                {
                    var decoded = await Task.Run(() => DecodeGifFrames(gifPath)).ConfigureAwait(true);
                    if (decoded.Oversized)
                    {
                        Console.WriteLine($" Огромный GIF не отображён: {gifPath}");
                        return;
                    }
                    if (_isDisposed || generation != _loadGeneration)
                    {
                        DisposeFrames(decoded.Frames);
                        return;
                    }
                    if (decoded.Frames.Count == 0)
                    {
                        LoadStaticImage(gifPath, generation);
                        return;
                    }

                    lock (_cacheLock)
                    {
                        if (_frameCache.TryGetValue(cacheKey, out var existing))
                        {
                            // Параллельный загрузчик уже расшифровал — используем его, свои кадры выбрасываем
                            DisposeFrames(decoded.Frames);
                            existing.RefCount++;
                            existing.LastAccess = DateTime.Now;
                            cached = existing;
                        }
                        else
                        {
                            cached = new CachedGif
                            {
                                Key = cacheKey,
                                Frames = decoded.Frames,
                                Delays = decoded.Delays,
                                RefCount = 1,
                                LastAccess = DateTime.Now
                            };
                            _frameCache[cacheKey] = cached;
                            TrimCache();
                        }
                    }

                    _cachedRef = cached;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки GIF: {ex.Message}");
                    LoadStaticImage(gifPath, generation);
                    return;
                }
            }

            _frames = cached.Frames;
            _frameDelays = cached.Delays;

            if (_frames.Count == 0)
            {
                LoadStaticImage(gifPath, generation);
                return;
            }

            // Загрузка устарела (успели скрыть иконку/переключить группу) — не показываем
            if (_isDisposed || generation != _loadGeneration)
                return;

            _targetImage.Source = _frames[0];
            _targetImage.IsVisible = true;

            if (_frames.Count > 1)
                StartAnimation();
        }

        /// <summary>
        /// Формирует ключ кэша: полный путь + время изменения файла
        /// </summary>
        private static string BuildCacheKey(string gifPath)
        {
            try
            {
                var fi = new FileInfo(gifPath);
                if (fi.Exists)
                    return fi.FullName + "|" + fi.LastWriteTimeUtc.Ticks;
            }
            catch { }

            return gifPath;
        }

        /// <summary>
        /// Ограничивает размер кэша (выкидывает неиспользуемые записи по LastAccess)
        /// </summary>
        private static void TrimCache()
        {
            if (_frameCache.Count <= MaxCacheEntries)
                return;

            // Диспоузим только те записи, на которые нет активных ссылок
            var freeloadable = _frameCache.Values
                .Where(c => c.RefCount <= 0)
                .OrderBy(c => c.LastAccess)
                .ToList();

            int toRemove = _frameCache.Count - MaxCacheEntries;
            var entries = freeloadable.Take(toRemove).ToList();
            foreach (var entry in entries)
            {
                DisposeFrames(entry.Frames);
                _frameCache.Remove(entry.Key);
            }
        }

        private static void DisposeFrames(List<Bitmap>? frames)
        {
            if (frames == null) return;
            foreach (var frame in frames)
                frame?.Dispose();
            frames.Clear();
        }

        /// <summary>
        /// Декодирует все кадры GIF через нативный SkiaSharp SKCodec (работает в фоне)
        /// </summary>
        private (List<Bitmap> Frames, List<int> Delays, bool Oversized) DecodeGifFrames(string gifPath)
        {
            var frames = new List<Bitmap>();
            var delays = new List<int>();

            using (var stream = File.OpenRead(gifPath))
            using (var codec = SKCodec.Create(stream))
            {
                if (codec == null)
                    return (frames, delays, false);

                int width = codec.Info.Width;
                int height = codec.Info.Height;
                int frameCount = Math.Max(1, codec.FrameCount);

                // Защита от гигантских GIF: их декод съел бы гигабайты памяти
                int fullMaxSide = Math.Max(width, height);
                if (fullMaxSide > SkipOversizeSide)
                {
                    Console.WriteLine($" GIF пропущен (слишком большой {width}x{height}): {Path.GetFileName(gifPath)}");
                    return (frames, delays, true);
                }

                // Аватары отображаются мелко, а каждый кадр в памяти ~ W*H*4 байт,
                // поэтому держим не больше MaxGifSide по большей стороне
                double scale = fullMaxSide > MaxGifSide ? (double)MaxGifSide / fullMaxSide : 1.0;
                int decodeWidth = Math.Max(1, (int)Math.Round(width * scale));
                int decodeHeight = Math.Max(1, (int)Math.Round(height * scale));

                for (int i = 0; i < frameCount; i++)
                    {
                        var fullInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

                        using (var fullBmp = new SKBitmap(fullInfo))
                        {
                            var options = new SKCodecOptions(i);
                            var result = codec.GetPixels(fullInfo, fullBmp.GetPixels(), options);
                            if (result != SKCodecResult.Success)
                                continue;

                            using (var scaled = new SKBitmap(decodeWidth, decodeHeight, SKColorType.Bgra8888, SKAlphaType.Premul))
                            {
                                fullBmp.ScalePixels(scaled, SKFilterQuality.Medium);

                                // Надёжный путь отображения: перекодируем кадр в PNG и строим штатный Bitmap
                                using (var image = SKImage.FromBitmap(scaled))
                                using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
                                {
                                    using var ms = new MemoryStream(png.ToArray());
                                    frames.Add(new Avalonia.Media.Imaging.Bitmap(ms));
                                }
                            }
                        }

                    if (frameCount > 1)
                    {
                        int delay = 100;
                        if (codec.GetFrameInfo(i, out var frameInfo))
                            delay = (int)TimeSpan.FromMilliseconds(frameInfo.Duration).TotalMilliseconds;
                        delays.Add(Math.Max(20, delay));
                    }
                    else
                    {
                        delays.Add(100);
                    }
                }
            }

            return (frames, delays, false);
        }

        /// <summary>
        /// Загружает статическое изображение
        /// </summary>
        private void LoadStaticImage(string imagePath, int generation)
        {
            try
            {
                using (var stream = File.OpenRead(imagePath))
                {
                    var bitmap = new Bitmap(stream);
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        // Загрузка устарела (успели скрыть иконку/переключить группу) — не показываем
                        if (_isDisposed || generation != _loadGeneration)
                        {
                            bitmap.Dispose();
                            return;
                        }

                        _targetImage.Source = bitmap;
                        _targetImage.IsVisible = true;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                Clear();
            }
        }

        /// <summary>
        /// Загружает статическое изображение (асинхронно)
        /// </summary>
        private async Task LoadStaticImageAsync(string imagePath, int generation)
        {
            Clear();

            await Task.Run(() =>
            {
                LoadStaticImage(imagePath, generation);
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// Запускает анимацию (только на UI-потоке)
        /// </summary>
        private void StartAnimation()
        {
            if (_timer != null || _frames == null || _frames.Count <= 1)
                return;

            _currentFrameIndex = 0;

            int delay = _frameDelays != null && _frameDelays.Count > 0
                ? _frameDelays[0]
                : 150;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(delay)
            };

            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        /// <summary>
        /// Обработчик тика таймера (вызывается на UI-потоке)
        /// </summary>
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_isDisposed || _frames == null || _frames.Count == 0)
            {
                Stop();
                return;
            }

            // Отрабатываем паузу после полного прохода всех кадров
            if (_holdPause)
            {
                _holdPause = false;
                _currentFrameIndex = 0;
                _targetImage.Source = _frames[0];

                if (_timer != null && _frameDelays != null && _frameDelays.Count > 0)
                    _timer.Interval = TimeSpan.FromMilliseconds(_frameDelays[0]);
                return;
            }

            int next = _currentFrameIndex + 1;
            if (next >= _frames.Count)
            {
                if (AnimationPauseMs > 0 && _frames.Count > 1)
                {
                    // Анимация прошла полностью: держим последний кадр на паузе,
                    // затем начинаем новый цикл с первого кадра
                    _holdPause = true;
                    if (_timer != null)
                        _timer.Interval = TimeSpan.FromMilliseconds(AnimationPauseMs);
                    return;
                }
                next = 0;
            }

            _currentFrameIndex = next;

            _targetImage.Source = _frames[_currentFrameIndex];

            if (_timer != null && _frameDelays != null && _frameDelays.Count > 0)
            {
                int index = _currentFrameIndex % _frameDelays.Count;
                _timer.Interval = TimeSpan.FromMilliseconds(_frameDelays[index]);
            }
        }

        /// <summary>
        /// Останавливает анимацию
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
            }

            _holdPause = false;
            _currentFrameIndex = 0;
        }

        /// <summary>
        /// Идёт ли анимация прямо сейчас
        /// </summary>
        public bool IsPlaying => _timer != null;

        /// <summary>
        /// Пауза: останавливает таймер, но оставляет текущий кадр на экране
        /// </summary>
        public void Pause()
        {
            if (_timer == null)
                return;

            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        /// <summary>
        /// Продолжает анимацию с того же кадра, на котором остановились
        /// </summary>
        public void Resume()
        {
            if (_timer != null || _frames == null || _frames.Count <= 1)
                return;

            // Пользователь нажал «продолжить» во время паузы-удержания — стартуем с начала
            if (_holdPause)
                _currentFrameIndex = 0;
            _holdPause = false;

            if (_currentFrameIndex >= _frames.Count)
                _currentFrameIndex = 0;

            _targetImage.Source = _frames[_currentFrameIndex];

            int delay = _frameDelays != null && _frameDelays.Count > 0
                ? _frameDelays[_currentFrameIndex % _frameDelays.Count]
                : 150;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(delay)
            };

            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        /// <summary>
        /// Переключает паузу/воспроизведение
        /// </summary>
        public void TogglePlay()
        {
            if (_timer != null)
                Pause();
            else
                Resume();
        }

        /// <summary>
        /// Очищает ресурсы
        /// </summary>
        public void Clear()
        {
            Stop();

            if (_cachedRef != null)
            {
                // Кадры из кэша — не диспоузим, только уменьшаем счётчик ссылок
                lock (_cacheLock)
                {
                    _cachedRef.RefCount--;
                    if (_cachedRef.RefCount <= 0)
                    {
                        DisposeFrames(_cachedRef.Frames);
                        _frameCache.Remove(_cachedRef.Key);
                    }
                }
                _cachedRef = null;
            }
            else
            {
                DisposeFrames(_frames);
            }

            _frames = null;
            _frameDelays = null;

            Dispatcher.UIThread.Invoke(() =>
            {
                _targetImage.Source = null;
                _targetImage.IsVisible = false;
            });
        }

        /// <summary>
        /// Отменяет незавершённые асинхронные загрузки и очищает текущее изображение.
        /// Используется при скрытии иконки (переход в «Без группы»), чтобы
        /// завершившийся в фоне GIF/картинка не вернули изображение на экран.
        /// </summary>
        public void CancelAndClear()
        {
            _loadGeneration++;
            Clear();
        }

        /// <summary>
        /// Проверяет, является ли файл GIF
        /// </summary>
        public static bool IsGifFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".gif";
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Clear();
        }
    }
}