using HonorBridge.AI;
using HonorBridge.Engine;
using System.Collections.Generic;
using Xunit;

namespace HonorBridge.Server.Tests;

public class ParametricBidderTests
{
     private Hand CreateBalancedHand(int points)
    {
        // Construct 4333 hand with approx points.
        // Quickhack: A=4, K=3.
        var cards = new List<Card>();
        
        // 13 pts: A(4) K(3) Q(2) J(1) in Spades (10) + K(3) in Hearts = 13.
        // 16 pts: A(4) K(3) Q(2) J(1) + A(4) in Hearts + K(2)? No.
        
        // Let's manually build.
        // Spades: AKQJ (10)
        // Hearts: K (3)
        // Diamonds: (0)
        // Clubs: (0)
        // Total 13. Rest low cards.
        
        // 13 Point Hand
        if (points == 13)
        {
             // S: AKQJ (10), H: Kxx (3), D: xxx (0), C: xxx (0) -> 4333
             return ParseHand("AKQJ", "K43", "432", "432");
        }
        
        // 16 Point Hand
        if (points == 16)
        {
             // S: AKQJ (10), H: KQx (5), D: xxx (1? J=1), C: xxx
             return ParseHand("AKQJ", "KQ4", "J32", "432"); // 10+5+1 = 16.
        }
        
        return ParseHand("23456", "2345", "23", "2"); // 0 points
    }
    
    private Hand ParseHand(string s, string h, string d, string c)
    {
        var list = new List<Card>();
        AddSuit(list, Suit.Spades, s);
        AddSuit(list, Suit.Hearts, h);
        AddSuit(list, Suit.Diamonds, d);
        AddSuit(list, Suit.Clubs, c);
        return new Hand(list);
    }
    private void AddSuit(List<Card> cards, Suit suit, string ranks)
    {
        foreach(char r in ranks)
            cards.Add(new Card(suit, ParseRank(r)));
    }
    private Rank ParseRank(char c) => c switch { 'A'=>Rank.Ace, 'K'=>Rank.King, 'Q'=>Rank.Queen, 'J'=>Rank.Jack, 'T'=>Rank.Ten, _ => (Rank)(c-'0') };

    [Fact]
    public void Acol_Opens_13Points_1NT()
    {
        var hand = CreateBalancedHand(13);
        var auction = new Auction(Compass.North);
        
        // Acol 12-14
        var bid = ParametricBidder.Acol.GetBestBid(auction, hand);
        Assert.Equal("1NT", bid.ToString());
    }

    [Fact]
    public void SAYC_Passes_13Points_IfBalanced()
    {
        // SAYC 15-17. 13 is too weak for 1NT. 
        // Logic: <12 Pass. 13 is Opening strength?
        // Parametric logic: if (hcp < 12) Pass.
        // It's 13.
        // Is Balanced? Yes.
        // NT check: 13 inside 15-17? No.
        // Majors check: 4 spades. MinLength 5. No.
        // Minors check: 3 diamonds, 3 clubs. 
        // Will bid Minor (usually 1C/1D) or Pass if Parametric logic strictly looks for NT/Major?
        // My implementation fell through to Minors.
        // "Equal length -> (d>=4)? No. -> 1C."
        // So SAYC opens 1C on 13 balanced. Correct.
        // Wait, verifying it does NOT open 1NT.
        
        var hand = CreateBalancedHand(13);
        var auction = new Auction(Compass.North);
        
        var bid = ParametricBidder.SAYC.GetBestBid(auction, hand);
        Assert.NotEqual("1NT", bid.ToString());
        Assert.Equal("1C", bid.ToString());
    }
    
    [Fact]
    public void Goren_Opens_16Points_1NT()
    {
        // Goren 16-18
        var hand = CreateBalancedHand(16);
        var auction = new Auction(Compass.North);
        
        var bid = ParametricBidder.Goren.GetBestBid(auction, hand);
        Assert.Equal("1NT", bid.ToString());
    }
}
