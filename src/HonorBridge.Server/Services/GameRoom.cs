using System;
using System.Collections.Generic;
using System.Linq;
using HonorBridge.Engine;
using HonorBridge.Shared.Models;
using HonorBridge.AI;

namespace HonorBridge.Server.Services;

public class GameRoom
{
    public string RoomId { get; }
    
    private readonly Dictionary<Compass, string?> _seats = new()
    {
        { Compass.North, null },
        { Compass.East, null },
        { Compass.South, null },
        { Compass.West, null }
    };

    // Engine State
    public Deck Deck { get; private set; }
    public Dictionary<Compass, Hand> Hands { get; private set; } = new();
    public Auction? CurrentAuction { get; private set; }
    public DealPlay? CurrentPlay { get; private set; }
    public Compass Dealer { get; private set; } = Compass.North;
    public Vulnerability Vulnerability { get; private set; } = Vulnerability.None;
    
    public enum RoomPhase { Waiting, Bidding, Play, Scoring }
    public RoomPhase Phase { get; private set; } = RoomPhase.Waiting;
    
    public ScoreResult? LastResult { get; private set; }
    
    // Setting
    public IBiddingSystem CurrentBiddingSystem { get; set; } = ParametricBidder.SAYC;

    private readonly Dictionary<Compass, IBridgePlayer> _aiPlayers = new();

    public GameRoom(string roomId)
    {
        RoomId = roomId;
        Deck = new Deck();
    }

    public void SetBiddingSystem(string systemName)
    {
        IBiddingSystem sys = systemName switch
        {
            "Acol" => ParametricBidder.Acol,
            "Goren" => ParametricBidder.Goren,
            "Strong Club" => ParametricBidder.StrongClub,
            _ => ParametricBidder.SAYC
        };
        
        CurrentBiddingSystem = sys;
        
        // Update existing AIs
        foreach (var seat in _aiPlayers.Keys.ToList())
        {
            _aiPlayers[seat] = new HonorBridge.AI.MonteCarloAI(sys);
        }
    }
    
    public void AddAI(Compass seat)
    {
        if (_seats[seat] != null) return; // Occupied by human
        
        _seats[seat] = $"Bot-{seat}";
        _aiPlayers[seat] = new HonorBridge.AI.MonteCarloAI(CurrentBiddingSystem);
        CheckStart();
    }

    public bool Sit(Compass seat, string playerName)
    {
        if (_seats[seat] != null && _seats[seat] != playerName)
            return false; // Seat taken

        var existing = _seats.FirstOrDefault(x => x.Value == playerName);
        if (!existing.Equals(default(KeyValuePair<Compass, string?>)))
        {
             _seats[existing.Key] = null;
        }

        _seats[seat] = playerName;
        // If human sits, remove AI if any (though usually we don't overwrite AI unless explicit removal, but for simplicity override AI)
        if (_aiPlayers.ContainsKey(seat)) _aiPlayers.Remove(seat);
        
        CheckStart();
        return true;
    }
    
    public void RemovePlayer(string playerName)
    {
         var existing = _seats.FirstOrDefault(x => x.Value == playerName);
        if (!existing.Equals(default(KeyValuePair<Compass, string?>)))
        {
             _seats[existing.Key] = null;
        }
    }

    public void CheckStart()
    {
        if (Phase == RoomPhase.Waiting && _seats.Values.All(s => s != null))
        {
            StartNewDeal();
            // If Dealer is AI, trigger loop
            _ = ProcessGameLoop();
        }
    }

    public void StartNewDeal()
    {
        Phase = RoomPhase.Bidding;
        Deck.Shuffle();
        Hands = Deck.Deal();
        CurrentAuction = new Auction(Dealer);
        CurrentPlay = null;
        LastResult = null;
    }
    
    private async Task ProcessGameLoop()
    {
        bool actionTaken = true;
        try
        {
            while (actionTaken)
            {
                actionTaken = false;
                
                // Check if game over
                if (Phase == RoomPhase.Scoring || Phase == RoomPhase.Waiting) return;

                if (Phase == RoomPhase.Bidding && CurrentAuction != null)
                {
                    var turn = CurrentAuction.NextToAct;
                    if (_aiPlayers.TryGetValue(turn, out var ai))
                    {
                        // AI Turn - Faster (500ms)
                        await Task.Delay(500);
                        
                        var bid = await ai.GetBidAsync(CurrentAuction, Hands[turn]);
                        CurrentAuction.MakeCall(bid);
                        actionTaken = true;
                        
                        CheckAuctionComplete();
                        OnStateChanged?.Invoke();
                    }
                }
                else if (Phase == RoomPhase.Play && CurrentPlay != null)
                {
                    var turn = CurrentPlay.Leader; 
                    
                    var actualActor = turn;
                    if (CurrentPlay.Dummy == turn)
                    {
                        // Declarer controls dummy.
                        actualActor = CurrentPlay.Declarer;
                    }
                    
                    if (_aiPlayers.TryGetValue(actualActor, out var ai))
                    {
                        await Task.Delay(500);
                        Card card;
                        if (actualActor == turn)
                        {
                            card = await ai.GetCardAsync(CurrentPlay, Hands[turn], turn);
                        }
                        else
                        {
                            // AI (Declarer) playing for Dummy
                            card = await ai.GetCardAsync(CurrentPlay, Hands[turn], turn); 
                        }
                        
                        CurrentPlay.PlayCard(player: actualActor, card); 
                        
                        actionTaken = true;
                        CheckPlayComplete();
                        OnStateChanged?.Invoke();
                    }
                }
            }
        }
        catch (Exception ex)
        {
             // Log error? Console.WriteLine(ex);
             // Prevent loop crash logic from killing entire room state?
             // Should ideally notify clients "Bot Crashed" but for now just swallow to stabilize.
             System.Console.WriteLine($"[Error] GameLoop: {ex.Message}");
        }
    }
    
