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
    
    // Split Dummy Hands for correct visual placement
    public ObservableCollection<CardViewModel> DummyHandNorth { get; } = new();
    public ObservableCollection<CardViewModel> DummyHandEast { get; } = new();
    public ObservableCollection<CardViewModel> DummyHandWest { get; } = new();
    public ObservableCollection<CardViewModel> DummyHandSouth { get; } = new(); // Rare (if I am Dummy?) - Usually MyHand is shown.

    // Played Cards for Trick (Dictionary mapping Compass -> CardViewModel)
    // We can't bind Dictionary directly to UI easily for updates if Keys change, but values change.
    // ObservableDictionary? Or just properties?
    // Let's use properties for the 4 compass positions to make XAML binding easy.
    [ObservableProperty] private CardViewModel? _cardNorth;
    [ObservableProperty] private CardViewModel? _cardSouth;
    [ObservableProperty] private CardViewModel? _cardEast;
    [ObservableProperty] private CardViewModel? _cardWest;
    
    // Display properties for Last Call bubble
    [ObservableProperty] private string _lastCallNorth = "";
    [ObservableProperty] private string _lastCallSouth = "";
    [ObservableProperty] private string _lastCallEast = "";
    [ObservableProperty] private string _lastCallWest = "";
    
    [ObservableProperty] private bool _isWinner;
    [ObservableProperty] private string _winnerName = "";
    [ObservableProperty] private bool _isAuctionReview;
    
    // Hand Valuation

    
    // Hand Valuation
    [ObservableProperty] private int _myHCP;
    [ObservableProperty] private int _myTotalPoints;



    // Active Turn Indicators
    [ObservableProperty] private bool _isTurnNorth;
    [ObservableProperty] private bool _isTurnSouth;
    [ObservableProperty] private bool _isTurnEast;
    [ObservableProperty] private bool _isTurnWest;

    public ObservableCollection<string> AuctionHistory { get; } = new();

    // Bidding
    public ObservableCollection<BidItemViewModel> BiddingBox { get; } = new();

    // Removed old dropdown properties
    // [ObservableProperty] private string _selectedLevel = "1";
    // [ObservableProperty] private string _selectedStrain = "NoTrump";
    // public ObservableCollection<string> Levels { get; } ...
    // public ObservableCollection<string> Strains { get; } ...

    public GameTableViewModel(SignalRClientService signalR)
    {
        _signalR = signalR;
        _state = new GameStateDto();
        InitializeBiddingBox();
    }

    private void InitializeBiddingBox()
    {
        // 1-7 levels, 5 strains per level = 35 bids
        // Plus Pass, X (Double), XX (Redouble)
        
        // Structure: We can just have a flat list and let the UI arrange it, 
        // or a list of "BidRows" if we want strict grid control.
        // A flat list with a WrapPanel or UniformGrid is easiest if we order them logically.
        // Standard Bidding Box: 
        // 1C 1D 1H 1S 1NT
        // 2C 2D 2H ...
        // ...
        // Pass Double Redouble (Separate row?)
        
        // Let's ensure the Collection has them in order: 1C..1NT, 2C.., etc.
        
        string[] strains = { "Clubs", "Diamonds", "Hearts", "Spades", "NoTrump" };
        string[] symbols = { "♣", "♦", "♥", "♠", "NT" };
        
        for (int l = 1; l <= 7; l++)
        {
            for (int s = 0; s < 5; s++)
            {
                string strainName = strains[s];
                string symbol = symbols[s];
                string label = $"{l}{symbol}";
                
                // Closure variable capture for command
                int level = l; 
                string strain = strainName;
                
                var cmd = new RelayCommand(async () => await Bid(level, strain, "Bid"));
                BiddingBox.Add(new BidItemViewModel(label, level, strain, "Bid", cmd));
            }
        }
        
        // Pass, Double, Redouble
        BiddingBox.Add(new BidItemViewModel("Pass", 0, "", "Pass", new RelayCommand(async () => await Bid(0, "", "Pass"))));
        BiddingBox.Add(new BidItemViewModel("X", 0, "", "Double", new RelayCommand(async () => await Bid(0, "", "Double"))));
        BiddingBox.Add(new BidItemViewModel("XX", 0, "", "Redouble", new RelayCommand(async () => await Bid(0, "", "Redouble"))));
    }
    
    public void UpdateState(GameStateDto state)
    {
        State = state;
        
        MyHand.Clear();
        var myCards = state.MyHand.Select(c => new CardViewModel(c)).ToList();
        
        bool isMyTurn = (!string.IsNullOrEmpty(state.MySeat) && state.NextToAct == state.MySeat);
        
        foreach(var c in myCards) 
        {
            c.IsEnabled = isMyTurn;
            MyHand.Add(c);
        }
        
        DummyHandNorth.Clear();
        DummyHandEast.Clear();
        DummyHandWest.Clear();
        DummyHandSouth.Clear();

        // 1. DUMMY HAND (Visible to everyone)
        var dummyCards = state.DummyHand.Select(c => new CardViewModel(c)).ToList();
        
        string dummySeat = "";
        if (!string.IsNullOrEmpty(state.Declarer))
        {
             dummySeat = GetPartnerSeat(state.Declarer);
        }
        
        ObservableCollection<CardViewModel> targetCollection = null;
        if (dummySeat == "North") targetCollection = DummyHandNorth;
        else if (dummySeat == "East") targetCollection = DummyHandEast;
        else if (dummySeat == "West") targetCollection = DummyHandWest;
        // else if (dummySeat == "South") targetCollection = DummyHandSouth; 
        // FIX: If Dummy is South (Me), MyHand already shows my cards. 
        // Logic: If dummySeat == MySeat, do NOT populate Dummy Hand collection for Visual Display?
        // Wait, standard bridge: Dummy Hand is "Laid Down". My Hand is "Held".
        // If I am Dummy, I "Lay Down" my cards.
        // So actually, MyHand should be EMPTY, and DummyHandSouth should be FULL?
        // OR MyHand stays full, and we just hide DummyHandSouth.
        // User saw duplicates.
        // Let's stick with: If (dummySeat == "South"), and I am South, I rely on MyHand.
        // However, MyHand is for "Playing". DummyHand is for "Displaying".
        // If I am Dummy, can I play? Normally NO. Declarer plays.
        // But here, Declarer is Bot-North. And I am playing *for* Bot-North.
        // And I am also playing for Myself (Dummy). (Effective Declarer plays Dummy).
        // So I control BOTH.
        // If I control both, I need both visible.
        // But if MyHand == DummyHand, seeing it twice is weird.
        // Let's disable DummyHandSouth if dummySeat == MySeat.
        
        else if (dummySeat == "South" && !(State.MySeat == "South")) targetCollection = DummyHandSouth; // Only show if I am NOT South
        else if (dummySeat == "South") { } // I am South-Dummy. My cards are in MyHand. Don't dup.
        
        // Populate Dummy
        if (targetCollection != null)
        {
            foreach(var c in dummyCards)
            {
                // Can I control Dummy?
                // Logic: Declarer controls Dummy.
                // If I am Declarer (Declarer==MySeat), I control Dummy.
                // If I am Partner of Declarer (Declarer==PartnerSeat), and EffectiveDeclarer logic gave me control?
                // The Turn Logic is separate.
                
                // Simplified: If Turn == DummySeat, and I control Dummy, Enable.
                // If Declarer == MySeat, I control Dummy.
                
                bool isDummyTurn = (!string.IsNullOrEmpty(state.NextToAct) && state.NextToAct == dummySeat);
                
                // If I am Declarer, I play Dummy.
                if (!string.IsNullOrEmpty(State.MySeat) && State.Declarer == State.MySeat)
                {
                    c.IsEnabled = isDummyTurn;
                }
                else 
                {
                    c.IsEnabled = false; 
                }
                
                targetCollection.Add(c);
            }
        }
        
        // 2. PARTNER HAND (Visible ONLY to me if I am taking over)
        if (state.PartnerHand != null && state.PartnerHand.Count > 0)
        {
            // PartnerHand is the Declarer's Hand (North, if I am South).
            // It should be displayed in the Partner's Seat (North).
            string partnerSeat = GetPartnerSeat(State.MySeat ?? "");
            
            ObservableCollection<CardViewModel> partnerCollection = null;
            if (partnerSeat == "North") partnerCollection = DummyHandNorth; // Reuse visual slot
            else if (partnerSeat == "East") partnerCollection = DummyHandEast;
            else if (partnerSeat == "West") partnerCollection = DummyHandWest;
            else if (partnerSeat == "South") partnerCollection = DummyHandSouth;
            
            if (partnerCollection != null)
            {
                // Only clear if empty? Or simple append? 
                // "DummyHandNorth" might be empty if Dummy is South.
                // So this populates the empty Top slot with Partner's cards.
                
                var partnerCards = state.PartnerHand.Select(c => new CardViewModel(c)).ToList();
                foreach(var c in partnerCards)
                {
                    // Enable if it is Partner's Turn?
                    // Yes, I am playing for Partner.
                    bool isPartnerTurn = (!string.IsNullOrEmpty(state.NextToAct) && state.NextToAct == partnerSeat);
                    c.IsEnabled = isPartnerTurn;
                    partnerCollection.Add(c);
                }
            }
        }
        
        // Update Trick
        CardNorth = null;
        CardSouth = null;
        CardEast = null;
        CardWest = null;
        
        if (state.CurrentTrick.TryGetValue("North", out var cN)) CardNorth = new CardViewModel(cN);
        if (state.CurrentTrick.TryGetValue("South", out var cS)) CardSouth = new CardViewModel(cS);
        if (state.CurrentTrick.TryGetValue("East", out var cE)) CardEast = new CardViewModel(cE);
        if (state.CurrentTrick.TryGetValue("West", out var cW)) CardWest = new CardViewModel(cW);
        
        UpdateBiddingAvailability();
        UpdateCallHistory();
        UpdateHCP();


        // Update Turn Indicators
        IsTurnNorth = (!string.IsNullOrEmpty(State.NextToAct) && State.NextToAct == "North");
        IsTurnSouth = (!string.IsNullOrEmpty(State.NextToAct) && State.NextToAct == "South");
        IsTurnEast = (!string.IsNullOrEmpty(State.NextToAct) && State.NextToAct == "East");
        IsTurnWest = (!string.IsNullOrEmpty(State.NextToAct) && State.NextToAct == "West");
        
        IsAuctionReview = (State.Phase == "AuctionReview");
        
        System.Diagnostics.Debug.WriteLine($"[DEBUG] State Update: MySeat={State.MySeat}, Next={State.NextToAct}, Phase={State.Phase}, IsMyTurn={(!string.IsNullOrEmpty(state.MySeat) && state.NextToAct == state.MySeat)}");

        System.Console.WriteLine($"[DEBUG] State Update: MySeat={State.MySeat}, Next={State.NextToAct}, Phase={State.Phase}, IsMyTurn={(!string.IsNullOrEmpty(state.MySeat) && state.NextToAct == state.MySeat)}");
    }

    public class BiddingRow
    {
        public string West { get; set; } = "";
        public string North { get; set; } = "";
        public string East { get; set; } = "";
        public string South { get; set; } = "";
    }

    public ObservableCollection<BiddingRow> BiddingSummary { get; } = new();

    private void UpdateCallHistory()
    {
        // 1. Clear current displays
        LastCallNorth = "";
        LastCallSouth = "";
        LastCallEast = "";
        LastCallWest = "";
        AuctionHistory.Clear();
        BiddingSummary.Clear();
        
        if (State.CallHistory == null || State.CallHistory.Count == 0) return;
        
        // Populate Summary Table
        // Headers are static: West, North, East, South (or N E S W)
        // Let's use West, North, East, South as standard reading order? 
        // Or North East South West. Let's do North East South West.
        
        // We need to know who started.
        if (!System.Enum.TryParse<HonorBridge.Engine.Compass>(State.Dealer, out var dealerCompass))
        {
            dealerCompass = HonorBridge.Engine.Compass.North; 
        }

        var currentRow = new BiddingRow();
        int callsInRow = 0;
        
        // If Dealer is NOT North, we need to pad the first row?
        // Wait, standard bridge pads the *start*.
        // If Order is N E S W.
        // If Dealer is East. N is empty.
        
        // Let's assume columns: NORTH | EAST | SOUTH | WEST
        
        // Fill padding
        // If Dealer is East (1). N(0) is empty.
        // If Dealer is South (2). N, E empty.
        // If Dealer is West (3). N, E, S empty.
        
        // Mapping Compass to 0-3 index (N=0, E=1, S=2, W=3)
        // Compass enum: North=0, East=1, South=2, West=3.
        
        int currentColumn = (int)dealerCompass;
        
        foreach (var call in State.CallHistory)
        {
            // Place call in currentColumn of currentRow
            if (currentColumn == 0) currentRow.North = call;
            else if (currentColumn == 1) currentRow.East = call;
            else if (currentColumn == 2) currentRow.South = call;
            else if (currentColumn == 3) currentRow.West = call;
            
            currentColumn++;
            if (currentColumn > 3)
            {
                BiddingSummary.Add(currentRow);
                currentRow = new BiddingRow();
                currentColumn = 0;
            }
        }
        
        // Add pending row if not empty
        if (!string.IsNullOrEmpty(currentRow.North) || !string.IsNullOrEmpty(currentRow.East) || 
            !string.IsNullOrEmpty(currentRow.South) || !string.IsNullOrEmpty(currentRow.West))
        {
            BiddingSummary.Add(currentRow);
        }

        // UPDATE WINNER STATUS (Existing Logic)
        IsWinner = false;
        WinnerName = "";
        
        if (!string.IsNullOrEmpty(State.LastScore) && !string.IsNullOrEmpty(State.Declarer))
        {
             if (System.Enum.TryParse<HonorBridge.Engine.Compass>(State.Declarer, out var declCompass))
             {
                 bool amIMeOrPartner = (declCompass == HonorBridge.Engine.Compass.South || declCompass == HonorBridge.Engine.Compass.North);
                 int points = State.LastPoints;
                 
                  if (amIMeOrPartner && points > 0) 
                 {
                     IsWinner = true;
                     WinnerName = "Sanjay";
                 }
                 else if (!amIMeOrPartner && points < 0)
                 {
                     IsWinner = true;
                     WinnerName = "Sanjay";
                 }
             }
        }
        
        // 2. Parse dealer for Last Call Bubbles
        var currentBidder = dealerCompass;
        
        // 3. Iterate history for Bubbles
        foreach (var call in State.CallHistory)
        {
            AuctionHistory.Add($"{currentBidder}: {call}");
            
            switch (currentBidder)
            {
                case HonorBridge.Engine.Compass.North: LastCallNorth = call; break;
                case HonorBridge.Engine.Compass.South: LastCallSouth = call; break;
                case HonorBridge.Engine.Compass.East: LastCallEast = call; break;
                case HonorBridge.Engine.Compass.West: LastCallWest = call; break;
            }
            currentBidder = (HonorBridge.Engine.Compass)(((int)currentBidder + 1) % 4);
        }
    }
    
    private void UpdateBiddingAvailability()
    {
        // Check current contract from State
        // Logic:
        // If no bid yet (Contract is empty or null): All Level bids valid. X, XX invalid.
        // If Contract exists:
        //   Identify Level and Strain of high bid.
        //   Any bid with Level > HighLevel is Valid.
        //   Any bid with Level == HighLevel AND StrainIndex > HighStrainIndex is Valid.
        //   Else Invalid.
        
        // X (Double): Valid if Opponent is current High Bidder AND not already doubled.
        // XX (Redouble): Valid if We (or Partner) are High Bidder AND currently Double.
        
        // Who is High Bidder? 
        // State.Declarer maps to the declarer, but the *last bidder* might be different from Declarer?
        // Actually, in Bridge, the "Contract" field usually reflects the last standing bid.
        // And "Declarer" is computed.
        // We need to know WHO made the last bid to validate X/XX.
        // State.CallHistory has the list.
        // But `GameStateDto` might not have enough info if it just gives strings.
        // `State.Contract` string like "1NT X".
        // `State.NextToAct` tells us whose turn it is. (Should be us if we are enabling buttons).
        
        // Simplified Logic for Demo:
        // We need to parse `State.Contract` (e.g., "1NT", "4H X").
        // If contract is null/empty -> 1C+ available. X/XX disabled.
        
        int currentLevel = 0;
        int currentStrainIndex = -1;
        bool isDoubled = false;
        bool isRedoubled = false;
        
        if (!string.IsNullOrEmpty(State.Contract))
        {
            // Parse "1NT", "1NT X", "7S XX"
            var parts = State.Contract.Split(' ');
            string bidPart = parts[0]; // "1NT"
            
            if (bidPart.Length >= 2) // "1C"
            {
                 currentLevel = int.Parse(bidPart[0].ToString());
                 string s = bidPart.Substring(1); // "C" or "NT"
                 currentStrainIndex = GetStrainIndex(s);
            }
            
            if (State.Contract.Contains("XX")) isRedoubled = true;
            else if (State.Contract.Contains("X")) isDoubled = true;
            
            // (Ignoring side checks for MVP UI task reliability).
            // (But we MUST check turn order to prevent stuck UI).
        }
        
        // TURN ORDER CHECK
        // If it is NOT my turn, disable everything.
        // MySeat is now in State.MySeat
        bool isMyTurn = false;
        if (!string.IsNullOrEmpty(State.MySeat) && !string.IsNullOrEmpty(State.NextToAct))
        {
            if (State.MySeat == State.NextToAct) isMyTurn = true;
        }
        
        // Debug override? No, strict.
        
        foreach (var item in BiddingBox)
        {
            // Default disabled
            item.IsEnabled = false;
            
            if (!isMyTurn) continue;

            if (item.CallType == "Pass")
            {
                item.IsEnabled = true;
            }
            else if (item.CallType == "Bid")
            {
                int itemStrainIndex = GetStrainIndexShort(item.Strain);
                if (item.Level > currentLevel) item.IsEnabled = true;
                else if (item.Level == currentLevel && itemStrainIndex > currentStrainIndex) item.IsEnabled = true;
                else item.IsEnabled = false;
            }
            else if (item.CallType == "Double")
            {
                item.IsEnabled = !string.IsNullOrEmpty(State.Contract) && !isDoubled && !isRedoubled;
            }
            else if (item.CallType == "Redouble")
            {
                item.IsEnabled = isDoubled && !isRedoubled;
            }
        }
    }
    
    // Helper for "Clubs", "Diamonds"...
    private int GetStrainIndex(string s)
    {
        // s could be "Clubs" (full) or "C" (short from parsing "1C")?
        // GameStateDto.Contract usually is "1NT", "4H". So it uses Short names if from `ToString()`.
        // Let's normalize.
        if (s == "C" || s == "Clubs") return 0;
        if (s == "D" || s == "Diamonds") return 1;
        if (s == "H" || s == "Hearts") return 2;
        if (s == "S" || s == "Spades") return 3;
        if (s == "NT" || s == "NoTrump") return 4;
        return -1;
    }
    
    private int GetStrainIndexShort(string full) // "Clubs" -> 0
    {
        return GetStrainIndex(full);
    }

    [RelayCommand]
    private async Task Sit(string compass)
    {
        await _signalR.Sit(compass);
    }
    
    // [RelayCommand]
    // private async Task Bid(...) -> Replaced by internal method for closures
    
    private async Task Bid(int level, string strain, string type) 
    {
        // Optimistic Disable: Prevent double clicks and show "Wait" state immediately
        foreach(var item in BiddingBox) item.IsEnabled = false;
        
        await _signalR.PlaceBid(level, strain, type);
    }
    
    [RelayCommand]
    private async Task PlayCard(string card)
    {
        await _signalR.PlayCard(card);
    }
    
    [RelayCommand]
    private async Task StartPlay()
    {
        await _signalR.StartPlay();
    }
    
    private void UpdateHCP()
    {
        int hcp = 0;
        var suits = new Dictionary<string, int> { {"C",0}, {"D",0}, {"H",0}, {"S",0} };
        
        foreach (var card in MyHand)
        {
            hcp += GetCardPoints(card.Rank);
            
            // Count distribution (Length points)
            // CardViewModel.Suit is "C", "D", "H", "S"
            if (suits.ContainsKey(card.Suit)) suits[card.Suit]++;
        }
        MyHCP = hcp;
        
        // Calculate Length Points: 1 pt for each card over 4 in value
        int lengthPoints = 0;
        foreach(var count in suits.Values)
        {
            if (count > 4) lengthPoints += (count - 4);
        }
        
        MyTotalPoints = MyHCP + lengthPoints;
    }
    
    private int GetCardPoints(string rank)
    {
        return rank switch
        {
            "A" => 4,
            "K" => 3,
            "Q" => 2,
            "J" => 1,
            "T" => 0, // 10
            _ => 0
        };
    }

    
    private string GetPartnerSeat(string seat)
    {
        if (seat == "North") return "South";
        if (seat == "South") return "North";
        if (seat == "East") return "West";
        if (seat == "West") return "East";
        return "";
    }
    
    private string GetPartner(string seat) => GetPartnerSeat(seat);
}
