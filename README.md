# P.T.S Password Tina Style

Настольный менеджер паролей с красивым интерфейсом. Работает локально — все данные хранятся только на вашем компьютере.

## Скачать

В [Релизах](../../releases) — **по одному архиву** на систему. Распакуйте архив и запустите файл: устанавливать .NET не требуется, всё вшито в исполняемый файл (~100 МБ).

| Архив | Что внутри | Система |
| --- | --- | --- |
| `TinaPASS-win-x64.7z` | `TinaPASS-win-x64.exe` | Windows 10/11 (64-bit) |
| `TinaPASS-linux-x64.7z` | `TinaPASS-linux-x64` | Linux (64-bit) |
| `TinaPASS-osx-x64.7z` | `TinaPASS-osx-x64` | macOS Intel |
| `TinaPASS-osx-arm64.7z` | `TinaPASS-osx-arm64` | macOS Apple Silicon (M1/M2/M3) |

### Windows

Распакуйте архив и запустите `TinaPASS-win-x64.exe` двойным кликом.

Если Windows Defender/SmartScreen ругается (файл без подписи), нажмите **«Подробнее» → «Всё равно запустить»**.

### Linux

```bash
chmod +x TinaPASS-linux-x64
./TinaPASS-linux-x64
```

### macOS

```bash
chmod +x TinaPASS-osx-arm64
./TinaPASS-osx-arm64
```

Если macOS заблокирует приложение («нельзя проверить разработчика»), разрешите запуск в **Системные настройки → Конфиденциальность и безопасность**.

## Возможности

- Хранение паролей локально (без облачных аккаунтов и синхронизации).
- Группы паролей с собственными обложками.
- Поиск по записям.
- Импорт из `txt`-файлов.
- Смена тем и анимированные фоны.
- Генератор паролей.

## Сборка из исходников

Нужен [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build -c Release TinaPASS.csproj
```

Самостоятельная однофайловая сборка:

```bash
dotnet publish TinaPASS.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

## Данные

Пароли хранятся в локальной базе `tinapass.db` в папке `%LOCALAPPDATA%\TinaPASS\` (Windows), `~/.local/share/TinaPASS/` (Linux) или `~/Library/Application Support/TinaPASS/` (macOS). База шифруется, но при общей учётной записи Windows доступ к файлу возможен у всех, кто пользуется этим компьютером. Не копируйте `tinapass.db` в ненадёжные места.

## Ограничения

- Облачная синхронизации нет — данные не покидают ваш компьютер.
- Нет встроенной блокировки по мастер-паролю при каждом запуске.

## Лицензия

MIT
