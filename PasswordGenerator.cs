using System;
using System.Linq;
using System.Security.Cryptography;

namespace TinaPASS.Main;

public class PasswordGenerator
{
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    public static string Generate(int length = 16,
        bool useLowercase = true,
        bool useUppercase = true,
        bool useDigits = true,
        bool useSpecial = true)
    {
        if (length <= 0) throw new ArgumentException("Длина должна быть больше 0");

        string charSet = "";
        if (useLowercase) charSet += Lowercase;
        if (useUppercase) charSet += Uppercase;
        if (useDigits) charSet += Digits;
        if (useSpecial) charSet += SpecialChars;

        if (string.IsNullOrEmpty(charSet))
            throw new ArgumentException("Должен быть выбран хотя бы один тип символов");

        // Криптостойкий генератор случайных чисел
        char[] password = new char[length];
        byte[] randomBytes = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        for (int i = 0; i < length; i++)
        {
            int index = randomBytes[i] % charSet.Length;
            password[i] = charSet[index];
        }

        // Гарантируем, что пароль содержит все выбранные типы символов
        password = EnsureAllCharTypes(password, useLowercase, useUppercase, useDigits, useSpecial, charSet);

        // Перемешиваем пароль
        return new string(Shuffle(password));
    }

    private static char[] EnsureAllCharTypes(char[] password, bool useLower, bool useUpper, bool useDigits,
        bool useSpecial, string charSet)
    {
        char[] result = (char[])password.Clone();

        if (useLower && !result.Any(c => Lowercase.Contains(c)))
            result[0] = Lowercase[RandomNumberGenerator.GetInt32(0, Lowercase.Length)];

        if (useUpper && !result.Any(c => Uppercase.Contains(c)))
            result[1] = Uppercase[RandomNumberGenerator.GetInt32(0, Uppercase.Length)];

        if (useDigits && !result.Any(c => Digits.Contains(c)))
            result[2] = Digits[RandomNumberGenerator.GetInt32(0, Digits.Length)];

        if (useSpecial && !result.Any(c => SpecialChars.Contains(c)))
            result[3] = SpecialChars[RandomNumberGenerator.GetInt32(0, SpecialChars.Length)];

        return result;
    }

    private static char[] Shuffle(char[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }

        return array;
    }

    // Генерация читаемого пароля (слова + цифры)
    public static string GenerateReadable(int wordCount = 3, int numberLength = 4)
    {
        string[] words =
        {
            "sun", "moon", "star", "sky", "fire", "water", "earth", "wind",
            "blue", "red", "gold", "silver", "dark", "light", "shadow", "flame",
            "storm", "thunder", "ocean", "mountain", "forest", "river", "cloud"
        };

        var random = RandomNumberGenerator.GetInt32(0, words.Length);
        string password = words[random];

        for (int i = 1; i < wordCount; i++)
        {
            random = RandomNumberGenerator.GetInt32(0, words.Length);
            password += "-" + words[random];
        }

        // Добавляем цифры
        for (int i = 0; i < numberLength; i++)
        {
            password += RandomNumberGenerator.GetInt32(0, 10).ToString();
        }

        return password;
    }
}