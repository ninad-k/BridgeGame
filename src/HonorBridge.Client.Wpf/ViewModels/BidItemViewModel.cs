using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class BidItemViewModel : ObservableObject
{
    public string Label { get; }
    public int Level { get; }
    public string Strain { get; }
    
    // "Bid", "Pass", "Double", "Redouble"
    public string CallType { get; } 
    
    [ObservableProperty]
    private bool _isEnabled = true;

    private System.Windows.Media.Brush _baseColor;

    public System.Windows.Media.Brush Foreground => IsEnabled ? _baseColor : System.Windows.Media.Brushes.LightGray;

    // Command to execute when clicked
    public ICommand BidCommand { get; }

    public BidItemViewModel(string label, int level, string strain, string callType, ICommand bidCommand)
    {
        Label = label;
        Level = level;
        Strain = strain;
        CallType = callType;
        BidCommand = bidCommand;
        
        bool isRed = (strain == "Diamonds" || strain == "Hearts" || strain == "Diamond" || strain == "Heart");
        _baseColor = isRed ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(Foreground));
    }
}
