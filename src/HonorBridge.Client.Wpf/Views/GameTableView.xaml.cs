using System.Windows.Controls;

namespace HonorBridge.Client.Wpf.Views;

public partial class GameTableView : UserControl
{
    public GameTableView()
    {
        InitializeComponent();
        
        // Listen for visibility changes on Fireworks to start animation
        // Fireworks control is named "Fireworks" in XAML
        // But we need to ensure it starts when visible.
        
        // Simple way: Poll or Event?
        // Let's hook DataContext or generic Loaded.
        
        this.LayoutUpdated += (s, e) => 
        {
             if (Fireworks != null && Fireworks.Visibility == Visibility.Visible)
             {
                 Fireworks.Start();
             }
        };
    }
}
