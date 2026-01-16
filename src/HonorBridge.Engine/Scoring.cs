using System;

namespace HonorBridge.Engine;

public static class Scoring
{



    
    // Adjusted signature method
    public static ScoreResult Calculate(Bid contract, CallType doubledState, int tricksTaken, Vulnerability vulnerability, Compass declarer)
    {
         if (contract.CallType != CallType.Bid)
            throw new ArgumentException("Must be a valid bid contract.");

        int book = 6;
        int tricksContracted = contract.Level;
        int tricksRequired = book + tricksContracted;
        
        bool isVul = false;
        if (vulnerability == Vulnerability.Both) isVul = true;
        else if (vulnerability == Vulnerability.NS && (declarer == Compass.North || declarer == Compass.South)) isVul = true;
        else if (vulnerability == Vulnerability.EW && (declarer == Compass.East || declarer == Compass.West)) isVul = true;

        if (tricksTaken >= tricksRequired)
        {
            return CalculateMade(contract, doubledState, tricksTaken, tricksContracted, isVul);
        }
        else
        {
            int down = tricksRequired - tricksTaken;
            return CalculateDown(doubledState, down, isVul);
        }
    }

    private static ScoreResult CalculateMade(Bid contract, CallType doubledState, int tricksTaken, int tricksContracted, bool isVul)
    {
        int overtricks = tricksTaken - (6 + tricksContracted);
        
        int baseTrickScore = 0;
        int trickFactor = 1;
        if (doubledState == CallType.Double) trickFactor = 2;
        if (doubledState == CallType.Redouble) trickFactor = 4;

        // Trick Points
        if (contract.Strain == Strain.Clubs || contract.Strain == Strain.Diamonds)
        {
            baseTrickScore = 20 * tricksContracted;
        }
        else if (contract.Strain == Strain.Hearts || contract.Strain == Strain.Spades)
        {
            baseTrickScore = 30 * tricksContracted;
        }
        else // NT
        {
            baseTrickScore = 40 + (30 * (tricksContracted - 1));
        }
        
        int contractPoints = baseTrickScore * trickFactor;
        
        // Level Bonus (Game/Partscore)
        int levelBonus = 0;
        if (contractPoints >= 100)
        {
            levelBonus = isVul ? 500 : 300;
        }
        else
        {
            levelBonus = 50;
        }

        // Slam Bonus
        int slamBonus = 0;
        if (tricksContracted == 6) slamBonus = isVul ? 750 : 500;
        else if (tricksContracted == 7) slamBonus = isVul ? 1500 : 1000;

        // Insult
        int insult = 0;
        if (doubledState == CallType.Double) insult = 50;
        if (doubledState == CallType.Redouble) insult = 100;

        // Overtricks
        int overtrickPoints = 0;
        if (overtricks > 0)
        {
            if (doubledState == CallType.Pass)
            {
                // Undoubled: Trick value (Diamonds/Clubs -> 20, Majors/NT -> 30)
                int perTrick = (contract.Strain == Strain.Clubs || contract.Strain == Strain.Diamonds) ? 20 : 30;
                overtrickPoints = overtricks * perTrick;
            }
            else if (doubledState == CallType.Double)
            {
                overtrickPoints = overtricks * (isVul ? 200 : 100);
            }
            else // Redoubled
            {
                overtrickPoints = overtricks * (isVul ? 400 : 200);
            }
        }

        int total = contractPoints + levelBonus + slamBonus + insult + overtrickPoints;
        
        string desc = $"{contract.Level}{FormatStrain(contract.Strain)}";
        if (doubledState == CallType.Double) desc += " X";
        else if (doubledState == CallType.Redouble) desc += " XX";
        
        if (overtricks > 0) desc += $" +{overtricks}";
        else desc += " =";

        return new ScoreResult(total, true, desc);
    }

    private static ScoreResult CalculateDown(CallType doubledState, int down, bool isVul)
    {
        int penalty = 0;

        if (doubledState == CallType.Pass)
        {
            // Undoubled
            int perTrick = isVul ? 100 : 50;
            penalty = down * perTrick;
        }
        else if (doubledState == CallType.Double)
        {
             // Doubled
            if (!isVul)
            {
                // 100 for 1st, 200 for 2nd/3rd, 300 for 4th+
                if (down >= 1) penalty += 100;
                if (down >= 2) penalty += 200;
                if (down >= 3) penalty += 200;
                if (down >= 4) penalty += (down - 3) * 300;
            }
            else
            {
                // Vul: 200 for 1st, 300 for subsequent
                if (down >= 1) penalty += 200;
                if (down > 1) penalty += (down - 1) * 300;
            }
        }
        else // Redoubled
        {
            // 2x Doubled penalty
            int doubledPenalty = 0;
             if (!isVul)
            {
                if (down >= 1) doubledPenalty += 100;
                if (down >= 2) doubledPenalty += 200;
                if (down >= 3) doubledPenalty += 200;
                if (down >= 4) doubledPenalty += (down - 3) * 300;
            }
            else
            {
                if (down >= 1) doubledPenalty += 200;
                if (down > 1) doubledPenalty += (down - 1) * 300;
            }
            penalty = doubledPenalty * 2;
        }

        string desc = $"-{down}";
        return new ScoreResult(-penalty, false, desc);
    }
    
    private static string FormatStrain(Strain s) => s switch
    {
        Strain.NoTrump => "NT",
        Strain.Clubs => "C",
        Strain.Diamonds => "D",
        Strain.Hearts => "H",
        Strain.Spades => "S",
        _ => ""
    };
}
