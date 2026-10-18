using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class CreateGroupPanel : UserControl
{
    public event EventHandler<GroupData>? GroupCreated;
    public event EventHandler? Cancelled;
    private string? selectedImagePath;
    private GifHelper? _previewGifHelper;

    public CreateGroupPanel()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(PhotoPreview, true);
        PhotoPreview.AddHandler(DragDrop.DragOverEvent, OnPhotoPreviewDragOver);
        PhotoPreview.AddHandler(DragDrop.DropEvent, OnPhotoPreviewDrop);
    }

    public void Show()
    {
        IsVisible = true;
        DragDrop.SetAllowDrop(PhotoPreview, true);
        GroupNameBox.Text = "";
        selectedImagePath = null;
        UpdatePhotoPreview();
        GroupNameBox.Focus();
    }

    public void Hide()
    {
        PanelFader.FadeOut(this);
        selectedImagePath = null;
        _previewGifHelper?.Dispose();
        _previewGifHelper = null;
        UpdatePhotoPreview();
    }

    private void OnPhotoPreviewEnter(object? sender, PointerEventArgs e)
    {
        PhotoPreview.BorderBrush = Brush.Parse("#E0FF33");
        PhotoPreview.Background = Brush.Parse("#3D3D3D");
        if (PhotoPreviewHint != null)
            PhotoPreviewHint.IsVisible = true;
    }

    private void OnPhotoPreviewLeave(object? sender, PointerEventArgs e)
    {
        PhotoPreview.BorderBrush = Brush.Parse("#CBFF21");
        PhotoPreview.Background = string.IsNullOrEmpty(selectedImagePath)
            ? Brush.Parse("#2D2D2D")
            : null;
        if (PhotoPreviewHint != null)
        {
            PhotoPreviewHint.Text = "Нажмите чтобы выбрать фото";
            PhotoPreviewHint.IsVisible = false;
        }
    }

    private async void OnPhotoPreviewTapped(object? sender, TappedEventArgs e)
    {
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
            selectedImagePath = files[0].Path.LocalPath;
            UpdatePhotoPreview();
        }
    }

    private void OnPhotoPreviewDragOver(object? sender, DragEventArgs e)
    {
        if (HasImageSource(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            PhotoPreview.BorderBrush = Brush.Parse("#E0FF33");
            PhotoPreview.Background = Brush.Parse("#3D3D3D");
            if (PhotoPreviewHint != null)
            {
                PhotoPreviewHint.Text = "Перетаскивай сюда";
                PhotoPreviewHint.IsVisible = true;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void OnPhotoPreviewDrop(object? sender, DragEventArgs e)
    {
        PhotoPreview.BorderBrush = Brush.Parse("#CBFF21");
        if (PhotoPreviewHint != null)
        {
            PhotoPreviewHint.Text = "Нажмите чтобы выбрать фото";
            PhotoPreviewHint.IsVisible = false;
        }

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

        selectedImagePath = path;
        UpdatePhotoPreview();
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

    private async void UpdatePhotoPreview()
    {
        var panel = PhotoPreview.Child as Panel;
        if (panel == null) return;

        var handledGifImage = _previewGifHelper?.TargetImage;
        _previewGifHelper?.Dispose();
        _previewGifHelper = null;

        // Удаляем все Image-элементы превью (найденные динамически элементы не
        // регистрируются в name-scope, поэтому FindControl может не найти их)
        var imagesToRemove = panel.Children.OfType<Image>().ToList();
        foreach (var imageControl in imagesToRemove)
        {
            if (imageControl != handledGifImage && imageControl.Source is Bitmap staticBmp)
            {
                imageControl.Source = null;
                staticBmp.Dispose();
            }
            panel.Children.Remove(imageControl);
        }

        if (string.IsNullOrEmpty(selectedImagePath))
        {
            PhotoPreviewText.IsVisible = true;
            PhotoPreviewText.Text = FluentIcons.Photo;
            PhotoPreviewText.FontSize = 100;
            PhotoPreview.Background = Brush.Parse("#2D2D2D");
        }
        else
        {
            try
            {
                PhotoPreviewText.IsVisible = false;
                PhotoPreview.Background = null;

                if (File.Exists(selectedImagePath))
                {
                    var image = new Image
                    {
                        Name = "PhotoPreviewImage",
                        Stretch = Avalonia.Media.Stretch.UniformToFill
                    };
                    panel.Children.Insert(0, image);

                    if (GifHelper.IsGifFile(selectedImagePath))
                    {
                        _previewGifHelper = new GifHelper(image);
                        await _previewGifHelper.LoadImageAsync(selectedImagePath);
                    }
                    else
                    {
                        image.Source = new Bitmap(selectedImagePath);
                    }
                }
                else
                {
                    PhotoPreviewText.IsVisible = true;
                    PhotoPreviewText.Text = FluentIcons.Cancel;
                    PhotoPreviewText.FontSize = 100;
                }
            }
            catch
            {
                PhotoPreviewText.IsVisible = true;
                PhotoPreviewText.Text = FluentIcons.Cancel;
                PhotoPreviewText.FontSize = 100;
            }
        }
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        string groupName = GroupNameBox.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            GroupCreated?.Invoke(this, new GroupData
            {
                Name = groupName,
                ImagePath = selectedImagePath ?? ""
            });
            Hide();
        }
        else
        {
            GroupNameBox.Background = Brush.Parse("#33FF4444");
        }
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e) => OnCancelClick(sender, new RoutedEventArgs());

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        GroupNameBox.Background = Brush.Parse("#2D2D2D");
        Cancelled?.Invoke(this, EventArgs.Empty);
        Hide();
    }
}

public class GroupData
{
    public string Name { get; set; } = "";
    public string ImagePath { get; set; } = "";
}