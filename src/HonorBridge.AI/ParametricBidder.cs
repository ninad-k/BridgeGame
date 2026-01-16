using System.Linq;
using HonorBridge.Engine;

namespace HonorBridge.AI;

public class ParametricBidder : IBiddingSystem
{
    public string Name { get; }
    
    // Configuration
    public int NtMin { get; }
    public int NtMax { get; }
    public int MajorMinLength { get; } // 4 or 5
    // Can extend with "UseStrongTwoClubs", "CurrentStyle" etc.

    public ParametricBidder(string name, int ntMin, int ntMax, int majorMinLength)
    {
        Name = name;
        NtMin = ntMin;
        NtMax = ntMax;
        MajorMinLength = majorMinLength;
    }

    // Factory methods for specific systems
    public static ParametricBidder SAYC => new ParametricBidder("SAYC", 15, 17, 5);
    public static ParametricBidder Acol => new ParametricBidder("Acol", 12, 14, 4); // Weak NT, 4-card majors
    public static ParametricBidder Goren => new ParametricBidder("Goren", 16, 18, 4); // Stronger NT, 4-card majors

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
            // Simple pass for now or delegate to Response Logic 
            // Reuse logic from SaycBidder but adapted? 
            // For Phase 10, let's port the logic structure we built in SaycBidder inside here.
            // Ideally SaycBidder logic should have been here.
            // For MVP: Return Pass for response to avoid duplicating complex response logic immediately,
            // OR simpler: Just Openings change by system, keep Responses generic?
            // Responses depend on Opening System (e.g. Acol response to 1NT is different).
            // Let's implement Opening logic properly. 
            return Bid.Pass; 
        }
    }
    
    private Bid EvaluatOpening(int hcp, bool balanced, int s, int h, int d, int c)
    {
        if (hcp < 12) return Bid.Pass; // Basic opening threshold usually 12

        // 1NT Opening
        if (balanced && hcp >= NtMin && hcp <= NtMax) return new Bid(1, Strain.NoTrump);

        // Majors
        // Check Min Length
        if (s >= MajorMinLength && s >= h) return new Bid(1, Strain.Spades);
        if (h >= MajorMinLength) return new Bid(1, Strain.Hearts);

        // Minors (Default logic 3+)
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
        voids += (4 - dist.Count);

        return voids == 0 && singletons == 0 && doubletons <= 1;
    }
}
