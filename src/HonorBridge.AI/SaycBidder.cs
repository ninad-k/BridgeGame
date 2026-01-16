using System;
using System.Linq;
using HonorBridge.Engine;

namespace HonorBridge.AI;

public static class SaycBidder
{
    public static Bid GetBestBid(Auction auction, Hand hand)
    {
        // 1. Analyze Hand
        int hcp = hand.HighCardPoints;
        var distribution = hand.Cards.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.Count());
        int spadeCount = distribution.ContainsKey(Suit.Spades) ? distribution[Suit.Spades] : 0;
        int heartCount = distribution.ContainsKey(Suit.Hearts) ? distribution[Suit.Hearts] : 0;
        int diamondCount = distribution.ContainsKey(Suit.Diamonds) ? distribution[Suit.Diamonds] : 0;
        int clubCount = distribution.ContainsKey(Suit.Clubs) ? distribution[Suit.Clubs] : 0;
        
        bool isBalanced = IsBalanced(distribution);

        // 2. Identify Role
        // Are we opener? or Responder?
        // Check Auction History to find Opening Bid.
        var history = auction.History;
        int callCount = history.Count;
        
        // Find who opened.
        // History: [N, E, S, W, N...] depending on dealer.
        // It's easier to check if "ContractBid" is null.
        bool isOpening = auction.ContractBid == null;

