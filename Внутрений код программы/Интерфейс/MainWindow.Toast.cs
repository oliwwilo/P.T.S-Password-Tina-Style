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
private async void ShowToast(string message)
{
    try
    {
        var toast = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(15, 10, 15, 10),
            Opacity = 0
        };

        var text = FluentIcons.Build(message, 13, Brushes.White);

        toast.Child = text;
        
        var parent = this.Content as Panel;
        if (parent != null)
        {
            toast.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            toast.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 0, 50);

            parent.Children.Add(toast);

            for (int i = 0; i <= 10; i++)
            {
                toast.Opacity = i * 0.1;
                await Task.Delay(10);
            }

            await Task.Delay(2000);

            for (int i = 10; i >= 0; i--)
            {
                toast.Opacity = i * 0.1;
                await Task.Delay(10);
            }

            parent.Children.Remove(toast);
        }
    }
    catch { }
}

}
