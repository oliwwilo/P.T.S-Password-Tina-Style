using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace TinaPASS.Main
{
    public abstract class BackgroundEffect : IDisposable
    {
        protected Canvas Canvas { get; private set; }
        protected DispatcherTimer? Timer;
        protected Random Random = new Random();
        protected bool IsRunning = false;

        public BackgroundEffect(Canvas canvas)
        {
            Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        public virtual void Start()
        {
            IsRunning = true;
            Canvas.Children.Clear();
            InitializeEffect();
            Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(40)
            };
            Timer.Tick += OnTick;
            Timer.Start();
        }

        public virtual void Stop()
        {
            IsRunning = false;
            Timer?.Stop();
            Timer = null;
            Canvas.Children.Clear();
        }

        public virtual void Pause()
        {
            Timer?.Stop();
        }

        public virtual void Resume()
        {
            if (Timer != null && IsRunning)
                Timer.Start();
        }

        protected abstract void InitializeEffect();
        protected abstract void OnTick(object? sender, EventArgs e);

        public virtual void Resize(double width, double height)
        {
            // Можно переопределить в наследниках
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}