    private void CheckAuctionComplete()
    {
         if (CurrentAuction != null && CurrentAuction.IsComplete)
        {
            if (CurrentAuction.ContractBid != null)
            {
                Phase = RoomPhase.Play;
                CurrentPlay = new DealPlay(Hands, CurrentAuction.ContractBid.Value, CurrentAuction.Declarer!.Value);
            }
            else
            {
                RotateDealer();
                StartNewDeal();
                // Loop continues in StartNewDeal if Dealer is AI
            }
        }
    }
    
    private void CheckPlayComplete()
    {
        if (CurrentPlay != null && CurrentPlay.IsGameComplete)
        {
            Phase = RoomPhase.Scoring;
            var doubledState = CurrentAuction!.CurrentDoubledState; 
            int tricks = (CurrentPlay.Declarer == Compass.North || CurrentPlay.Declarer == Compass.South) 
                ? CurrentPlay.TricksWonNS 
                : CurrentPlay.TricksWonEW;
                
            LastResult = Scoring.Calculate(CurrentPlay.Contract, doubledState, tricks, Vulnerability, CurrentPlay.Declarer);
        }
    }

    public event Action? OnStateChanged;

    public async Task MakeBidAsync(Compass seat, Bid bid)
    {
        if (Phase != RoomPhase.Bidding) throw new InvalidOperationException("Not in bidding phase.");
        if (CurrentAuction == null) throw new InvalidOperationException("No auction active.");
        if (CurrentAuction.NextToAct != seat) throw new InvalidOperationException("Not your turn.");

        CurrentAuction.MakeCall(bid);
        CheckAuctionComplete();
        OnStateChanged?.Invoke();
        
        await ProcessGameLoop();
    }

    public async Task PlayCardAsync(Compass seat, Card card)
    {
        if (Phase != RoomPhase.Play) throw new InvalidOperationException("Not in play phase.");
        if (CurrentPlay == null) throw new InvalidOperationException("No play active.");
        
        Compass turn = CurrentPlay.Leader;
        Compass actualMover = seat;
        
        if (CurrentPlay.Dummy == turn)
        {
            if (seat != CurrentPlay.Declarer) throw new InvalidOperationException("Only Declarer can play for Dummy.");
            actualMover = CurrentPlay.Dummy;
        }
        else
        {
             if (seat != turn) throw new InvalidOperationException("Not your turn.");
        }
        
        CurrentPlay.PlayCard(actualMover, card);
        CheckPlayComplete();
        OnStateChanged?.Invoke();
        
        await ProcessGameLoop();
    }

    
    public void RotateDealer()
    {
        Dealer = (Compass)(((int)Dealer + 1) % 4);
    }
    
    // Helper to generate DTO for a specific player view
    public GameStateDto GetState(string playerName)
    {
        var seatKvp = _seats.FirstOrDefault(x => x.Value == playerName);
        Compass? mySeat = string.IsNullOrEmpty(playerName) ? null : 
                          (!seatKvp.Equals(default(KeyValuePair<Compass, string?>)) ? seatKvp.Key : null);

        var dto = new GameStateDto
        {
            RoomId = RoomId,
            Phase = Phase.ToString(),
            Dealer = Dealer.ToString(),
            Seats = _seats.ToDictionary(k => k.Key.ToString(), k => k.Value)
        };

        if (Hands.Count > 0 && mySeat.HasValue && Hands.ContainsKey(mySeat.Value))
        {
            dto.MyHand = Hands[mySeat.Value].Cards.Select(c => c.ToShortString()).ToList();
        }
        
        // Hand Counts
        foreach(var kvp in Hands)
        {
            dto.HandCounts[kvp.Key.ToString()] = kvp.Value.Size;
        }

        if (CurrentAuction != null)
        {
            dto.NextToAct = CurrentAuction.NextToAct.ToString();
            dto.CallHistory = CurrentAuction.History.Select(b => b.ToString()).ToList();
            if (CurrentAuction.ContractBid != null)
            {
                dto.Contract = CurrentAuction.ContractBid.Value.ToString();
                if (CurrentAuction.CurrentDoubledState == CallType.Double) dto.Contract += " X";
                if (CurrentAuction.CurrentDoubledState == CallType.Redouble) dto.Contract += " XX";
                
                dto.Declarer = CurrentAuction.Declarer.ToString();
            }
        }

        if (CurrentPlay != null)
        {
            dto.CurrentTrick = CurrentPlay.CurrentTrick.Cards.ToDictionary(k => k.Key.ToString(), k => k.Value.ToShortString());
            dto.TricksNS = CurrentPlay.TricksWonNS;
            dto.TricksEW = CurrentPlay.TricksWonEW;
            
            // Dummy Hand is visible
            if (Hands.ContainsKey(CurrentPlay.Dummy))
            {
                dto.DummyHand = Hands[CurrentPlay.Dummy].Cards.Select(c => c.ToShortString()).ToList();
            }
        }
        
        if (LastResult != null)
        {
            dto.LastScore = LastResult.ToString();
        }

        return dto;
    }
}
