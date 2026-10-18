using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;

namespace TinaPASS.Main
{
    public class BackgroundEffectManager
    {
        private static BackgroundEffectManager? _instance;
        public static BackgroundEffectManager Instance => _instance ??= new BackgroundEffectManager();

        private BackgroundEffect? _currentEffect;
        private Canvas? _canvas;
        private readonly Dictionary<string, Type> _effectTypes;
        private string _currentEffectName = "Океан";

        private BackgroundEffectManager()
        {
            _effectTypes = new Dictionary<string, Type>
            {
                { "Океан", typeof(WaveEffect) },
                { "Закат", typeof(WaveEffect) },
                { "Неоновые волны", typeof(WaveEffect) },
                { "Лунная ночь", typeof(WaveEffect) },
                { "Степь", typeof(WaveEffect) },
                { "Градиент", typeof(StrategyGradientEffect) },
                { "Градиент 2", typeof(CleanGradientEffect) },
                { "Яндекс музыка", typeof(YandexMusicGradientEffect) },
                { "Figma (неон)", typeof(FigmaNeonEffect) }
            };
        }

        public IReadOnlyDictionary<string, Type> EffectTypes => _effectTypes;

        public void Initialize(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void SetEffect(string effectName)
        {
            if (_canvas == null) return;

            StopCurrentEffect();

            if (_effectTypes.TryGetValue(effectName, out var effectType))
            {
                //  ПРАВИЛЬНЫЙ СПОСОБ - ПЕРЕДАЕМ ПАРАМЕТРЫ В МАССИВЕ
                _currentEffect = (BackgroundEffect)Activator.CreateInstance(effectType, new object[] { _canvas, effectName })!;
                _currentEffect.Start();
                _currentEffectName = effectName;
                SaveEffect(effectName);
            }
        }

        private void StopCurrentEffect()
        {
            _currentEffect?.Stop();
            _currentEffect = null;
        }

        public void Pause()
        {
            _currentEffect?.Pause();
        }

        public void Resume()
        {
            _currentEffect?.Resume();
        }

        public void Resize(double width, double height)
        {
            _currentEffect?.Resize(width, height);
        }

        public string GetCurrentEffectName()
        {
            return _currentEffectName;
        }

        public void SaveEffect(string effectName)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TinaPASS");
                
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);
                
                string effectFile = Path.Combine(appDataPath, "effect.txt");
                File.WriteAllText(effectFile, effectName);
            }
            catch { }
        }

        public string LoadSavedEffect()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TinaPASS");
                
                string effectFile = Path.Combine(appDataPath, "effect.txt");
                
                if (File.Exists(effectFile))
                {
                    string effectName = File.ReadAllText(effectFile);
                    if (_effectTypes.ContainsKey(effectName))
                        return effectName;
                }
            }
            catch { }
            return "Яндекс музыка";
        }
    }
}