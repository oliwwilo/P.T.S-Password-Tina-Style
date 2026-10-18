using System;
using System.Text.Json.Serialization;

namespace TinaPASS.Main
{
    public class PasswordEntry
    {
        
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Email { get; set; } = "";
        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
        public string GroupId { get; set; } = "";
        public string Description { get; set; } = "";
        public int Number { get; set; }           // для нумерации
        public bool IsSelected { get; set; }      // для чекбокса
        public string PasswordDisplay => $" Пароль: {new string('●', Password.Length)}";
    
        public string emp { get; set; } = "";
        //  ДОБАВЛЯЕМ МЕТОДЫ ДЛЯ РАБОТЫ С ШИФРОВАНИЕМ
        public string GetEncryptedPassword()
        {
            return AesEncryptor.Encrypt(Password);
        }
    
        public void SetEncryptedPassword(string encryptedPassword)
        {
            Password = AesEncryptor.Decrypt(encryptedPassword);
        }
    }
}