using HonorBridge.Engine;
using Xunit;
using System;

namespace HonorBridge.Engine.Tests;

public class AuctionTests
{
    [Fact]
    public void Auction_PassedOut_UpdatesState()
    {
        var auction = new Auction(Compass.North);
        auction.MakeCall(Bid.Pass);
        auction.MakeCall(Bid.Pass);
        auction.MakeCall(Bid.Pass);
        auction.MakeCall(Bid.Pass);

        Assert.True(auction.IsComplete);
        Assert.Null(auction.ContractBid);
        Assert.Null(auction.Declarer);
    }

    [Fact]
    public void Auction_ValidContract_DeterminesDeclarer()
    {
        // Dealer North
        var auction = new Auction(Compass.North);
        
        auction.MakeCall(new Bid(1, Strain.Hearts)); // North bids 1H
        auction.MakeCall(Bid.Pass);                  // East passes
        auction.MakeCall(new Bid(2, Strain.Hearts)); // South bids 2H
        auction.MakeCall(Bid.Pass);                  // West
        auction.MakeCall(Bid.Pass);                  // North
        auction.MakeCall(Bid.Pass);                  // East - 3 Passes

        Assert.True(auction.IsComplete);
        Assert.Equal(new Bid(2, Strain.Hearts), auction.ContractBid);
        
        // North bid Hearts first, so North is Declarer
        Assert.Equal(Compass.North, auction.Declarer);
    }
    
    [Fact]
    public void Declarer_IsFirstToBidStrain()
    {
        // Dealer North
        var auction = new Auction(Compass.North);
        
        auction.MakeCall(Bid.Pass);                   // N
        auction.MakeCall(new Bid(1, Strain.Spades));  // E starts Spades
        auction.MakeCall(new Bid(2, Strain.Spades));  // S bids Spades (interference) -> No, Wait, South is Opponent.
        // If South makes a bid, it's valid if sufficient.
        // But if contract ends in Spades, who declared it?
        // E/W are partners. N/S are partners.
        
        // Let's do a clearer E/W partnership example where West ends up playing
        
        auction.MakeCall(new Bid(3, Strain.Spades));  // W raises Spades
        auction.MakeCall(Bid.Pass); // N
        auction.MakeCall(Bid.Pass); // E
        auction.MakeCall(Bid.Pass); // S
        
        // E bid Spades first. W raised. Contract is 3S by W (last bidder).
        // Declarer should be E (first to bid strain for the partnership).
        
        Assert.Equal(new Bid(3, Strain.Spades), auction.ContractBid);
        Assert.Equal(Compass.East, auction.Declarer); 
    }

    [Fact]
    public void ValidateCall_PreventsInsufficientBids()
    {
        var auction = new Auction(Compass.North);
        auction.MakeCall(new Bid(1, Strain.Hearts));
        
        Assert.Throws<InvalidOperationException>(() => 
            auction.MakeCall(new Bid(1, Strain.Diamonds))); // Lower strain same level
            
         Assert.Throws<InvalidOperationException>(() => 
            auction.MakeCall(new Bid(1, Strain.Hearts))); // Same bid
    }
    
    [Fact]
    public void ValidateCall_Doubles_Rules()
    {
        var auction = new Auction(Compass.North);
        auction.MakeCall(new Bid(1, Strain.NoTrump)); // N
        
        // East can double
        auction.MakeCall(Bid.Double);
        
        // South (Partner of N) can Redouble
        auction.MakeCall(Bid.Redouble);
        
        // West Pass
        auction.MakeCall(Bid.Pass);
        
        // North Pass
        auction.MakeCall(Bid.Pass);
        
        // East Pass -> Auction End
        auction.MakeCall(Bid.Pass);
        
        Assert.True(auction.IsComplete);
        Assert.Equal(CallType.Redouble, auction.CurrentDoubledState);
    }
}
