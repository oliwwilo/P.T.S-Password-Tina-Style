using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TinaPASS.Main
{
    public static class AesEncryptor
    {
        private static readonly byte[] Salt = new byte[] 
            { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

        private static string GetPassword()
        {
            // Генерируем ключ на основе аппаратного ID или юзернейма
            string user = Environment.UserName;
            string machine = Environment.MachineName;
            return $"{user}@{machine}";
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (var aes = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(GetPassword(), Salt, 10000, HashAlgorithmName.SHA256);
                aes.Key = pdb.GetBytes(32);
                aes.IV = pdb.GetBytes(16);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return encryptedText;

            try
            {
                using (var aes = Aes.Create())
                {
                    var pdb = new Rfc2898DeriveBytes(GetPassword(), Salt, 10000, HashAlgorithmName.SHA256);
                    aes.Key = pdb.GetBytes(32);
                    aes.IV = pdb.GetBytes(16);

                    using (var ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Если не удалось расшифровать, возвращаем как есть
                return encryptedText;
            }
        }
    }
}