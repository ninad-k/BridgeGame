using System.Collections.Generic;
using HonorBridge.AI;
using HonorBridge.Engine;
using System.Linq;
using Xunit;

namespace HonorBridge.Server.Tests;

public class SaycBidderTests
{
    private Hand CreateHand(string spades, string hearts, string diamonds, string clubs)
    {
        var cards = new List<Card>();
        // Helper to parse strings like "AKJ98"
        AddSuit(cards, Suit.Spades, spades);
        AddSuit(cards, Suit.Hearts, hearts);
        AddSuit(cards, Suit.Diamonds, diamonds);
        AddSuit(cards, Suit.Clubs, clubs);
        return new Hand(cards);
    }

    private void AddSuit(List<Card> cards, Suit suit, string ranks)
    {
        foreach(char r in ranks)
        {
            Rank rank = ParseRank(r);
            cards.Add(new Card(suit, rank));
        }
    }

    private Rank ParseRank(char c)
    {
        return c switch {
            'A' => Rank.Ace, 'K' => Rank.King, 'Q' => Rank.Queen, 'J' => Rank.Jack,
            'T' => Rank.Ten, '9' => Rank.Nine, '8' => Rank.Eight, '7' => Rank.Seven,
            '6' => Rank.Six, '5' => Rank.Five, '4' => Rank.Four, '3' => Rank.Three, '2' => Rank.Two,
            _ => Rank.Two
        };
    }

    [Fact]
    public void Open_1NT_Balanced16()
    {
        // 16 HCP Balanced (4333)
        var hand = CreateHand("AJT9", "KQ54", "QJ2", "32"); // 5+5+3+0=13?? Wait. A=4,J=1,T=0=5. K=3,Q=2=5. Q=2,J=1=3. 13HCP.
        // Need 16.
        hand = CreateHand("AJT9", "KQ54", "KQ2", "32"); // A(4)+J(1)=5. K(3)+Q(2)=5. K(3)+Q(2)=5. 15HCP.
        
        var auction = new Auction(Compass.North);
        var bid = SaycBidder.GetBestBid(auction, hand);
        Assert.Equal("1NT", bid.ToString());
    }

    [Fact]
    public void Open_1Spade_5Card()
    {
        var hand = CreateHand("AKJ98", "K43", "32", "32"); // 7+3=10 HCP? Low.
        // Need 12+.
        hand = CreateHand("AKJ98", "KJ43", "A2", "32"); // 7+4+4=15. 5 Spades.
        
        var auction = new Auction(Compass.North);
        var bid = SaycBidder.GetBestBid(auction, hand);
        Assert.Equal("1S", bid.ToString());
    }
    
    [Fact]
    public void Respond_RaiseMajor()
    {
        // Partner bids 1H. We have 3 Hearts and 8 HCP. Should bid 2H.
        var hand = CreateHand("984", "K87", "QJ43", "Q32"); // hcp: K(3)+Q(2)+J(1)+Q(2)=8.
        
        var auction = new Auction(Compass.North);
        // Simulate Partner Opening 1H
        auction.MakeCall(new Bid(1, Strain.Hearts)); 
        // Opponent Pass
        auction.MakeCall(Bid.Pass);
        
        // Our Turn (South)
        var bid = SaycBidder.GetBestBid(auction, hand);
        Assert.Equal("2H", bid.ToString());
    }
    
    [Fact]
    public void Respond_NewSuit_1Level()
    {
        // Partner 1D. We have 4 Spades, 8 HCP. Should bid 1S.
        var hand = CreateHand("KJ87", "98", "Q432", "J32"); // K(3)+J(1)+Q(2)+J(1)=7. 
        // Need 6+. 7 is OK.
        
        var auction = new Auction(Compass.North);
        auction.MakeCall(new Bid(1, Strain.Diamonds));
        auction.MakeCall(Bid.Pass);
        
        var bid = SaycBidder.GetBestBid(auction, hand);
        Assert.Equal("1S", bid.ToString());
    }
}
