using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace TinaPASS.Main
{
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event EventHandler<Theme>? ThemeChanged;

        private Theme _currentTheme;
        private readonly Dictionary<string, Theme> _themes;

        private ThemeManager()
        {
            _themes = new Dictionary<string, Theme>
            {
                { "Dark", CreateDarkTheme() },
                { "Light", CreateLightTheme() },
                { "Purple", CreatePurpleTheme() },
                { "Green", CreateGreenTheme() },
                { "Ocean", CreateOceanTheme() },
                { "DeepNight", CreateDeepNightTheme() },
                { "SoftLime", CreateSoftLimeTheme() },
                { "Amber", CreateAmberTheme() },
                { "Minimal", CreateMinimalTheme() },
                { "Figma", CreateFigmaTheme() }
            };

            _currentTheme = _themes["Figma"];
            LoadSavedTheme();
        }

        public IReadOnlyDictionary<string, Theme> Themes => _themes;
        public Theme CurrentTheme => _currentTheme;

        public void SetTheme(string themeName)
        {
            if (_themes.TryGetValue(themeName, out var theme))
            {
                _currentTheme = theme;
                ApplyTheme(theme);
                ThemeChanged?.Invoke(this, theme);
                SaveTheme(themeName);
            }
        }

        private void ApplyTheme(Theme theme)
        {
            try
            {
                if (Application.Current is App app)
                {
                    app.ApplyTheme(theme);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка применения темы: {ex.Message}");
            }
        }

        private void SaveTheme(string themeName)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TinaPASS");
        
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);
        
                string themeFile = Path.Combine(appDataPath, "theme.txt");
                File.WriteAllText(themeFile, themeName);
        
                Console.WriteLine($" Тема сохранена: {themeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка сохранения темы: {ex.Message}");
            }
        }


        private void LoadSavedTheme()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TinaPASS");
        
                string themeFile = Path.Combine(appDataPath, "theme.txt");
        
                if (File.Exists(themeFile))
                {
                    string themeName = File.ReadAllText(themeFile);
                    if (_themes.ContainsKey(themeName))
                    {
                        _currentTheme = _themes[themeName];
                        Console.WriteLine($" Тема загружена: {themeName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка загрузки темы: {ex.Message}");
            }
        }

        private Theme CreateDarkTheme()
        {
            return new Theme
            {
                Name = "Dark",
                Background = Color.FromRgb(18, 18, 18),
                Surface = Color.FromRgb(30, 30, 30),
                SurfaceLight = Color.FromRgb(45, 45, 45),
                Primary = Color.FromRgb(203, 255, 33),
                PrimaryDark = Color.FromRgb(150, 200, 20),
                Text = Colors.White,
                TextSecondary = Color.FromRgb(204, 204, 204),
                Accent = Color.FromRgb(255, 68, 68),
                Success = Color.FromRgb(76, 175, 80),
                Border = Color.FromRgb(60, 60, 60),
                IsDark = true
            };
        }

        private Theme CreateLightTheme()
        {
            return new Theme
            {
                Name = "Light",
                Background = Color.FromRgb(240, 240, 240),
                Surface = Color.FromRgb(255, 255, 255),
                SurfaceLight = Color.FromRgb(245, 245, 245),
                Primary = Color.FromRgb(33, 150, 243),
                PrimaryDark = Color.FromRgb(25, 118, 210),
                Text = Color.FromRgb(33, 33, 33),
                TextSecondary = Color.FromRgb(117, 117, 117),
                Accent = Color.FromRgb(244, 67, 54),
                Success = Color.FromRgb(76, 175, 80),
                Border = Color.FromRgb(200, 200, 200),
                IsDark = false
            };
        }

        private Theme CreatePurpleTheme()
        {
            return new Theme
            {
                Name = "Purple",
                Background = Color.FromRgb(30, 20, 40),
                Surface = Color.FromRgb(50, 35, 65),
                SurfaceLight = Color.FromRgb(70, 50, 90),
                Primary = Color.FromRgb(156, 39, 176),
                PrimaryDark = Color.FromRgb(123, 31, 162),
                Text = Colors.White,
                TextSecondary = Color.FromRgb(200, 180, 220),
                Accent = Color.FromRgb(255, 68, 68),
                Success = Color.FromRgb(76, 175, 80),
                Border = Color.FromRgb(70, 50, 90),
                IsDark = true
            };
        }

        private Theme CreateGreenTheme()
        {
            return new Theme
            {
                Name = "Green",
                Background = Color.FromRgb(20, 35, 20),
                Surface = Color.FromRgb(35, 55, 35),
                SurfaceLight = Color.FromRgb(50, 75, 50),
                Primary = Color.FromRgb(76, 175, 80),
                PrimaryDark = Color.FromRgb(56, 142, 60),
                Text = Colors.White,
                TextSecondary = Color.FromRgb(180, 220, 180),
                Accent = Color.FromRgb(255, 68, 68),
                Success = Color.FromRgb(76, 175, 80),
                Border = Color.FromRgb(50, 75, 50),
                IsDark = true
            };
        }

        private Theme CreateOceanTheme()
        {
            return new Theme
            {
                 Name = "Ocean",
                 Background = Color.FromRgb(10, 25, 45),
                 Surface = Color.FromRgb(20, 45, 70),
                 SurfaceLight = Color.FromRgb(35, 65, 95),
                 Primary = Color.FromRgb(33, 150, 243),
                 PrimaryDark = Color.FromRgb(25, 118, 210),
                 Text = Colors.White,
                 TextSecondary = Color.FromRgb(180, 210, 240),
                 Accent = Color.FromRgb(0, 188, 212),
                 Success = Color.FromRgb(76, 175, 80),
                 Border = Color.FromRgb(35, 65, 95),
                 IsDark = true
             };
         }

         private Theme CreateDeepNightTheme()
         {
             return new Theme
             {
                 Name = "DeepNight",
                 Background = Color.FromRgb(13, 16, 40),
                 Surface = Color.FromRgb(23, 28, 60),
                 SurfaceLight = Color.FromRgb(38, 46, 90),
                 Primary = Color.FromRgb(100, 120, 255),
                 PrimaryDark = Color.FromRgb(70, 90, 200),
                 Text = Colors.White,
                 TextSecondary = Color.FromRgb(180, 190, 230),
                 Accent = Color.FromRgb(77, 224, 202),
                 Success = Color.FromRgb(84, 200, 140),
                 Border = Color.FromRgb(46, 55, 120),
                 IsDark = true
             };
         }

         private Theme CreateSoftLimeTheme()
         {
             return new Theme
             {
                 Name = "SoftLime",
                 Background = Color.FromRgb(20, 24, 20),
                 Surface = Color.FromRgb(32, 38, 32),
                 SurfaceLight = Color.FromRgb(48, 56, 44),
                 Primary = Color.FromRgb(160, 216, 132),
                 PrimaryDark = Color.FromRgb(120, 170, 90),
                 Text = Colors.White,
                 TextSecondary = Color.FromRgb(190, 210, 185),
                 Accent = Color.FromRgb(193, 255, 33),
                 Success = Color.FromRgb(140, 210, 130),
                 Border = Color.FromRgb(55, 65, 52),
                 IsDark = true
             };
         }

         private Theme CreateAmberTheme()
         {
             return new Theme
             {
                 Name = "Amber",
                 Background = Color.FromRgb(32, 20, 12),
                 Surface = Color.FromRgb(48, 32, 20),
                 SurfaceLight = Color.FromRgb(66, 46, 28),
                 Primary = Color.FromRgb(255, 173, 82),
                 PrimaryDark = Color.FromRgb(230, 145, 56),
                 Text = Colors.White,
                 TextSecondary = Color.FromRgb(225, 200, 170),
                 Accent = Color.FromRgb(255, 116, 60),
                 Success = Color.FromRgb(190, 180, 90),
                 Border = Color.FromRgb(70, 50, 30),
                 IsDark = true
             };
         }

         private Theme CreateFigmaTheme()
         {
             return new Theme
             {
                 Name = "Figma",
                 Background = Color.FromRgb(254, 1, 164),
                 Surface = Color.FromRgb(1, 0, 1),
                 SurfaceLight = Color.FromRgb(26, 14, 32),
                 Primary = Color.FromRgb(105, 219, 254),
                 PrimaryDark = Color.FromRgb(63, 163, 202),
                 Text = Color.FromRgb(250, 250, 254),
                 TextSecondary = Color.FromRgb(216, 234, 253),
                 Accent = Color.FromRgb(247, 255, 0),
                 Success = Color.FromRgb(77, 191, 224),
                 Border = Color.FromRgb(58, 34, 56),
                 IsDark = true
             };
         }

         private Theme CreateMinimalTheme()
         {
             return new Theme
             {
                 Name = "Minimal",
                 Background = Color.FromRgb(24, 24, 28),
                 Surface = Color.FromRgb(38, 38, 44),
                 SurfaceLight = Color.FromRgb(52, 52, 60),
                 Primary = Color.FromRgb(96, 125, 204),
                 PrimaryDark = Color.FromRgb(76, 100, 170),
                 Text = Colors.White,
                 TextSecondary = Color.FromRgb(180, 180, 195),
                 Accent = Color.FromRgb(140, 200, 255),
                 Success = Color.FromRgb(110, 180, 150),
                 Border = Color.FromRgb(60, 60, 72),
                 IsDark = true
            };
        }
    }

    public class Theme
    {
        public string Name { get; set; } = "";
        public Color Background { get; set; }
        public Color Surface { get; set; }
        public Color SurfaceLight { get; set; }
        public Color Primary { get; set; }
        public Color PrimaryDark { get; set; }
        public Color Text { get; set; }
        public Color TextSecondary { get; set; }
        public Color Accent { get; set; }
        public Color Success { get; set; }
        public Color Border { get; set; }
        public bool IsDark { get; set; }

        public SolidColorBrush BackgroundBrush => new SolidColorBrush(Background);
        public SolidColorBrush SurfaceBrush => new SolidColorBrush(Surface);
        public SolidColorBrush SurfaceLightBrush => new SolidColorBrush(SurfaceLight);
        public SolidColorBrush PrimaryBrush => new SolidColorBrush(Primary);
        public SolidColorBrush PrimaryDarkBrush => new SolidColorBrush(PrimaryDark);
        public SolidColorBrush TextBrush => new SolidColorBrush(Text);
        public SolidColorBrush TextSecondaryBrush => new SolidColorBrush(TextSecondary);
        public SolidColorBrush AccentBrush => new SolidColorBrush(Accent);
        public SolidColorBrush SuccessBrush => new SolidColorBrush(Success);
        public SolidColorBrush BorderBrush => new SolidColorBrush(Border);
    }
}