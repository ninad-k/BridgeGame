using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HonorBridge.Client.Wpf.Services;
using HonorBridge.Server.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class GameTableViewModel : ObservableObject
{
    private readonly SignalRClientService _signalR;
    
    [ObservableProperty]
    private GameStateDto _state;
    
    // Derived properties for UI binding
    public ObservableCollection<string> MyHand { get; } = new();
    public ObservableCollection<string> DummyHand { get; } = new();
    
    // Bidding
    [ObservableProperty] private string _selectedLevel = "1";
    [ObservableProperty] private string _selectedStrain = "NoTrump";
    public ObservableCollection<string> Levels { get; } = new ObservableCollection<string> { "1", "2", "3", "4", "5", "6", "7" };
    public ObservableCollection<string> Strains { get; } = new ObservableCollection<string> { "Clubs", "Diamonds", "Hearts", "Spades", "NoTrump" };

    public GameTableViewModel(SignalRClientService signalR)
    {
        _signalR = signalR;
        _state = new GameStateDto();
    }
    
    public void UpdateState(GameStateDto state)
    {
        State = state;
        
        MyHand.Clear();
        foreach (var c in state.MyHand) MyHand.Add(c);
        
        DummyHand.Clear();
        foreach (var c in state.DummyHand) DummyHand.Add(c);
    }

    [RelayCommand]
    private async Task Sit(string compass)
    {
        await _signalR.Sit(compass);
    }
    
    [RelayCommand]
    private async Task Bid(string type) // "Bid", "Pass", "Double", "Redouble"
    {
        int lvl = int.Parse(SelectedLevel);
        await _signalR.PlaceBid(lvl, SelectedStrain, type);
    }
    
    [RelayCommand]
    private async Task PlayCard(string card)
    {
        await _signalR.PlayCard(card);
    }
}
