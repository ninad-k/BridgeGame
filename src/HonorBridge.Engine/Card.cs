using System;

namespace HonorBridge.Engine;

public readonly struct Card : IEquatable<Card>, IComparable<Card>
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public int HighCardPoints => Rank switch
    {
        Rank.Ace => 4,
        Rank.King => 3,
        Rank.Queen => 2,
        Rank.Jack => 1,
        _ => 0
    };

    public bool Equals(Card other)
    {
        return Suit == other.Suit && Rank == other.Rank;
    }

    public override bool Equals(object? obj)
    {
        return obj is Card other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Suit, Rank);
    }

    public int CompareTo(Card other)
    {
        // Standard Bridge sorting: Spades > Hearts > Diamonds > Clubs, then Rank
        int suitCompare = Suit.CompareTo(other.Suit);
        if (suitCompare != 0)
            return suitCompare;
        
        return Rank.CompareTo(other.Rank);
    }

    public static bool operator ==(Card left, Card right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Card left, Card right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }
    
    public string ToShortString()
    {
        char s = Suit switch {
            Suit.Clubs => 'C',
            Suit.Diamonds => 'D',
            Suit.Hearts => 'H',
            Suit.Spades => 'S',
            _ => '?'
        };
        
        string r = Rank switch {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            Rank.Ten => "T",
            _ => ((int)Rank).ToString()
        };
        
        return $"{r}{s}";
    }
}
