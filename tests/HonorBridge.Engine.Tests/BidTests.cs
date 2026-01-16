using HonorBridge.Engine;
using Xunit;

namespace HonorBridge.Engine.Tests;

public class BidTests
{
    [Fact]
    public void IsSufficient_WorksCorrectly()
    {
        var oneClub = new Bid(1, Strain.Clubs);
        var oneDiamond = new Bid(1, Strain.Diamonds);
        var oneHeart = new Bid(1, Strain.Hearts);
        var oneSpade = new Bid(1, Strain.Spades);
        var oneNT = new Bid(1, Strain.NoTrump);
        var twoClubs = new Bid(2, Strain.Clubs);

        Assert.True(oneDiamond.IsSufficient(oneClub));
        Assert.False(oneClub.IsSufficient(oneDiamond));
        
        Assert.True(oneNT.IsSufficient(oneSpade));
        Assert.False(oneSpade.IsSufficient(oneNT));
        
        Assert.True(twoClubs.IsSufficient(oneNT));
        Assert.False(oneNT.IsSufficient(twoClubs));
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        Assert.Equal("1H", new Bid(1, Strain.Hearts).ToString());
        Assert.Equal("7NT", new Bid(7, Strain.NoTrump).ToString());
        Assert.Equal("Pass", Bid.Pass.ToString());
        Assert.Equal("X", Bid.Double.ToString());
        Assert.Equal("XX", Bid.Redouble.ToString());
    }
}
