using HonorBridge.Engine;
using Xunit;

namespace HonorBridge.Engine.Tests;

public class HandTests
{
    [Fact]
    public void HighCardPoints_SumIsCorrect()
    {
        var hand = new Hand();
        hand.Add(new Card(Suit.Spades, Rank.Ace)); // 4
        hand.Add(new Card(Suit.Hearts, Rank.King)); // 3
        
        Assert.Equal(7, hand.HighCardPoints);
    }

    [Fact]
    public void Sort_OrdersBySuitDescendingThenRankDescending()
    {
        var hand = new Hand();
        hand.Add(new Card(Suit.Clubs, Rank.Two));
        hand.Add(new Card(Suit.Spades, Rank.Ace));
        hand.Add(new Card(Suit.Hearts, Rank.King));
        
        var sorted = hand.Cards;
        Assert.Equal(Suit.Spades, sorted[0].Suit);
        Assert.Equal(Suit.Hearts, sorted[1].Suit);
        Assert.Equal(Suit.Clubs, sorted[2].Suit);
    }

    [Fact]
    public void Add_MaintainsSort()
    {
        var hand = new Hand();
        hand.Add(new Card(Suit.Clubs, Rank.Two));
        hand.Add(new Card(Suit.Spades, Rank.Ace)); // Should go to front
        
        Assert.Equal(Suit.Spades, hand.Cards[0].Suit);
    }
}
