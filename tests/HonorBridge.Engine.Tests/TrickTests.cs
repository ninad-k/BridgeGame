using HonorBridge.Engine;
using Xunit;
using System;

namespace HonorBridge.Engine.Tests;

public class TrickTests
{
    [Fact]
    public void DetermineWinner_NoTrump_HighCardInLedSuitWins()
    {
        // Trick: North Leads 2H. East KH. South AH. West 5H.
        // Winner: South (AH)
        
        var trick = new Trick(Compass.North, Strain.NoTrump);
        trick.Add(Compass.North, new Card(Suit.Hearts, Rank.Two));
        trick.Add(Compass.East, new Card(Suit.Hearts, Rank.King));
        trick.Add(Compass.South, new Card(Suit.Hearts, Rank.Ace));
        trick.Add(Compass.West, new Card(Suit.Hearts, Rank.Five));
        
        Assert.True(trick.IsComplete);
        Assert.Equal(Compass.South, trick.DetermineWinner());
    }

    [Fact]
    public void DetermineWinner_SuitContract_TrumpWins()
    {
        // Spades is Trump.
        // North Leads AH. East 2S (Trump). South KH. West 5H.
        // Winner: East (2S beats AH)
        
        var trick = new Trick(Compass.North, Strain.Spades);
        trick.Add(Compass.North, new Card(Suit.Hearts, Rank.Ace));
        trick.Add(Compass.East, new Card(Suit.Spades, Rank.Two));
        trick.Add(Compass.South, new Card(Suit.Hearts, Rank.King));
        trick.Add(Compass.West, new Card(Suit.Hearts, Rank.Five));
        
        Assert.Equal(Compass.East, trick.DetermineWinner());
    }
    
    [Fact]
    public void DetermineWinner_Discard_Loses()
    {
        // Spades is Trump.
        // North Leads AH. East 2D (Discard). South KH. West 5H.
        // Winner: North (AH beats KH, Discard is useless)
        
        var trick = new Trick(Compass.North, Strain.Spades);
        trick.Add(Compass.North, new Card(Suit.Hearts, Rank.Ace));
        trick.Add(Compass.East, new Card(Suit.Diamonds, Rank.Two)); // Discard
        trick.Add(Compass.South, new Card(Suit.Hearts, Rank.King));
        trick.Add(Compass.West, new Card(Suit.Hearts, Rank.Five));
        
        Assert.Equal(Compass.North, trick.DetermineWinner());
    }
}
