using CommunityToolkit.Mvvm.ComponentModel;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class HowToPlayViewModel : ObservableObject
{
    public string Title => "How to Play Bridge";
    
    // Content Sections
    
    public string BasicsTitle => "1. The Basics";
    public string BasicsContent => 
@"Bridge is played by four players in two partnerships (North-South and East-West) using a standard 52-card deck.
Rank of Suits (High to Low): Spades, Hearts, Diamonds, Clubs.
Rank of Cards: Ace, King, Queen, Jack, 10, 9...2.

The game has two phases: The Auction (Bidding) and The Play.";

    public string AuctionTitle => "2. The Auction";
    public string AuctionContent => 
@"Players bid to decide the 'Contract'—how many tricks they commit to taking.
A Bid consists of a Level (1-7) and a Strain (Club, Diamond, Heart, Spade, NoTrump).
Contract Level + 6 = Number of Tricks Required.
Example: 1 Spade contract requires taking 1 + 6 = 7 tricks.

The Auction ends after 3 consecutive Passes. The highest bidder becomes the Declarer.";

    public string PlayTitle => "3. The Play";
    public string PlayContent => 
@"The player to the left of the Declarer leads the first card.
Declarer's partner (Dummy) lays their cards face up. Declarer plays for both.
Players must follow suit if possible.
Highest card of the suit led wins, unless a Trump is played (if a Trump suit was named).
Winner of the trick leads next.";

    public string SystemsTitle => "4. Bidding Systems";
    public string SystemsIntro => 
@"Partnerships use agreed systems to communicate hand strength and shape via bids. Here are common systems:";

    public string SaycTitle => "SAYC (Standard American Yellow Card)";
    public string SaycContent => 
@"The system used by Honor Bridge AI.
- 5-Card Majors: Opening 1H/1S implies 5+ cards.
- Strong NoTrump: Opening 1NT implies 15-17 High Card Points (HCP) and balanced hand.
- Convenience Minors: 1C/1D openings can be on 3 cards.
- Suit Preference: Spades > Hearts > Diamonds > Clubs.";

    public string GorenTitle => "Goren (Traditional)";
    public string GorenContent => 
@"Charles Goren's system, popular in the mid-20th century.
- Focuses on Point Count (HCP + Distribution points).
- 4-Card Majors: Older style allowed opening 1H/1S on 4 cards.
- Strong Two-Bids: Opening 2 of a suit was game forcing (modern SAYC uses 2C for strong hands and 2D/H/S for weak).";

    public string AcolTitle => "Acol (British Standard)";
    public string AcolContent => 
@"Common in the UK.
- Weak NoTrump: Opening 1NT implies 12-14 HCP (riskier but preemptive).
- 4-Card Majors: Often opens 4-card major suits.
- Gamble: Encourages aggressive partial bidding.";

    public string TwoOverOneTitle => "2/1 Game Force";
    public string TwoOverOneContent => 
@"Modern tournament standard (an evolution of SAYC).
- If partner opens 1 Major, and you respond with a new suit at level 2 (e.g. 1S - 2C), it forces game.
- Allows slow, descriptive auctions to find the perfect slam or game contract.";
}
