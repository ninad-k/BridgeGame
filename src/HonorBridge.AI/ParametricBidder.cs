using System.Linq;
using HonorBridge.Engine;

namespace HonorBridge.AI;

public class ParametricBidder : IBiddingSystem
{
    public string Name { get; }
    
    // Configuration
    public int NtMin { get; }
    public int NtMax { get; }
    public int MajorMinLength { get; }
    public bool IsStrongClub { get; }

    public ParametricBidder(string name, int ntMin, int ntMax, int majorMinLength, bool isStrongClub = false)
    {
        Name = name;
        NtMin = ntMin;
        NtMax = ntMax;
        MajorMinLength = majorMinLength;
        IsStrongClub = isStrongClub;
    }

    // Factory methods for specific systems
    public static ParametricBidder SAYC => new ParametricBidder("SAYC", 15, 17, 5);
    public static ParametricBidder Acol => new ParametricBidder("Acol", 12, 14, 4); // Weak NT, 4-card majors
    public static ParametricBidder Goren => new ParametricBidder("Goren", 16, 18, 4); // Stronger NT, 4-card majors
    // Strong Club: 16+ HCP = 1C. 13-15 NT. 5-card majors.
    public static ParametricBidder StrongClub => new ParametricBidder("Strong Club", 13, 15, 5, true);

    public Bid GetBestBid(Auction auction, Hand hand)
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
        bool isOpening = auction.ContractBid == null;

