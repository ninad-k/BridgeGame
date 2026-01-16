namespace HonorBridge.Engine;

public enum Suit
{
    Clubs = 0,
    Diamonds = 1,
    Hearts = 2,
    Spades = 3
}

public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

public enum Compass
{
    North,
    East,
    South,
    West
}

public enum Vulnerability
{
    None,
    NS,
    EW,
    Both
}

public enum Strain
{
    Clubs = 0,
    Diamonds = 1,
    Hearts = 2,
    Spades = 3,
    NoTrump = 4
}

public enum CallType
{
    Pass,
    Bid,
    Double,
    Redouble
}
