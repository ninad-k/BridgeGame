using HonorBridge.Engine;
using Xunit;

namespace HonorBridge.Engine.Tests;

public class CardTests
{
    [Fact]
    public void HighCardPoints_AreCorrect()
    {
        Assert.Equal(4, new Card(Suit.Spades, Rank.Ace).HighCardPoints);
        Assert.Equal(3, new Card(Suit.Hearts, Rank.King).HighCardPoints);
        Assert.Equal(2, new Card(Suit.Diamonds, Rank.Queen).HighCardPoints);
        Assert.Equal(1, new Card(Suit.Clubs, Rank.Jack).HighCardPoints);
        Assert.Equal(0, new Card(Suit.Spades, Rank.Ten).HighCardPoints);
        Assert.Equal(0, new Card(Suit.Clubs, Rank.Two).HighCardPoints);
    }

    [Fact]
    public void Equality_IsCorrect()
    {
        var c1 = new Card(Suit.Spades, Rank.Ace);
        var c2 = new Card(Suit.Spades, Rank.Ace);
        var c3 = new Card(Suit.Hearts, Rank.Ace);

        Assert.Equal(c1, c2);
        Assert.NotEqual(c1, c3);
        Assert.True(c1 == c2);
        Assert.True(c1 != c3);
    }

    [Fact]
    public void CompareTo_SortsBySuitThenRank()
    {
        var aceSpades = new Card(Suit.Spades, Rank.Ace);
        var kingSpades = new Card(Suit.Spades, Rank.King);
        var aceHearts = new Card(Suit.Hearts, Rank.Ace);

        // Spades > Hearts
        Assert.True(aceSpades.CompareTo(aceHearts) > 0);
        // Ace > King
        Assert.True(aceSpades.CompareTo(kingSpades) > 0);
    }

    [Fact]
    public void ToShortString_IsCorrect()
    {
        Assert.Equal("AS", new Card(Suit.Spades, Rank.Ace).ToShortString());
        Assert.Equal("TD", new Card(Suit.Diamonds, Rank.Ten).ToShortString());
        Assert.Equal("2C", new Card(Suit.Clubs, Rank.Two).ToShortString());
    }
}
