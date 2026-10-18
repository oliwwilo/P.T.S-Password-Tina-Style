using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace TinaPASS.Main;

public static class FluentIcons
{
    public const string Font = "Segoe Fluent Icons, Segoe MDL2 Assets";
    public const string TextFont = "Segoe Fluent Icons, Segoe MDL2 Assets";

    public const string Search = "\uE721";
    public const string Add = "\uE710";
    public const string Cancel = "\uE711";
    public const string Delete = "\uE74D";
    public const string Save = "\uE74E";
    public const string CheckMark = "\uE73E";
    public const string Folder = "\uE8B7";
    public const string FolderOpen = "\uE838";
    public const string Document = "\uE8A5";
    public const string Inbox = "\uE840";
    public const string Mail = "\uE715";
    public const string Lock = "\uE72E";
    public const string Contact = "\uE77B";
    public const string Copy = "\uE8C8";
    public const string Refresh = "\uE72C";
    public const string ChevronDown = "\uE70D";
    public const string ChevronUp = "\uE70E";
    public const string Back = "\uE72B";
    public const string Cloud = "\uE753";
    public const string Color = "\uE790";
    public const string Brightness = "\uE706";
    public const string Star = "\uE734";
    public const string Snow = "\uE736";
    public const string Sparkle = "\uE73B";
    public const string Photo = "\uE91B";
    public const string Info = "\uE946";
    public const string Warning = "\uE7BA";
    public const string Sync = "\uE895";

    public static bool IsGlyph(char c) => c >= 0xE000 && c <= 0xF8FF;

    public static void Set(TextBlock textBlock, string text)
    {
        if (string.IsNullOrEmpty(text) || !IsGlyph(text[0]))
        {
            textBlock.Text = text;
            return;
        }

        char glyph = text[0];
        string rest = text.Substring(1);

        var inlines = new InlineCollection();
        var iconRun = new Run { Text = glyph.ToString(), FontFamily = new FontFamily(Font) };
        var textRun = new Run { Text = rest };
        inlines.Add(iconRun);
        inlines.Add(textRun);
        textBlock.Inlines = inlines;
    }

    public static TextBlock Build(string text, double fontSize = 14, IBrush? foreground = null, TextWrapping? textWrapping = null)
    {
        var tb = new TextBlock
        {
            FontSize = fontSize,
            Foreground = foreground ?? Brushes.White,
            TextWrapping = textWrapping ?? TextWrapping.NoWrap
        };

        if (string.IsNullOrEmpty(text) || !IsGlyph(text[0]))
        {
            tb.Text = text;
        }
        else
        {
            Set(tb, text);
        }

        return tb;
    }

    public static TextBlock Icon(string glyph, double fontSize = 16, IBrush? foreground = null)
    {
        return new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily(Font),
            FontSize = fontSize,
            Foreground = foreground ?? Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
    }
}