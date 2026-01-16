using System.Threading.Tasks;
using HonorBridge.Engine;

namespace HonorBridge.AI;

public interface IBridgePlayer
{
    Task<Bid> GetBidAsync(Auction auction, Hand myHand);
    Task<Card> GetCardAsync(DealPlay play, Hand myHand, Compass mySeat);
}
