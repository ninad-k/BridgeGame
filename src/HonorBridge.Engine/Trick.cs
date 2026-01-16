using System;
using System.Collections.Generic;
using System.Linq;

namespace HonorBridge.Engine;

public class Trick
{
    public Compass Leader { get; }
    public Strain? TrumpStr { get; } // Nullable, though usually handled by Strain.NoTrump if NT
    // Actually, Strain enum has NoTrump, so we can just use Strain.
    // However, if we pass Strain directly, we need to know if it's NoTrump for comparison logic.
    private readonly Strain _trumpStrain;

    private readonly Dictionary<Compass, Card> _cards = new();
    public IReadOnlyDictionary<Compass, Card> Cards => _cards;

    public Suit LedSuit { get; private set; }
    public bool IsComplete => _cards.Count == 4;

    public Trick(Compass leader, Strain trumpStrain)
    {
        Leader = leader;
        _trumpStrain = trumpStrain;
    }

    public void Add(Compass player, Card card)
    {
        if (IsComplete)
            throw new InvalidOperationException("Trick is already full.");
        
        if (_cards.ContainsKey(player))
            throw new InvalidOperationException($"Player {player} has already played to this trick.");

        // First card determines the Led Suit
        if (_cards.Count == 0)
        {
            LedSuit = card.Suit;
        }

        _cards[player] = card;
    }

    public Compass DetermineWinner()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Trick is not complete.");

        Compass winner = Leader;
        Card winningCard = _cards[Leader];

        // Iterate through other 3 cards in order of play
        Compass current = NextCompass(Leader);
        for (int i = 0; i < 3; i++)
        {
            Card challenged = _cards[current];
            if (Beats(challenged, winningCard))
            {
                winningCard = challenged;
                winner = current;
            }
            current = NextCompass(current);
        }

        return winner;
    }

    private bool Beats(Card challenger, Card defender)
    {
        // 1. If challenger is Trump and defender is not, challenger wins
        bool challengerIsTrump = IsTrump(challenger.Suit);
        bool defenderIsTrump = IsTrump(defender.Suit);

        if (challengerIsTrump && !defenderIsTrump) return true;
        if (!challengerIsTrump && defenderIsTrump) return false;

        // 2. If both are Trump, higher rank wins
        if (challengerIsTrump && defenderIsTrump)
        {
            return challenger.Rank > defender.Rank;
        }

        // 3. If neither is Trump:
        //    - If challenger follows Led Suit and defender doesn't (and isn't trump), challenger wins? 
        //      No, defender must be either Led Suit or Trump. If defender is random off-suit discard, it loses to Led Suit.
        //      Actually, defender IS the current winning card.
        
        // Let's refine:
        // We are comparing `challenger` (just played) vs `defender` (current best).
        
        // Case A: Defender is Trump.
        // Challenger must be higher Trump to win. (Already covered above).
        
        // Case B: Defender is NOT Trump.
        // Defender must be Led Suit (since it beat the leader, or IS the leader).
        // If Challenger is Trump, Challenger wins. (Covered).
        // If Challenger is Led Suit, Higher Rank wins.
        // If Challenger is off-suit (not Trump, not Led Suit), Challenger loses.

        if (defender.Suit == LedSuit)
        {
            if (challenger.Suit == LedSuit)
                return challenger.Rank > defender.Rank;
            else
                return false; // Off-suit discard loses to Led Suit
        }
        
        // Should not reach here if logic holds (Defender is always either Leader [LedSuit] or a Trump that beat it)
        return false;
    }

    public Trick Clone()
    {
        var t = new Trick(Leader, _trumpStrain);
        foreach(var kvp in _cards)
        {
            t.Add(kvp.Key, kvp.Value); 
        }
        return t;
    }

    private bool IsTrump(Suit s)
    {
        if (_trumpStrain == Strain.NoTrump) return false;
        return (int)s == (int)_trumpStrain;
    }

    private Compass NextCompass(Compass c)
    {
        return (Compass)(((int)c + 1) % 4);
    }
}
