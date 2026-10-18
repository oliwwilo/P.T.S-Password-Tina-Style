using System;
using System.Diagnostics;

namespace TinaPASS.Main
{
    public static class PerformanceLogger
    {
        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static string _currentMethod = "";

        public static void Start(string methodName)
        {
            _currentMethod = methodName;
            _stopwatch.Restart();
            Console.WriteLine($" [{methodName}] начат");
        }

        public static void Log(string message)
        {
            Console.WriteLine($"   {message}");
        }

        public static void Finish(string extraInfo = "")
        {
            _stopwatch.Stop();
            var elapsed = _stopwatch.Elapsed;
            string info = string.IsNullOrEmpty(extraInfo) ? "" : $" ({extraInfo})";
            Console.WriteLine($" [{_currentMethod}] завершён за {elapsed.TotalMilliseconds:F2} мс{info}");
            
            // Если время превышает 100 мс — выделяем жирным (в консоли можно добавить звёздочки)
            if (elapsed.TotalMilliseconds > 100)
                Console.WriteLine($"    ВНИМАНИЕ: {_currentMethod} выполняется дольше 100 мс!");
        }

        // Для измерения времени внутри цикла или вложенных операций
        public static void StartNested(string operationName)
        {
            Console.WriteLine($"    {operationName}...");
        }

        public static void FinishNested(string operationName, long elapsedMs)
        {
            Console.WriteLine($"    {operationName} завершён за {elapsedMs} мс");
        }

        public static void LogCount(string collectionName, int count)
        {
            Console.WriteLine($"    {collectionName}: {count} элементов");
        }
    }
}