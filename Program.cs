using System;
using System.Text;
using Avalonia;

namespace TinaPASS.Main;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //  УСТАНАВЛИВАЕМ КОДИРОВКУ UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}