        if (isOpening)
        {
            return EvaluatOpening(hcp, isBalanced, spadeCount, heartCount, diamondCount, clubCount);
        }
        else
        {
            return EvaluateResponse(auction, hand, hcp, isBalanced, spadeCount, heartCount, diamondCount, clubCount);
        }
    }
    
    private Bid EvaluatOpening(int hcp, bool balanced, int s, int h, int d, int c)
    {
        if (hcp < 12) return Bid.Pass; // Basic opening threshold usually 12
        
        // Strong Club Logic
        if (IsStrongClub)
        {
            if (hcp >= 16) return new Bid(1, Strain.Clubs);
            // If < 16, standard logic applies BUT max range is 15.
            // also 1C is not available as natural (use 1D or 2C?)
            // Simplified: If Clubs is best but blocked, bid 1D (Precision style "imperfect diamond")
        }

        // 1NT Opening
        if (balanced && hcp >= NtMin && hcp <= NtMax) return new Bid(1, Strain.NoTrump);

        // Majors
        // Check Min Length
        if (s >= MajorMinLength && s >= h) return new Bid(1, Strain.Spades);
        if (h >= MajorMinLength) return new Bid(1, Strain.Hearts);

        // Minors (Default logic 3+)
        // If Strong Club, we can't bid 1C naturally usually.
        // Precision uses 1D for "Nebulous Diamond" (2+ diamonds).
        // Let's implement that simple swap.
        
        if (IsStrongClub)
        {
             // Must bid 1D if no Major and not NT range. 
             // (Precision 2C is usually 6 clubs or 5 and 11-15).
             // Let's just default to 1D for any minor opening < 16 HCP in this MVP.
             return new Bid(1, Strain.Diamonds);
        }

        if (d > c) return new Bid(1, Strain.Diamonds);
        if (c > d) return new Bid(1, Strain.Clubs);
        
        // Equal length
        if (d >= 4) return new Bid(1, Strain.Diamonds);
        return new Bid(1, Strain.Clubs);
    }

    private bool IsBalanced(System.Collections.Generic.Dictionary<Suit, int> dist)
    {
        int doubletons = dist.Values.Count(c => c == 2);
        int singletons = dist.Values.Count(c => c == 1);
        int voids = dist.Values.Count(c => c == 0);
        voids += (4 - dist.Count); // Suites not in dict are voids

        return voids == 0 && singletons == 0 && doubletons <= 1;
    }
    
    private Bid EvaluateResponse(Auction auction, Hand hand, int hcp, bool balanced, int s, int h, int d, int c)
    {
        Compass me = auction.NextToAct;
        Compass currentWinner = auction.GetCurrentContractHolder();
        
        bool isPartner = ((int)me + 2) % 4 == (int)currentWinner;
        var lastBid = auction.ContractBid!.Value;

        if (isPartner)
        {
             return EvaluatePartnerResponse(lastBid, hcp, balanced, s, h, d, c);
        }
        else
        {
             return EvaluateOvercall(lastBid, hcp, s, h, d, c);
        }
    }

    private Bid EvaluatePartnerResponse(Bid lastBid, int hcp, bool balanced, int s, int h, int d, int c)
    {
        // Simple System:
        // 0-5 HCP: Pass
        // 6-9 HCP: Raise Major (if fit), or 1NT usually.
        // 10+ HCP: New Suit (Forcing) or Jump Raise.
        
        if (hcp < 6) return Bid.Pass;
        
        // Support Major?
        // If partner bid 1H/1S
        if (lastBid.Strain == Strain.Hearts || lastBid.Strain == Strain.Spades)
        {
            int support = (lastBid.Strain == Strain.Hearts) ? h : s;
            if (support >= 3)
            {
                // Raise.
                // If 6-9: Raise to 2
                if (hcp <= 9) return CreateSafeBid(new Bid(lastBid.Level + 1, lastBid.Strain), lastBid);
                // If 10-12: Raise to 3 (Limit)
                if (hcp <= 12) return CreateSafeBid(new Bid(lastBid.Level + 2, lastBid.Strain), lastBid);
                // If 13+: Game?
                return CreateSafeBid(new Bid(4, lastBid.Strain), lastBid);
            }
        }
        
        // If no support, or Partner bid Minor/NT:
        // Try to bid unbid Major 4+
        
        // Candidate: 1NT?
        // Only if sufficient.
        var oneNT = new Bid(1, Strain.NoTrump);
        if (oneNT.IsSufficient(lastBid) && balanced && hcp >= 6 && hcp <= 9) return oneNT;
        
        // Candidate: New Suit (Check sufficiency)
        if (s >= 5) 
        {
            var b = new Bid(1, Strain.Spades);
            if (!b.IsSufficient(lastBid)) b = new Bid(2, Strain.Spades);
            if (CanBid(b, lastBid, hcp)) return b;
        }
        if (h >= 5)
        {
            var b = new Bid(1, Strain.Hearts);
             if (!b.IsSufficient(lastBid)) b = new Bid(2, Strain.Hearts);
            if (CanBid(b, lastBid, hcp)) return b;
        }

        return Bid.Pass;
    }

    private Bid EvaluateOvercall(Bid lastBid, int hcp, int s, int h, int d, int c)
    {
        // Simple Overcall: 12+ HCP, 5+ Card Suit
        if (hcp < 12) return Bid.Pass;
        
        // Try Spades
        if (s >= 5)
        {
            var b = new Bid(1, Strain.Spades);
            if (!b.IsSufficient(lastBid)) b = new Bid(2, Strain.Spades);
            // Don't go too high on overcall without amazing hand
            if (b.Level <= 2 && b.IsSufficient(lastBid)) return b;
        }
        
        // Try Hearts
        if (h >= 5)
        {
            var b = new Bid(1, Strain.Hearts);
            if (!b.IsSufficient(lastBid)) b = new Bid(2, Strain.Hearts);
            if (b.Level <= 2 && b.IsSufficient(lastBid)) return b;
        }
        
        return Bid.Pass;
    }
    
    private bool CanBid(Bid candidate, Bid current, int hcp)
    {
        if (!candidate.IsSufficient(current)) return false;
        // Basic cap: Don't bid Level 3+ with < 12 points unless pre-empt (not implemented)
        if (candidate.Level >= 3 && hcp < 12) return false;
        return true;
    }
    
    private Bid CreateSafeBid(Bid candidate, Bid current)
    {
        if (candidate.IsSufficient(current)) return candidate;
        return Bid.Pass;
    }
}
