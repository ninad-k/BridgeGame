using CommunityToolkit.Mvvm.ComponentModel;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    public string Title => "How to Play Honor Bridge";
    
    public string RulesText => 
@"1. Bidding Phase:
   - Players bid to determine the contract.
   - Use 'Bid' to state Level + Strain.
   - 'Pass', 'Double', 'Redouble' are available.

2. Play Phase:
   - Declarer (winner of auction) plays for both themselves and Dummy.
   - Follow Suit rule is strictly enforced.
   - Tricks are won by the highest card of the led suit or highest trump.

3. Scoring:
   - Duplicate Bridge scoring is used.
   - Vulnerability affects penalties and bonuses.

4. AI:
   - Currently, AI opponents (Level 1) play random legal cards.
   - Advanced AI Logic is planned for future updates.";
}
