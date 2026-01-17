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
    
    public enum RoomPhase { Waiting, Bidding, Play, Scoring, Lobby }
    private RoomPhase _phase = RoomPhase.Waiting;
    private bool _showingTrickResult = false;
    public RoomPhase Phase { get { return _phase; } private set { _phase = value; } } // Assuming Phase property should now use _phase
    
    public ScoreResult? LastResult { get; private set; }
    
    // Setting
    public IBiddingSystem CurrentBiddingSystem { get; set; } = ParametricBidder.SAYC;

    private readonly Dictionary<Compass, MonteCarloAI> _aiPlayers = new();
    private AILevel _currentLevel = AILevel.Pro;

    public GameRoom(string roomId)
    {
        RoomId = roomId;
        Deck = new Deck();
        // Randomize Dealer
        Dealer = (Compass)new Random().Next(4);
        InitializeAI();
    }
    
    public void SetAILevel(AILevel level)
    {
        _currentLevel = level;
        InitializeAI();
    }
    
    private void InitializeAI()
    {
        int sims = _currentLevel switch
        {
            AILevel.Beginner => 50,
            AILevel.Intermediate => 100,
            AILevel.Advanced => 250,
            AILevel.Pro => 500,
            _ => 500
        };
        
        _aiPlayers.Clear();
        _aiPlayers[Compass.East] = new MonteCarloAI(null, sims);
        _aiPlayers[Compass.West] = new MonteCarloAI(null, sims);
        _aiPlayers[Compass.North] = new MonteCarloAI(null, sims); // Bot Partner
    }

    public void RestartGame()
    {
        RotateDealer();
        StartNewDeal();
        OnStateChanged?.Invoke();
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
            // Preserve current simulation count (skill level)
            int currentSims = 200; // Default
            if (_aiPlayers.TryGetValue(seat, out var oldAi))
            {
                // We can't easily read _simulationCount it's private.
                // But we know _currentLevel.
                currentSims = _currentLevel switch
                {
                    AILevel.Beginner => 50,
                    AILevel.Intermediate => 100,
                    AILevel.Advanced => 250,
                    AILevel.Pro => 500,
                    _ => 500
                };
            }
            _aiPlayers[seat] = new MonteCarloAI(sys, currentSims);
        }
    }
    
    public void AddAI(Compass seat)
    {
        if (_seats[seat] != null) return; // Occupied by human
        
        _seats[seat] = $"Bot-{seat}";
        int sims = _currentLevel switch
        {
            AILevel.Beginner => 50,
            AILevel.Intermediate => 100,
            AILevel.Advanced => 250,
            AILevel.Pro => 500,
            _ => 500
        };
        _aiPlayers[seat] = new MonteCarloAI(CurrentBiddingSystem, sims);
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
        if ((Phase == RoomPhase.Waiting || Phase == RoomPhase.Lobby) && _seats.Values.All(s => s != null))
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
    
    private bool IsHuman(Compass c)
    {
        return _seats[c] != null && !_seats[c]!.StartsWith("Bot-");
    }

    private Compass GetEffectiveDeclarer()
    {
        if (CurrentPlay == null) return Compass.North; // Fallback
        
        Compass decl = CurrentPlay.Declarer;
        if (IsHuman(decl)) return decl;
        
        // If Declarer is Bot, check Partner
        Compass partner = (Compass)(((int)decl + 2) % 4);
        if (IsHuman(partner)) return partner;
        
        return decl;
    }

    private bool _isGameLoopRunning = false;

    private async Task ProcessGameLoop()
    {
        if (_isGameLoopRunning) return;
        _isGameLoopRunning = true;

        try 
        {
            bool actionTaken = true;
            while(actionTaken)
            {
                actionTaken = false;
                
                if (Phase == RoomPhase.Bidding && CurrentAuction != null && !CurrentAuction.IsComplete)
                {
                    // ... Bidding logic ...
                    Compass turn = CurrentAuction.NextToAct;
                    if (_aiPlayers.TryGetValue(turn, out var ai))
                    {
                         await Task.Delay(500); // Thinking delay
                         var bid = await ai.GetBidAsync(CurrentAuction, Hands[turn]);
                         await MakeBidAsync(turn, bid, fromGameLoop: true);
                         actionTaken = true;
                    }
                }
                else if (Phase == RoomPhase.Play && CurrentPlay != null && !CurrentPlay.IsGameComplete)
                {
                    Compass turn = CurrentPlay.NextToAct;
                    
                    // CHECK: Is this turn controlled by a Human?
                    Compass effectiveDeclarer = GetEffectiveDeclarer();
                    
                    bool isHumanTurn = false;
                    
                    if (turn == CurrentPlay.Dummy)
                    {
                        // Dummy's turn is played by Effective Declarer
                        isHumanTurn = IsHuman(effectiveDeclarer);
                    }
                    else if (turn == effectiveDeclarer)
                    {
                        // Effective Declarer playing their own hand (or the hand they took over)
                        isHumanTurn = IsHuman(effectiveDeclarer);
                    }
                    else
                    {
                        // Defender or normal play
                        isHumanTurn = IsHuman(turn);
                    }

                    if (isHumanTurn)
                    {
                        // Wait for human input.
                        continue; 
                    }

                    // Otherwise, AI plays
                    // ...
                    Compass actualActor = turn; // The AI that strictly owns the hand
                    
                    // If turn is Dummy, Declarer AI plays. 
                    // But if EffectiveDeclarer is Bot, it plays.
                    // If we are here, isHumanTurn is false.
                    // So EffectiveDeclarer is Bot.
                    
                    if (turn == CurrentPlay.Dummy)
                    {
                         actualActor = CurrentPlay.Declarer; // AI Declarer plays for Dummy
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
                        
                        // Execute Play
                        await PlayCardAsync(turn, card, fromGameLoop: true);
                        
                        actionTaken = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Game Loop: {ex.Message}");
        }
        finally
        {
            _isGameLoopRunning = false;
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

    public async Task MakeBidAsync(Compass seat, Bid bid, bool fromGameLoop = false)
    {
        if (Phase != RoomPhase.Bidding) throw new InvalidOperationException("Not in bidding phase.");
        if (CurrentAuction == null) throw new InvalidOperationException("No auction active.");
        if (CurrentAuction.NextToAct != seat) throw new InvalidOperationException("Not your turn.");

        CurrentAuction.MakeCall(bid);
        CheckAuctionComplete();
        OnStateChanged?.Invoke();
        
        if (!fromGameLoop)
        {
             _ = ProcessGameLoop();
        }
    }

    public async Task PlayCardAsync(Compass seat, Card card, bool fromGameLoop = false)
    {
        if (Phase != RoomPhase.Play) throw new InvalidOperationException("Not in play phase.");
        if (CurrentPlay == null) throw new InvalidOperationException("No play active.");
        
        Compass turn = CurrentPlay.NextToAct;
        
        // Determine who is allowed to move
        Compass effectiveDeclarer = GetEffectiveDeclarer();
        
        if (CurrentPlay.Dummy == turn)
        {
            // Dummy's turn. 
            // Only EffectiveDeclarer can play.
            if (seat != effectiveDeclarer) throw new InvalidOperationException("Only Declarer (or Effective Declarer) can play for Dummy.");
        }
        else if (turn == effectiveDeclarer || (turn == CurrentPlay.Declarer && seat == effectiveDeclarer))
        {
             // Declarer's turn (or effectively Declarer's turn if they took over).
             if (seat != effectiveDeclarer) throw new InvalidOperationException("Not your turn.");
        }
        else
        {
             // Standard Defender turn
             if (seat != turn) throw new InvalidOperationException("Not your turn.");
        }
        
        // Execute
        Console.WriteLine($"[GameRoom] PlayCardAsync: {seat} plays {card}");
        try
        {
            int beforeTricks = CurrentPlay.CompletedTricks.Count;
            CurrentPlay.PlayCard(player: turn, card);
            int afterTricks = CurrentPlay.CompletedTricks.Count;
    
            if (afterTricks > beforeTricks)
            {
                // Trick complete. Show result for 3.5s
                _showingTrickResult = true;
                OnStateChanged?.Invoke();
                // If we are in the loop, we can await delay here if we want strictly sync behavior
                // but ProcessGameLoop handles logic too.
                // However, blocking here for 3.5s implies the caller waits.
                await Task.Delay(3500); 
                _showingTrickResult = false;
            }
    
            CheckPlayComplete();
            OnStateChanged?.Invoke();
        }
            catch(Exception ex)
            {
                Console.WriteLine($"[GameRoom] ERROR Playing Card: {ex.Message}");
                throw; // Re-throw to caller
            }
        
        if (!fromGameLoop)
        {
            _ = ProcessGameLoop();
        }
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
            Compass handSource = mySeat.Value;
            if (CurrentPlay != null)
            {
                var eff = GetEffectiveDeclarer();
                if (eff == mySeat.Value && CurrentPlay.Declarer != mySeat.Value)
                {
                    // If I am Effective Declarer (taking over for Bot Declarer), 
                    // I need to see the Bot's hand in "My Hand" area to play it.
                    handSource = CurrentPlay.Declarer;
                }
            }
            
            dto.MyHand = Hands[handSource].Cards.Select(c => c.ToShortString()).ToList();
        }
        
        if (mySeat.HasValue)
        {
            dto.MySeat = mySeat.Value.ToString();
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
            dto.NextToAct = CurrentPlay.NextToAct.ToString();
            
            if (_showingTrickResult && CurrentPlay.CompletedTricks.Count > 0)
            {
                // Show the JUST Completed trick
                var lastTrick = CurrentPlay.CompletedTricks.Last();
                dto.CurrentTrick = lastTrick.Cards.ToDictionary(k => k.Key.ToString(), v => v.Value.ToShortString());
                dto.LastTrickWinner = lastTrick.DetermineWinner().ToString();
            }
            else if (CurrentPlay.CurrentTrick != null)
            {
                // Show active trick
                dto.CurrentTrick = CurrentPlay.CurrentTrick.Cards.ToDictionary(k => k.Key.ToString(), v => v.Value.ToShortString());
                dto.LastTrickWinner = null;
            }
            
            dto.Declarer = CurrentPlay.Declarer.ToString();
            dto.TricksNS = CurrentPlay.TricksWonNS;
            dto.TricksEW = CurrentPlay.TricksWonEW;
            
            // Dummy Hand is visible ONLY after opening lead (Turn 1 card 1)
            // Opening lead is when CurrentTrick has 1 card, OR CompletedTricks > 0.
            bool openingLeadMade = CurrentPlay.CompletedTricks.Count > 0 || CurrentPlay.CurrentTrick.Cards.Count > 0;
            
            if (openingLeadMade && Hands.ContainsKey(CurrentPlay.Dummy))
            {
                dto.DummyHand = Hands[CurrentPlay.Dummy].Cards.Select(c => c.ToShortString()).ToList();
            }
        }
        
        if (LastResult != null)
        {
            dto.LastScore = LastResult.ToString();
            dto.LastPoints = LastResult.Points;
        }

        return dto;
    }
}
