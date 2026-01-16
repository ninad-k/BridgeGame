using System;

namespace HonorBridge.Engine;

public readonly struct Bid : IEquatable<Bid>
{
    public CallType CallType { get; }
    public int Level { get; } // 1-7
    public Strain Strain { get; }

    public static Bid Pass => new Bid(CallType.Pass);
    public static Bid Double => new Bid(CallType.Double);
    public static Bid Redouble => new Bid(CallType.Redouble);

    public Bid(CallType callType, int level = 0, Strain strain = Strain.Clubs)
    {
        CallType = callType;
        Level = level;
        Strain = strain;
    }

    public Bid(int level, Strain strain) : this(CallType.Bid, level, strain)
    {
        if (level < 1 || level > 7)
            throw new ArgumentOutOfRangeException(nameof(level), "Bid level must be between 1 and 7.");
    }

    public bool IsSufficient(Bid currentHighBid)
    {
        if (CallType != CallType.Bid)
            return false; // Only strict bids can be sufficient over another bid

        if (currentHighBid.CallType != CallType.Bid)
            return true; // Any bid beats a non-bid (though strictly, you can't bid over a Double, you bid over the underlying bid) -> This logic usually handled by Auction validation

        if (Level > currentHighBid.Level)
            return true;

        if (Level < currentHighBid.Level)
            return false;

        return Strain > currentHighBid.Strain;
    }

    public override string ToString()
    {
        return CallType switch
        {
            CallType.Pass => "Pass",
            CallType.Double => "X",
            CallType.Redouble => "XX",
            CallType.Bid => $"{Level}{StrainToShortString()}",
            _ => "Unknown"
        };
    }

    private string StrainToShortString()
    {
        return Strain switch
        {
            Strain.Clubs => "C",
            Strain.Diamonds => "D",
            Strain.Hearts => "H",
            Strain.Spades => "S",
            Strain.NoTrump => "NT",
            _ => "?"
        };
    }

    public bool Equals(Bid other)
    {
        return CallType == other.CallType && Level == other.Level && Strain == other.Strain;
    }

    public override bool Equals(object? obj)
    {
        return obj is Bid other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CallType, Level, Strain);
    }

    public static bool operator ==(Bid left, Bid right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Bid left, Bid right)
    {
        return !left.Equals(right);
    }
}
