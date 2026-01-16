using HonorBridge.Engine;
using Xunit;
using System;
using System.Collections.Generic;

namespace HonorBridge.Engine.Tests;

public class DealPlayTests
{
    private Dictionary<Compass, Hand> CreateMockHands()
    {
        // Simple distribution for testing
        var hands = new Dictionary<Compass, Hand>();
        
        // North has AH, KH
        hands[Compass.North] = new Hand();
        hands[Compass.North].Add(new Card(Suit.Hearts, Rank.Ace));
        hands[Compass.North].Add(new Card(Suit.Hearts, Rank.King));
        
        // East has 2H, 3H
        hands[Compass.East] = new Hand();
        hands[Compass.East].Add(new Card(Suit.Hearts, Rank.Two));
        hands[Compass.East].Add(new Card(Suit.Hearts, Rank.Three));
        
        // South has 4H, 5H
        hands[Compass.South] = new Hand();
        hands[Compass.South].Add(new Card(Suit.Hearts, Rank.Four));
        hands[Compass.South].Add(new Card(Suit.Hearts, Rank.Five));
        
        // West has 6H, 2S (Spade!)
        hands[Compass.West] = new Hand();
        hands[Compass.West].Add(new Card(Suit.Hearts, Rank.Six));
        hands[Compass.West].Add(new Card(Suit.Spades, Rank.Two));
        
        return hands;
    }

    [Fact]
    public void PlayCard_EnforcesTurnOrder()
    {
        var hands = CreateMockHands();
        // Declarer South -> Leader West
        var play = new DealPlay(hands, new Bid(1, Strain.NoTrump), Compass.South);
        
        Assert.Equal(Compass.West, play.Leader);
        
        Assert.Throws<InvalidOperationException>(() => 
            play.PlayCard(Compass.North, hands[Compass.North].Cards[0])); // Wrong turn
            
        // Correct turn
        play.PlayCard(Compass.West, hands[Compass.West].Cards[0]); // Plays 6H
    }

    [Fact]
    public void PlayCard_EnforcesFollowSuit()
    {
        var hands = CreateMockHands();
        // Declarer South -> Leader West
        var play = new DealPlay(hands, new Bid(1, Strain.NoTrump), Compass.South);
        
        // West leads Spades (2S)
        var s2 = new Card(Suit.Spades, Rank.Two);
        play.PlayCard(Compass.West, s2);
        
        // North must follow Spades? Check hands.
        // North has AH, KH. No Spades. Can discard.
        var nh = hands[Compass.North].Cards[0]; // AH
        play.PlayCard(Compass.North, nh); // Legal (void in Spades)
        
        // Play completes for E/S...
        play.PlayCard(Compass.East, hands[Compass.East].Cards[0]);
        play.PlayCard(Compass.South, hands[Compass.South].Cards[0]);
        
        // Trick 2. S2 (Spade) won (others were Hearts discards). West won.
        // West leads again. West has 6H.
        var h6 = new Card(Suit.Hearts, Rank.Six);
        play.PlayCard(Compass.West, h6);
        
        // North Must follow Hearts.
        // If North tries to play a Club (if he had one), it would fail.
        // Since play logic checks "HasSuit", let's make a fail case.
        // Let's repurpose the setup.
        
        var newHands = new Dictionary<Compass, Hand>();
        // North has 2H, 2C
        newHands[Compass.North] = new Hand(new[] { new Card(Suit.Hearts, Rank.Two), new Card(Suit.Clubs, Rank.Two) });
        
        // West (Leader) plays H
        newHands[Compass.West] = new Hand(new[] { new Card(Suit.Hearts, Rank.Ace) });
        newHands[Compass.East] = new Hand(new[] { new Card(Suit.Hearts, Rank.Three) });
        newHands[Compass.South] = new Hand(new[] { new Card(Suit.Hearts, Rank.Four) });

        var play2 = new DealPlay(newHands, new Bid(1, Strain.NoTrump), Compass.South);
        
        play2.PlayCard(Compass.West, new Card(Suit.Hearts, Rank.Ace));
        
        // North holds H and C. Must play H. Tries to play C.
        Assert.Throws<InvalidOperationException>(() => 
            play2.PlayCard(Compass.North, new Card(Suit.Clubs, Rank.Two)));
            
        // Correct play
        play2.PlayCard(Compass.North, new Card(Suit.Hearts, Rank.Two));
    }
}
