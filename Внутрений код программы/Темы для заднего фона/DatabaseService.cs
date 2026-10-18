using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TinaPASS.Main
{
    public class DatabaseService : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<PasswordEntryDocument> _passwords;
        private readonly ILiteCollection<GroupDocument> _groups;
        private readonly ILiteCollection<PasswordEntryDocument> _noGroupPasswords;
        private readonly ILiteCollection<OrderDocument> _order;

        public DatabaseService(string databasePath = null)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dir = System.IO.Path.Combine(appData, "TinaPASS");
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                databasePath = System.IO.Path.Combine(dir, "tinapass.db");
            }

            const int maxAttempts = 5;
            const int delayMs = 200;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    _db = new LiteDatabase($"Filename={databasePath}; Connection=Shared;");
                    _passwords = _db.GetCollection<PasswordEntryDocument>("passwords");
                    _groups = _db.GetCollection<GroupDocument>("groups");
                    _noGroupPasswords = _db.GetCollection<PasswordEntryDocument>("nogroup_passwords");
                    _order = _db.GetCollection<OrderDocument>("order");

                    _passwords.EnsureIndex(x => x.GroupId);
                    _passwords.EnsureIndex(x => x.Title);
                    _groups.EnsureIndex(x => x.Name);
                    _noGroupPasswords.EnsureIndex(x => x.Title);
                    _order.EnsureIndex(x => x.Name);

                    return;
                }
                catch (IOException ex) when (attempt < maxAttempts - 1)
                {
                    Console.WriteLine($" Попытка {attempt + 1} открыть БД не удалась: {ex.Message}. Повтор через {delayMs} мс...");
                    System.Threading.Thread.Sleep(delayMs);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($" Не удалось открыть БД после {maxAttempts} попыток.");
                    throw;
                }
            }
        }

        // ----- ГРУППЫ -----
        public List<GroupDocument> GetAllGroups() => _groups.Query().ToList();
        public GroupDocument GetGroup(string name) => _groups.FindOne(x => x.Name == name);

        public void SaveGroup(GroupDocument group)
        {
            var existing = GetGroup(group.Name);
            if (existing != null)
            {
                group.Id = existing.Id;
                _groups.Update(group);
            }
            else
                _groups.Insert(group);
        }

        public void DeleteGroup(string name)
        {
            _groups.DeleteMany(x => x.Name == name);
            _passwords.DeleteMany(x => x.GroupId == name);
        }

        // ----- ПАРОЛИ ОБЫЧНЫХ ГРУПП -----
        public List<PasswordEntryDocument> GetPasswordsByGroup(string groupId)
            => _passwords.Find(x => x.GroupId == groupId).ToList();

        public void SavePassword(PasswordEntryDocument password)
        {
            var existing = _passwords.FindOne(x => x.Id == password.Id);
            if (existing != null)
                _passwords.Update(password);
            else
                _passwords.Insert(password);
        }

        public void DeletePassword(Guid id) => _passwords.Delete(id);
        public void DeletePasswordsByGroup(string groupId) => _passwords.DeleteMany(x => x.GroupId == groupId);

        // ----- ПАРОЛИ "БЕЗ ГРУППЫ" -----
        public List<PasswordEntryDocument> GetNoGroupPasswords()
            => _noGroupPasswords.Query().ToList();

        public void SaveNoGroupPassword(PasswordEntryDocument password)
        {
            var existing = _noGroupPasswords.FindOne(x => x.Id == password.Id);
            if (existing != null)
                _noGroupPasswords.Update(password);
            else
                _noGroupPasswords.Insert(password);
        }

        public void DeleteNoGroupPassword(Guid id) => _noGroupPasswords.Delete(id);
        public void DeleteAllNoGroupPasswords() => _noGroupPasswords.DeleteAll();

        // ----- ПОРЯДОК ГРУПП (отдельная коллекция) -----
        public List<string> GetGroupOrder()
        {
            var doc = _order.FindOne(x => x.Name == "group_order");
            return doc?.GroupOrder ?? new List<string>();
        }

        public void SaveGroupOrder(List<string> groupOrder)
        {
            Console.WriteLine($" SaveGroupOrder: сохраняем порядок = [{string.Join(", ", groupOrder ?? new List<string>())}]");
    
            var doc = _order.FindOne(x => x.Name == "group_order");
            if (doc == null)
            {
                doc = new OrderDocument
                {
                    Id = Guid.NewGuid(),
                    Name = "group_order",
                    GroupOrder = groupOrder ?? new List<string>()
                };
                _order.Insert(doc);
                Console.WriteLine("    Создан новый документ order");
            }
            else
            {
                doc.GroupOrder = groupOrder ?? new List<string>();
                _order.Update(doc);
                Console.WriteLine("    Обновлён существующий документ order");
            }
        }

        public void DeleteGroupOrder()
        {
            _order.DeleteMany(x => x.Name == "group_order");
        }

        // ----- ИМПОРТ / ЭКСПОРТ -----
    public void ImportFromDictionary(Dictionary<string, List<PasswordEntry>> allPasswords,
                                 Dictionary<string, string> groupImages,
                                 List<string> groupOrder = null)
{
    const int maxRetries = 3;
    Console.WriteLine($" ImportFromDictionary: получен groupOrder = [{string.Join(", ", groupOrder ?? new List<string>())}]");
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            _db.BeginTrans();

            // 1. Очищаем все коллекции
            _passwords.DeleteAll();
            _groups.DeleteAll();
            _noGroupPasswords.DeleteAll();

            // 2. Сохраняем порядок групп отдельно
            if (groupOrder != null)
            {
                SaveGroupOrder(groupOrder);
            }

            // 3. Гарантируем уникальность ID для всех паролей
            var allIds = new HashSet<Guid>();
            foreach (var kv in allPasswords)
            {
                foreach (var pwd in kv.Value)
                {
                    if (pwd.Id == Guid.Empty || !allIds.Add(pwd.Id))
                    {
                        pwd.Id = Guid.NewGuid();
                        allIds.Add(pwd.Id);
                        Console.WriteLine($" Перегенерирован ID для '{pwd.Title}' (попытка {attempt + 1})");
                    }
                }
            }

            // 4. Вставляем группы и пароли
            foreach (var kv in allPasswords)
            {
                var groupName = kv.Key;

                // ---- Группа "Без группы" – только пароли в отдельную коллекцию ----
                if (groupName == "Без группы")
                {
                    foreach (var pwd in kv.Value)
                    {
                        if (pwd.Id == Guid.Empty) pwd.Id = Guid.NewGuid();

                        var doc = new PasswordEntryDocument
                        {
                            Id = pwd.Id,
                            GroupId = "Без группы",
                            Title = pwd.Title,
                            Email = pwd.Email,
                            Login = pwd.Login,
                            Password = pwd.GetEncryptedPassword(),
                            Description = pwd.Description,
                            Number = pwd.Number,
                            IsSelected = pwd.IsSelected
                        };
                        _noGroupPasswords.Insert(doc);
                    }
                    continue; // не создаём группу в _groups
                }

                // ---- Обычная группа ----
                var group = new GroupDocument
                {
                    Id = Guid.NewGuid(),
                    Name = groupName,
                    ImagePath = groupImages.TryGetValue(groupName, out var img) ? img : null
                };
                _groups.Insert(group);

                // ---- Пароли обычной группы ----
                foreach (var pwd in kv.Value)
                {
                    if (pwd.Id == Guid.Empty) pwd.Id = Guid.NewGuid();

                    var doc = new PasswordEntryDocument
                    {
                        Id = pwd.Id,
                        GroupId = groupName,
                        Title = pwd.Title,
                        Email = pwd.Email,
                        Login = pwd.Login,
                        Password = pwd.GetEncryptedPassword(),
                        Description = pwd.Description,
                        Number = pwd.Number,
                        IsSelected = pwd.IsSelected
                    };
                    _passwords.Insert(doc);
                }
            }

            _db.Commit();
            Console.WriteLine($" ImportFromDictionary успешно завершён (попытка {attempt + 1})");
            return;
        }
        catch (LiteException ex) when (ex.Message.Contains("duplicate key") && attempt < maxRetries - 1)
        {
            Console.WriteLine($" Дубликат ключа, попытка {attempt + 1} из {maxRetries}. Откат и перегенерация ID...");
            _db.Rollback();
            // Перегенерируем ID для всех паролей
            foreach (var kv in allPasswords)
                foreach (var pwd in kv.Value)
                    pwd.Id = Guid.NewGuid();
        }
        catch (Exception ex)
        {
            _db.Rollback();
            Console.WriteLine($" Ошибка в ImportFromDictionary: {ex.Message}");
            throw;
        }
    }
    throw new Exception($"Не удалось сохранить данные после {maxRetries} попыток.");
}

        /// <summary>
        /// Инкрементальное сохранение: пишет в БД только изменения.
        /// Не перезаписывает базу целиком, поэтому файл и память не растут со временем.
        /// </summary>
        public void SaveAllIncremental(Dictionary<string, List<PasswordEntry>> allPasswords,
                                       Dictionary<string, string> groupImages,
                                       List<string> groupOrder = null)
        {
            const int maxRetries = 3;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    _db.BeginTrans();

                    // ---------- 1. Группы: создаём/обновляем/удаляем ----------
                    var dbGroupMap = new Dictionary<string, GroupDocument>(StringComparer.Ordinal);
                    foreach (var g in _groups.FindAll())
                        if (!dbGroupMap.ContainsKey(g.Name))
                            dbGroupMap[g.Name] = g;

                    var desiredGroupNames = new HashSet<string>(
                        allPasswords.Keys.Where(k => k != "Без группы"), StringComparer.Ordinal);

                    foreach (var groupName in desiredGroupNames)
                    {
                        string? imagePath = groupImages != null && groupImages.TryGetValue(groupName, out var img) ? img : null;
                        if (dbGroupMap.TryGetValue(groupName, out var existing))
                        {
                            if (existing.ImagePath != imagePath)
                            {
                                existing.ImagePath = imagePath;
                                _groups.Update(existing);
                            }
                        }
                        else
                        {
                            _groups.Insert(new GroupDocument
                            {
                                Id = Guid.NewGuid(),
                                Name = groupName,
                                ImagePath = imagePath
                            });
                        }
                    }

                    foreach (var name in dbGroupMap.Keys.ToList())
                    {
                        if (!desiredGroupNames.Contains(name))
                        {
                            _groups.DeleteMany(x => x.Name == name);
                            _passwords.DeleteMany(x => x.GroupId == name);
                        }
                    }

                    // ---------- 2. Пароли по каждой группе (включая "Без группы") ----------
                    foreach (var kv in allPasswords)
                    {
                        string groupName = kv.Key;
                        bool isNoGroup = groupName == "Без группы";
                        var collection = isNoGroup ? _noGroupPasswords : _passwords;

                        var existing = new Dictionary<Guid, PasswordEntryDocument>();
                        foreach (var doc in collection.Find(x => x.GroupId == groupName))
                            if (!existing.ContainsKey(doc.Id))
                                existing[doc.Id] = doc;

                        var desiredIds = new HashSet<Guid>();
                        foreach (var pwd in kv.Value)
                        {
                            if (pwd.Id == Guid.Empty) pwd.Id = Guid.NewGuid();
                            desiredIds.Add(pwd.Id);

                            var doc = ToDocument(pwd, groupName);
                            if (existing.TryGetValue(pwd.Id, out var old))
                            {
                                if (!DocumentsEqual(old, doc))
                                {
                                    doc.Id = old.Id;
                                    collection.Update(doc);
                                }
                            }
                            else
                            {
                                collection.Insert(doc);
                            }
                        }

                        foreach (var id in existing.Keys.ToList())
                        {
                            if (!desiredIds.Contains(id))
                                collection.Delete(id);
                        }
                    }

                    // ---------- 3. Порядок групп (пишем только при изменении) ----------
                    var orderDoc = _order.FindOne(x => x.Name == "group_order");
                    var newOrder = groupOrder ?? new List<string>();
                    if (orderDoc == null)
                    {
                        _order.Insert(new OrderDocument
                        {
                            Id = Guid.NewGuid(),
                            Name = "group_order",
                            GroupOrder = newOrder
                        });
                    }
                    else if (orderDoc.GroupOrder == null || !orderDoc.GroupOrder.SequenceEqual(newOrder))
                    {
                        orderDoc.GroupOrder = newOrder;
                        _order.Update(orderDoc);
                    }

                    _db.Commit();
                    _db.Checkpoint();
                    Console.WriteLine($" SaveAllIncremental: сохранены изменения (попытка {attempt + 1})");
                    return;
                }
                catch (LiteException ex) when (ex.Message.Contains("duplicate key") && attempt < maxRetries - 1)
                {
                    Console.WriteLine($" Дубликат ключа, попытка {attempt + 1} из {maxRetries}. Откат и перегенерация ID...");
                    _db.Rollback();
                    foreach (var kv in allPasswords)
                        foreach (var pwd in kv.Value)
                            pwd.Id = Guid.NewGuid();
                }
                catch (Exception ex)
                {
                    _db.Rollback();
                    Console.WriteLine($" Ошибка в SaveAllIncremental: {ex.Message}");
                    throw;
                }
            }
            throw new Exception($"Не удалось сохранить данные после {maxRetries} попыток.");
        }

        /// <summary>
        /// Преобразует PasswordEntry в документ для LiteDB
        /// </summary>
        private static PasswordEntryDocument ToDocument(PasswordEntry pwd, string groupName)
        {
            return new PasswordEntryDocument
            {
                Id = pwd.Id,
                GroupId = groupName,
                Title = pwd.Title,
                Email = pwd.Email,
                Login = pwd.Login,
                Password = pwd.GetEncryptedPassword(),
                Description = pwd.Description,
                Number = pwd.Number,
                IsSelected = pwd.IsSelected
            };
        }

        /// <summary>
        /// Сравнивает документ из БД с новым (поля без учета Id)
        /// </summary>
        private static bool DocumentsEqual(PasswordEntryDocument a, PasswordEntryDocument b)
        {
            return a.GroupId == b.GroupId
                && a.Title == b.Title
                && a.Email == b.Email
                && a.Login == b.Login
                && a.Password == b.Password
                && a.Description == b.Description
                && a.Number == b.Number
                && a.IsSelected == b.IsSelected;
        }

        public (Dictionary<string, List<PasswordEntry>>, Dictionary<string, string>, List<string>) ExportToDictionary()
        {
            var allPasswords = new Dictionary<string, List<PasswordEntry>>();
            var groupImages = new Dictionary<string, string>();

            // Загружаем порядок из отдельной коллекции
            var groupOrder = GetGroupOrder();

            // Загружаем обычные группы (без учёта порядка)
            var groups = _groups.Query().ToList();

            foreach (var group in groups)
            {
                var entries = GetPasswordsByGroup(group.Name);
                var list = new List<PasswordEntry>();
                foreach (var doc in entries)
                {
                    var entry = new PasswordEntry
                    {
                        Id = doc.Id,
                        Title = doc.Title,
                        Email = doc.Email,
                        Login = doc.Login,
                        Description = doc.Description,
                        Number = doc.Number,
                        IsSelected = doc.IsSelected
                    };
                    entry.SetEncryptedPassword(doc.Password);
                    list.Add(entry);
                }
                if (entries.Count > 1)
                    list = list.OrderBy(x => x.Number).ToList();
                allPasswords[group.Name] = list;
                if (!string.IsNullOrEmpty(group.ImagePath))
                    groupImages[group.Name] = group.ImagePath;
            }

            // Добавляем "Без группы", если её нет в allPasswords
            var noGroupEntries = GetNoGroupPasswords();
            var noGroupList = new List<PasswordEntry>();
            foreach (var doc in noGroupEntries)
            {
                var entry = new PasswordEntry
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    Email = doc.Email,
                    Login = doc.Login,
                    Description = doc.Description,
                    Number = doc.Number,
                    IsSelected = doc.IsSelected
                };
                entry.SetEncryptedPassword(doc.Password);
                noGroupList.Add(entry);
            }
            if (noGroupEntries.Count > 1)
                noGroupList = noGroupList.OrderBy(x => x.Number).ToList();
            allPasswords["Без группы"] = noGroupList;

            if (!groupOrder.Contains("Без группы"))
                groupOrder.Insert(0, "Без группы");

            return (allPasswords, groupImages, groupOrder);
        }

        public void ClearAll()
        {
            _passwords.DeleteAll();
            _groups.DeleteAll();
            _noGroupPasswords.DeleteAll();
            DeleteGroupOrder();
        }

        public void Dispose() => _db?.Dispose();
    }

    // Документы LiteDB
    public class GroupDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
    }

    public class PasswordEntryDocument
    {
        public Guid Id { get; set; }
        public string GroupId { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public int Number { get; set; }
        public bool IsSelected { get; set; }
    }

    public class OrderDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "group_order";
        public List<string> GroupOrder { get; set; } = new List<string>();
    }
}