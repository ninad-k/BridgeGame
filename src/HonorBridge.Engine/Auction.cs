using System;
using System.Collections.Generic;
using System.Linq;

namespace HonorBridge.Engine;

public class Auction
{
    public Compass Dealer { get; }
    public Compass NextToAct { get; private set; }
    public Bid? ContractBid { get; private set; }
    public Compass? Declarer { get; private set; }
    public CallType CurrentDoubledState { get; private set; } = CallType.Pass; // Pass (None), Double, Redouble
    public bool IsComplete { get; private set; }

    private readonly List<Bid> _calls = new();
    public IReadOnlyList<Bid> History => _calls.AsReadOnly();

    // Track first bidder for each strain for each partnership to determine Declarer
    private readonly Dictionary<Compass, Dictionary<Strain, bool>> _strainBids = new();

    public Auction(Compass dealer)
    {
        Dealer = dealer;
        NextToAct = dealer;
        
        foreach (Compass c in Enum.GetValues(typeof(Compass)))
        {
            _strainBids[c] = new Dictionary<Strain, bool>();
        }
    }

    public void MakeCall(Bid call)
    {
        if (IsComplete)
            throw new InvalidOperationException("Auction is already complete.");

        ValidateCall(call);

        _calls.Add(call);

        if (call.CallType == CallType.Bid)
        {
            ContractBid = call;
            CurrentDoubledState = CallType.Pass;
            
            // Record that this player bid this strain (for Declarer logic)
            if (!_strainBids[NextToAct].ContainsKey(call.Strain))
            {
                _strainBids[NextToAct][call.Strain] = true;
            }
        }
        else if (call.CallType == CallType.Double)
        {
            CurrentDoubledState = CallType.Double;
        }
        else if (call.CallType == CallType.Redouble)
        {
            CurrentDoubledState = CallType.Redouble;
        }

        CheckCompletion();
        NextToAct = NextCompass(NextToAct);
    }

    private void ValidateCall(Bid call)
    {
        switch (call.CallType)
        {
            case CallType.Bid:
                if (ContractBid != null && !call.IsSufficient(ContractBid.Value))
                    throw new InvalidOperationException($"Bid {call} is insufficient over {ContractBid}.");
                break;

            case CallType.Double:
                // Can only double if current state is Undoubled contract by Opponents
                if (ContractBid == null)
                    throw new InvalidOperationException("Cannot double when no contract bid exists.");
                if (IsPartner(NextToAct, GetContractBidder()))
                     throw new InvalidOperationException("Cannot double your partner's bid.");
                if (CurrentDoubledState != CallType.Pass)
                    throw new InvalidOperationException("Cannot double; already doubled or redoubled.");
                break;

            case CallType.Redouble:
                // Can only redouble if current state is Doubled by Opponents
                 if (ContractBid == null)
                    throw new InvalidOperationException("Cannot redouble when no contract bid exists.");
                if (IsPartner(NextToAct, GetContractBidder()))
                     // Partner was doubled, so I can redouble. OK.
                     // Logic: Contract by MySide, modify by Opps (Double), I Redouble.
                     // The last "meaningful" action must be a Double.
                     {}
                else
                    // Opponent bid, Partner doubled. I cannot redouble. I can bid or pass.
                    // Wait, Redouble is only valid if the contract implies *we* are doubled.
                    // If Contract is by US, and State is Doubled (by THEM), then WE can Redouble.
                    // If Contract is by THEM, and State is Doubled (by US), then THEY can Redouble.
                    {}
                
                // Simplified check:
                // To Redouble, the current state MUST be Doubled.
                if (CurrentDoubledState != CallType.Double)
                     throw new InvalidOperationException("Cannot redouble; must be doubled first.");
                
                // And the Doubler must be an opponent (implied by turn order, since you can't double partner).
                // Actually, ensure we are on the side that was doubled.
                // If ContractBidder is MySide -> We were doubled -> We can Redouble.
                // If ContractBidder is OppSide -> They were doubled -> We cannot Redouble (we just doubled them!).
                bool isMySidContract = IsPartner(NextToAct, GetContractBidder()) || NextToAct == GetContractBidder();
                if(!isMySidContract)
                    throw new InvalidOperationException("Cannot redouble opponents' doubled contract (they must redouble).");

                break;

            case CallType.Pass:
                break;
        }
    }

    private void CheckCompletion()
    {
        if (_calls.Count >= 4)
        {
            var last3 = _calls.TakeLast(3).ToList();
            if (last3.All(c => c.CallType == CallType.Pass))
            {
                // Passed out (4 passes) or 3 passes after a bid
                if (_calls.Count == 4 && _calls[0].CallType == CallType.Pass)
                {
                    // Passed out deal
                    IsComplete = true;
                    ContractBid = null;
                    Declarer = null;
                }
                else if (ContractBid != null)
                {
                    // Valid contract established
                    IsComplete = true;
                    DetermineDeclarer();
                }
            }
        }
    }

    private void DetermineDeclarer()
    {
        if (ContractBid == null) return;

        var strain = ContractBid.Value.Strain;
        var winner = GetContractBidder();

        // Declarer is the first player of the partnership to bid the strain
        // Partnership: (N/S) or (E/W)
        
        Compass p1, p2;
        if (winner == Compass.North || winner == Compass.South)
        {
            p1 = Compass.North;
            p2 = Compass.South;
        }
        else
        {
            p1 = Compass.East;
            p2 = Compass.West;
        }

        // Check history to see who bid strain first
        // We iterate bids. If bid is by p1/p2 and strain matches, that's the declarer.
        // We need to re-scan _calls because _strainBids doesn't store order perfectly if we just used bools.
        // But _strainBids was simple. Let's do a scan.
        
        Compass actor = Dealer;
        foreach (var call in _calls)
        {
            if (call.CallType == CallType.Bid && call.Strain == strain)
            {
                if (actor == p1 || actor == p2)
                {
                    Declarer = actor;
                    return;
                }
            }
            actor = NextCompass(actor);
        }
    }

    public Compass GetCurrentContractHolder()
    {
        // Scan backwards to find the last bid
        // Because of passes/doubles, ContractBid variable holds the bid content, but not WHO made it.
        // We need to calculate who made ContractBid.
        
        // Quick way: Replay indices
        Compass actor = Dealer;
        Compass lastBidder = Dealer;
        foreach(var call in _calls)
        {
            if (call.CallType == CallType.Bid)
                lastBidder = actor;
            actor = NextCompass(actor);
        }
        return lastBidder;
    }

    private Compass GetContractBidder() => GetCurrentContractHolder();

    private Compass NextCompass(Compass c)
    {
        return (Compass)(((int)c + 1) % 4);
    }

    private bool IsPartner(Compass a, Compass b)
    {
        return (a == Compass.North && b == Compass.South) ||
               (a == Compass.South && b == Compass.North) ||
               (a == Compass.East && b == Compass.West) ||
               (a == Compass.West && b == Compass.East);
    }
}
