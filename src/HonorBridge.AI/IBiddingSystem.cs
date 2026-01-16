using HonorBridge.Engine;

namespace HonorBridge.AI;

public interface IBiddingSystem
{
    string Name { get; }
    Bid GetBestBid(Auction auction, Hand hand);
}
