using System;
using System.Collections.Generic;

namespace HonorBridge.Engine;

public class DealPlay
{
    public Dictionary<Compass, Hand> Hands { get; }
    public Bid Contract { get; }
    public Compass Declarer { get; }
    public Compass Dummy { get; }
    public Compass Leader => CurrentTrick?.Leader ?? _nextLeader;

    public Trick CurrentTrick { get; private set; }
    public List<Trick> CompletedTricks { get; } = new();

    public int TricksWonNS { get; private set; }
    public int TricksWonEW { get; private set; }

    private Compass _nextLeader;
    
    public DealPlay(Dictionary<Compass, Hand> hands, Bid contract, Compass declarer)
    {
        Hands = hands;
        Contract = contract;
        Declarer = declarer;
        Dummy = PartnerOf(declarer);
        
        // Opening lead is by player to left of Declarer
        _nextLeader = NextCompass(Declarer);
        
        StartNewTrick();
    }

    public void PlayCard(Compass player, Card card)
    {
        if (CurrentTrick.IsComplete)
            throw new InvalidOperationException("Trick is complete. Waiting for next trick start.");

        // Validation
        // 1. Turn order check
        Compass expectedPlayer = DetermineNextPlayer();
        if (player != expectedPlayer)
             throw new InvalidOperationException($"It is {expectedPlayer}'s turn, not {player}'s.");

        // 2. Card possession check
        if (!Hands[player].Cards.Contains(card))
             throw new InvalidOperationException($"{player} does not hold {card}.");

        // 3. Follow suit check
        if (CurrentTrick.Cards.Count > 0)
        {
            Suit ledSuit = CurrentTrick.LedSuit;
            if (card.Suit != ledSuit)
            {
                // Player is playing off-suit. Must check if they HAVE the led suit.
                bool hasLedSuit = HasSuit(Hands[player], ledSuit);
                if (hasLedSuit)
                    throw new InvalidOperationException($"Must follow suit ({ledSuit}).");
            }
        }

        // Execute Play
        Hands[player].Remove(card);
        CurrentTrick.Add(player, card);

        if (CurrentTrick.IsComplete)
        {
            ResolveTrick();
        }
    }

    public bool IsGameComplete => CompletedTricks.Count == 13;

    private void StartNewTrick()
    {
        CurrentTrick = new Trick(_nextLeader, Contract.Strain);
    }

    private void ResolveTrick()
    {
        Compass winner = CurrentTrick.DetermineWinner();
        CompletedTricks.Add(CurrentTrick);

        if (winner == Compass.North || winner == Compass.South)
            TricksWonNS++;
        else
            TricksWonEW++;

        _nextLeader = winner;
        
        if (!IsGameComplete)
        {
            StartNewTrick();
        }
    }

    private Compass DetermineNextPlayer()
    {
        if (CurrentTrick.Cards.Count == 0)
            return _nextLeader;
        
        // Find who played last
        // Iterate leader -> +1 -> +2
        // If count is K, the next is Leader + K
        int playedCount = CurrentTrick.Cards.Count;
        Compass p = CurrentTrick.Leader;
        for(int i=0; i<playedCount; i++) p = NextCompass(p);
        
        return p;
    }

    private bool HasSuit(Hand hand, Suit suit)
    {
        foreach(var c in hand.Cards)
        {
            if (c.Suit == suit) return true;
        }
        return false;
    }

    private Compass NextCompass(Compass c)
    {
        return (Compass)(((int)c + 1) % 4);
    }

    private Compass PartnerOf(Compass c)
    {
        return (Compass)(((int)c + 2) % 4);
    }
    public DealPlay Clone()
    {
        var newHands = Hands.ToDictionary(k => k.Key, k => k.Value.Clone());
        
        // Use a private constructor or a way to bypass initialization logic for cloning?
        // Standard ctor calls StartNewTrick() => CurrentTrick
        // We want to force state.
        // Let's create new and overwrite.
        
        var clone = new DealPlay(newHands, Contract, Declarer);
        
        // Restore State
        clone.TricksWonNS = TricksWonNS;
        clone.TricksWonEW = TricksWonEW;
        clone.CompletedTricks.Clear();
        foreach(var t in CompletedTricks) clone.CompletedTricks.Add(t.Clone());
        
        // Restore Current Trick
        // StartNewTrick was called in ctor, but our current trick state might be mid-trick.
        clone.CurrentTrick = CurrentTrick.Clone();
        
        // Restore NextLeader logic?
        // _nextLeader is private. But `Leader` property derives from CurrentTrick or _nextLeader.
        // If mid-trick, CurrentTrick.Leader rules.
        // If trick just resolved (waiting for next), _nextLeader matters.
        // We lack access to set _nextLeader.
        // However, if we clone `CompletedTricks`, we can re-derive?
        // Actually, logic is: `Leader => CurrentTrick?.Leader ?? _nextLeader;`
        // If `CurrentTrick` is copied, we are good for mid-trick.
        // If `CurrentTrick` is empty (new trick start), it needs correct Leader.
        // `Trick` constructor takes Leader.
        // If we clone `CurrentTrick` correctly, it carries the Leader.
        
        return clone;
    }
}
