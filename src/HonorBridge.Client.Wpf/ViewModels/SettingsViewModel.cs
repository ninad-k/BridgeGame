using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // Simplified: Just string names. In real app, would map to Enum or ID.
    public ObservableCollection<string> BiddingSystems { get; } = new()
    {
        "SAYC",
        "Acol",
        "Goren"
    };

    public string SelectedSystem
    {
        get => GameOptions.BiddingSystem;
        set
        {
            if (GameOptions.BiddingSystem != value)
            {
                GameOptions.BiddingSystem = value;
                OnPropertyChanged();
            }
        }
    }
}
