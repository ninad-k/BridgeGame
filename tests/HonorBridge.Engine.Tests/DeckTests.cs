using HonorBridge.Engine;
using Xunit;
using System.Linq;

namespace HonorBridge.Engine.Tests;

public class DeckTests
{
    [Fact]
    public void Deal_Distributes52Cards()
    {
        var deck = new Deck();
        deck.Shuffle();
        var hands = deck.Deal();

        Assert.Equal(4, hands.Count);
        Assert.Equal(13, hands[Compass.North].Size);
        Assert.Equal(13, hands[Compass.East].Size);
        Assert.Equal(13, hands[Compass.South].Size);
        Assert.Equal(13, hands[Compass.West].Size);
    }

    [Fact]
    public void Deal_CardsAreUnique()
    {
        var deck = new Deck();
        var hands = deck.Deal();

        var allCards = hands.Values.SelectMany(h => h.Cards).ToList();
        Assert.Equal(52, allCards.Distinct().Count());
    }
}