        if (isOpening)
        {
            return EvaluatOpening(hcp, isBalanced, spadeCount, heartCount, diamondCount, clubCount);
        }
        else
        {
            // Responder Logic (Simplified)
            // Determine Partner's bid.
            // Auction manages "CurrentBid".
            // If Partner bids, then Ops bid, we are here.
            // We need to know PARTNER's last bid.
            // If ContractBid is set, that's the one to beat.
            return EvaluateResponse(auction, hand, hcp);
        }
    }

    private static bool IsBalanced(System.Collections.Generic.Dictionary<Suit, int> dist)
    {
        // Balanced: No singleton/void, at most one doubleton. (4333, 4432, 5332)
        int doubletons = dist.Values.Count(c => c == 2);
        int singletons = dist.Values.Count(c => c == 1);
        int voids = dist.Values.Count(c => c == 0); // Dictionary might omit 0s depending on how built.
        voids += (4 - dist.Count); // If missing form dictionary

        return voids == 0 && singletons == 0 && doubletons <= 1;
    }

    private static Bid EvaluatOpening(int hcp, bool balanced, int s, int h, int d, int c)
    {
        // 1. Pass if < 12 (Simplified, ignoring rule of 20)
        if (hcp < 12) return Bid.Pass;

        // 2. 1NT Opening: 15-17 Balanced
        if (balanced && hcp >= 15 && hcp <= 17) return new Bid(1, Strain.NoTrump);

        // 3. Majors (5-card strict for SAYC)
        // Bid longest major. If equal length 5-5, bid Spades first.
        if (s >= 5 && s >= h) return new Bid(1, Strain.Spades);
        if (h >= 5) return new Bid(1, Strain.Hearts);

        // 4. Minors (3+ acceptable, usually 4+ D unless 3-3 in minors then 1C)
        // Bid longer minor. If equal:
        // 3-3 -> 1C
        // 4-4 -> 1D
        // 5-5 -> 1D (Actually 1S is usually higher ranking suit? No, for minors: 1D is higher than 1C purely by rank? No bidding is level based. 
        // Standard: equal length minors: 3-3=1C, 4-4+=1D.
        if (d > c) return new Bid(1, Strain.Diamonds);
        if (c > d) return new Bid(1, Strain.Clubs);
        
        // Equal length
        if (d >= 4) return new Bid(1, Strain.Diamonds);
        return new Bid(1, Strain.Clubs);
    }

    private static Bid EvaluateResponse(Auction auction, Hand hand, int hcp)
    {
        // 1. Identify Partner's Bid
        // We are R-HO (Responder). Partner is L-HO.
        // Last Bid MUST be Partner's if we are responding to opener, OR Ops made a bid.
        // Checking ContractBid logic:
        // If ContractBid matches Auction.ContractBid, and we check who made it.
        
        var contractBid = auction.ContractBid;
        if (contractBid == null) return Bid.Pass; // Should not happen here if checked correctly
        
        var bidder = auction.GetCurrentContractHolder();
        var mySeat = auction.NextToAct;
        bool isPartnerBid = (bidder == PartnerOf(mySeat));
        
        if (!isPartnerBid)
        {
            // Opponent intervened. 
            // Simplified: Pass. (Competitive Bidding is Phase 9+).
            return Bid.Pass;
        }

        // We are responding to Partner's Open.
        var pBid = contractBid.Value;

        // -- RESPONSES TO 1NT --
        if (pBid.Level == 1 && pBid.Strain == Strain.NoTrump)
        {
            // 0-7 HCP: Pass
            if (hcp < 8) return Bid.Pass;
            
            // 8-9 HCP: Invite (2NT) - Simplified SAYC often uses Stayman/Transfer here.
            // MVP: Natural bidding.
            // If Balanced: Raise to 2NT (Invite) or 3NT (Game).
            // If unbalanced (6+ Major): Bid it? (Drop dead 2H/2S usually weak/transfer).
            // Let's stick to Natural-ish:
            
            if (hcp >= 10 && hcp <= 15) return new Bid(3, Strain.NoTrump); // Game
            if (hcp >= 8 && hcp <= 9) return new Bid(2, Strain.NoTrump); // Invite
            
            // Slam interest? (4NT) - Later.
            return Bid.Pass;
        }

        // -- RESPONSES TO SUIT OPENING (1H/1S/1C/1D) --
        if (pBid.Level == 1 && pBid.Strain != Strain.NoTrump)
        {
            // 1. Support Logic (Raise)
            // Need 3+ card support for Major (since Partner guaranteed 5).
            // Need 4-5+ card support for Minor (Partner guaranteed 3).
            
            var suit = pBid.Strain;
            int supportCount = hand.Cards.Count(c => c.Suit == (Suit)suit);
            
            // Major Raises
            if (suit == Strain.Hearts || suit == Strain.Spades)
            {
                if (supportCount >= 3)
                {
                    // 6-9 HCP: Single Raise (2H)
                    if (hcp >= 6 && hcp <= 9) return new Bid(2, suit);
                    
                    // 10-12 HCP: Limit Raise (3H) - Invite
                    if (hcp >= 10 && hcp <= 12) return new Bid(3, suit);
                    
                    // 13+ HCP: Game Force (Usually Jacoby 2N or new suit).
                    // Simplified: Bid Game (4H)
                    return new Bid(4, suit);
                }
            }
            
            // Minor Raises (Less common to raise immediately unless inverted)
            // Simplified: Raise if 5+ support and no major.
            if (suit == Strain.Clubs || suit == Strain.Diamonds)
            {
                 if (supportCount >= 5 && hcp >= 6 && hcp <= 9) return new Bid(2, suit);
                 // Otherwise prefer new suit or NT
            }
            
            // 2. New Suit (Forcing) - 1 Level
            // Requires 6+ HCP.
            // Bid longest suit (4+) at Level 1 if possible.
            // Spades over Hearts?
            // "New Suit at 1 Level" -> 6+ HCP.
            if (hcp >= 6)
            {
                // Can we bid 1 spade?
                if (suit != Strain.Spades)
                {
                    // Check spades
                    int spades = hand.Cards.Count(c => c.Suit == Suit.Spades);
                    if (spades >= 4)
                    {
                        // Valid to bid 1S over 1H/1D/1C
                         return new Bid(1, Strain.Spades);
                    }
                }
                
                // Can we bid 1 Heart? (Only over 1D/1C)
                if (suit == Strain.Clubs || suit == Strain.Diamonds)
                {
                    int hearts = hand.Cards.Count(c => c.Suit == Suit.Hearts);
                    if (hearts >= 4) return new Bid(1, Strain.Hearts);
                }
            }
            
            // 3. New Suit (Forcing) - 2 Level
            // Requires 10-11+ HCP (2/1 is Game Force, classic is 10+).
            // Let's say 11+ to bid new suit at level 2.
            if (hcp >= 11)
            {
                // Bid longest suit 5+.
                // Check all suits.
                var bestSuit = GetLongestSuit(hand);
                if (bestSuit.count >= 5 && bestSuit.suit != (Suit)suit)
                {
                    // Check sufficiency (must be > pBid).
                    // If we bid 2X.
                    // Simple check:
                    return new Bid(2, (Strain)bestSuit.suit);
                }
            }

            // 4. 1NT Response
            // 6-9 HCP, nothing better to do.
            if (hcp >= 6 && hcp <= 9) return new Bid(1, Strain.NoTrump);
            
            // Pass if < 6
        }
        
        return Bid.Pass;
    }
    
    // Helpers
    
    private static (Suit suit, int count) GetLongestSuit(Hand hand)
    {
        var g = hand.Cards.GroupBy(c => c.Suit)
                    .Select(x => (suit: x.Key, count: x.Count()))
                    .OrderByDescending(x => x.count)
                    .First();
        return g;
    }
    
    private static Compass PartnerOf(Compass c)
    {
        return (Compass)(((int)c + 2) % 4);
    }
}
