using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace HonorBridge.Client.Wpf.Controls;

public partial class FireworksControl : UserControl
{
    private readonly Random _rng = new();
    
    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register("IsPlaying", typeof(bool), typeof(FireworksControl), 
            new PropertyMetadata(false, OnIsPlayingChanged));

    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FireworksControl ctrl && (bool)e.NewValue)
        {
            ctrl.Start();
        }
    }

    public FireworksControl()
    {
        InitializeComponent();
    }

    public void Start()
    {
        ParticleCanvas.Children.Clear();
        // Spawn multiple explosions
        for (int i = 0; i < 5; i++)
        {
            SpawnExplosion();
        }
    }

    private async void SpawnExplosion()
    {
        double centerX = _rng.NextDouble() * ActualWidth;
        double centerY = _rng.NextDouble() * ActualHeight;
        Color color = Color.FromRgb((byte)_rng.Next(256), (byte)_rng.Next(256), (byte)_rng.Next(256));

        int particleCount = 20;
        for (int i = 0; i < particleCount; i++)
        {
            CreateParticle(centerX, centerY, color);
        }
        
        // Loop? Or just one shot triggered by parent. Parent triggers Start() periodically if needed.
        // Let's self-loop a bit for duration.
        await Task.Delay(500 + _rng.Next(1000));
        if (this.Visibility == Visibility.Visible)
             SpawnExplosion();
    }

    private void CreateParticle(double x, double y, Color color)
    {
        Ellipse p = new Ellipse
        {
            Width = 5, Height = 5,
            Fill = new SolidColorBrush(color)
        };
        
        Canvas.SetLeft(p, x);
        Canvas.SetTop(p, y);
        ParticleCanvas.Children.Add(p);

        // Animate
        double angle = _rng.NextDouble() * 360;
        double speed = 50 + _rng.NextDouble() * 100;
        double rad = angle * Math.PI / 180;
        double toX = Math.Cos(rad) * speed;
        double toY = Math.Sin(rad) * speed;

        TranslateTransform trans = new TranslateTransform();
        p.RenderTransform = trans;

        DoubleAnimation animX = new DoubleAnimation(0, toX, TimeSpan.FromSeconds(1));
        DoubleAnimation animY = new DoubleAnimation(0, toY, TimeSpan.FromSeconds(1));
        DoubleAnimation animOp = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));

        trans.BeginAnimation(TranslateTransform.XProperty, animX);
        trans.BeginAnimation(TranslateTransform.YProperty, animY);
        p.BeginAnimation(UIElement.OpacityProperty, animOp);
        
        // Auto remove? Hard to do simple auto-remove in simple WPF without events.
        // Just clearing canvas on Start is simpler for MVP memory management.
    }
}
