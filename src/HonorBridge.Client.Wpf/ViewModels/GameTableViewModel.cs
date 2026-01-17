using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HonorBridge.Client.Wpf.Services;
using HonorBridge.Shared.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class GameTableViewModel : ObservableObject
{
    private readonly SignalRClientService _signalR;
    
    [ObservableProperty]
    private GameStateDto _state;
    
    // Derived properties for UI binding
    public ObservableCollection<CardViewModel> MyHand { get; } = new();
    public ObservableCollection<CardViewModel> DummyHand { get; } = new();
    
    // Played Cards for Trick (Dictionary mapping Compass -> CardViewModel)
    // We can't bind Dictionary directly to UI easily for updates if Keys change, but values change.
    // ObservableDictionary? Or just properties?
    // Let's use properties for the 4 compass positions to make XAML binding easy.
    [ObservableProperty] private CardViewModel? _cardNorth;
    [ObservableProperty] private CardViewModel? _cardSouth;
    [ObservableProperty] private CardViewModel? _cardEast;
    [ObservableProperty] private CardViewModel? _cardWest;
    
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
        foreach (var c in state.MyHand) MyHand.Add(new CardViewModel(c));
        
        DummyHand.Clear();
        foreach (var c in state.DummyHand) DummyHand.Add(new CardViewModel(c));
        
        // Update Trick
        CardNorth = null;
        CardSouth = null;
        CardEast = null;
        CardWest = null;
        
        if (state.CurrentTrick.TryGetValue("North", out var cN)) CardNorth = new CardViewModel(cN);
        if (state.CurrentTrick.TryGetValue("South", out var cS)) CardSouth = new CardViewModel(cS);
        if (state.CurrentTrick.TryGetValue("East", out var cE)) CardEast = new CardViewModel(cE);
        if (state.CurrentTrick.TryGetValue("West", out var cW)) CardWest = new CardViewModel(cW);
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
