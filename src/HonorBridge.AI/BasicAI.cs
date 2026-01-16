using System;
using System.Linq;
using System.Threading.Tasks;
using HonorBridge.Engine;

namespace HonorBridge.AI;

public class BasicAI : IBridgePlayer
{
    private readonly Random _rng = new();

    public Task<Bid> GetBidAsync(Auction auction, Hand myHand)
    {
        // Level 1: Always Pass (easiest legal bid)
        // Improvement: If 3 passes and we are 4th, maybe bid something?
        // But "Always Pass" is safe and strictly legal for v1 MVP.
        return Task.FromResult(Bid.Pass);
    }

    public Task<Card> GetCardAsync(DealPlay play, Hand myHand, Compass mySeat)
    {
        // Must follow suit if able
        Suit? ledSuit = play.CurrentTrick.Cards.Count > 0 ? play.CurrentTrick.LedSuit : null;

        var candidates = myHand.Cards.ToList();

        if (ledSuit.HasValue)
        {
            var followers = candidates.Where(c => c.Suit == ledSuit.Value).ToList();
            if (followers.Any())
            {
                candidates = followers;
            }
        }

        // Pick Randomly from legal candidates
        int index = _rng.Next(candidates.Count);
        return Task.FromResult(candidates[index]);
    }
}
