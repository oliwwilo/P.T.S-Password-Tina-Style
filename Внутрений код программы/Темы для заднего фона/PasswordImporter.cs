using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TinaPASS.Main
{
    /// <summary>
    /// Импорт паролей из текстового файла.
    /// Формат строки: "Название =PASSWORD пароль =NAME логин =EMAIL почта"
    /// </summary>
    public static class PasswordImporter
    {
        public static async Task<List<PasswordEntry>> ImportFromFileAsync(
            string filePath,
            Action<int>? onProgress = null,
            Action<string>? onLineParsed = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            var lines = await File.ReadAllLinesAsync(filePath);
            var result = new List<PasswordEntry>();
            int total = lines.Length;

            for (int i = 0; i < total; i++)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    var entry = ParseLine(line);
                    if (entry != null)
                    {
                        result.Add(entry);
                        onLineParsed?.Invoke(string.IsNullOrEmpty(entry.Description)
                            ? $" Импортирован: {entry.Title}"
                            : entry.Description);
                    }
                }

                onProgress?.Invoke(total > 0 ? (int)((i + 1) * 100.0 / total) : 100);
            }

            return result;
        }

        private static PasswordEntry? ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            var entry = new PasswordEntry();
            string password = "", login = "", email = "";

            int passwordIndex = -1, nameIndex = -1, emailIndex = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                string token = NormalizeToken(parts[i]);
                if (token == "PASSWORD") passwordIndex = i;
                else if (token == "NAME") nameIndex = i;
                else if (token == "EMAIL") emailIndex = i;
            }

            if (passwordIndex != -1) password = ReadValue(parts, passwordIndex);
            if (nameIndex != -1) login = ReadValue(parts, nameIndex);
            if (emailIndex != -1) email = ReadValue(parts, emailIndex);

            int firstMarkerIndex = parts.Length;
            if (passwordIndex != -1) firstMarkerIndex = Math.Min(firstMarkerIndex, passwordIndex);
            if (nameIndex != -1) firstMarkerIndex = Math.Min(firstMarkerIndex, nameIndex);
            if (emailIndex != -1) firstMarkerIndex = Math.Min(firstMarkerIndex, emailIndex);

            string title = "";
            if (firstMarkerIndex > 0)
            {
                var titleParts = new List<string>();
                for (int i = 0; i < firstMarkerIndex; i++) titleParts.Add(parts[i]);
                title = string.Join(" ", titleParts);
            }
            else title = parts[0];

            if (string.IsNullOrEmpty(password) && parts.Length > 1 && !IsMarker(parts[1]))
                password = parts[1];

            if (string.IsNullOrEmpty(password))
            {
                entry.Title = string.IsNullOrEmpty(title) ? parts[0] : title;
                entry.Login = login;
                entry.Email = email;
                entry.Description = $" Ошибка импорта: {line}";
                return entry;
            }

            entry.Title = string.IsNullOrEmpty(title) ? parts[0] : title;
            entry.Password = password;
            entry.Login = login;
            entry.Email = email;
            return entry;
        }

        private static string NormalizeToken(string token)
        {
            string t = token.ToUpperInvariant();
            if (t.StartsWith("=")) t = t.Substring(1);
            return t;
        }

        private static bool IsMarker(string token)
        {
            string t = NormalizeToken(token);
            return t == "PASSWORD" || t == "NAME" || t == "EMAIL";
        }

        private static string ReadValue(string[] parts, int markerIndex)
        {
            var values = new List<string>();
            for (int i = markerIndex + 1; i < parts.Length; i++)
            {
                if (IsMarker(parts[i])) break;
                values.Add(parts[i]);
            }
            return string.Join(" ", values);
        }
    }
}
