using HonorBridge.Engine;
using Xunit;
using System;

namespace HonorBridge.Engine.Tests;

public class ScoringTests
{
    [Fact]
    public void Calculate_Partscore_NoneVul()
    {
        // 2S made 3 (Tricks=9). Non-Vul.
        // Base: 2*30 = 60. Overtrick: 30. Total: 90.
        // Bonus: 50 (Partscore).
        // Grand Total: 140.
        var contract = new Bid(2, Strain.Spades);
        var result = Scoring.Calculate(contract, CallType.Pass, 9, Vulnerability.None, Compass.North);
        
        Assert.True(result.Made);
        Assert.Equal(140, result.Points);
    }
    
    [Fact]
    public void Calculate_Game_Vul()
    {
        // 4H made 4 (Tricks=10). Vul.
        // Base: 4*30 = 120. (>100 -> Game).
        // Bonus: 500 (Game Vul).
        // Total: 620.
        var contract = new Bid(4, Strain.Hearts);
        var result = Scoring.Calculate(contract, CallType.Pass, 10, Vulnerability.Both, Compass.North); // Both Vul
        
        Assert.Equal(620, result.Points);
    }

    [Fact]
    public void Calculate_NT_Game_NonVul()
    {
        // 3NT made 3 (Tricks=9). Non-Vul.
        // Base: 40 + 30 + 30 = 100.
        // Bonus: 300 (Game NonVul).
        // Total: 400.
        var contract = new Bid(3, Strain.NoTrump);
        var result = Scoring.Calculate(contract, CallType.Pass, 9, Vulnerability.None, Compass.North);
        
        Assert.Equal(400, result.Points);
    }
    
    [Fact]
    public void Calculate_SmallSlam_Vul()
    {
        // 6D Made 6. (Tricks=12). Vul.
        // Base: 6*20 = 120.
        // Game Bonus: 500.
        // Slam Bonus: 750 (Small Vul).
        // Total: 1370.
        var contract = new Bid(6, Strain.Diamonds);
        var result = Scoring.Calculate(contract, CallType.Pass, 12, Vulnerability.Both, Compass.North);
        
        Assert.Equal(1370, result.Points);
    }
    
    [Fact]
    public void Calculate_Doubled_Penalty_NonVul()
    {
        // 1NT X -1 (Tricks=6). NonVul.
        // Penalty:
        // Down 1 = 100.
        // Score: -100.
        var contract = new Bid(1, Strain.NoTrump);
        var result = Scoring.Calculate(contract, CallType.Double, 6, Vulnerability.None, Compass.North);
        
        Assert.False(result.Made);
        Assert.Equal(-100, result.Points);
    }
    
    [Fact]
    public void Calculate_Doubled_Penalty_Vul()
    {
        // 1NT X -2 (Tricks=5). Vul.
        // Down 1: 200.
        // Down 2: 300.
        // Total: -500.
        var contract = new Bid(1, Strain.NoTrump);
        var result = Scoring.Calculate(contract, CallType.Double, 5, Vulnerability.NS, Compass.North);
        
        Assert.Equal(-500, result.Points);
    }
    
    [Fact]
    public void Calculate_Insult_Bonus()
    {
        // 2H X made 2. NonVul.
        // Base: 2 * 30 * 2 = 120.
        // Game Bonus: 300 (since 120 >= 100).
        // Insult: 50.
        // Total: 470.
        var contract = new Bid(2, Strain.Hearts);
        var result = Scoring.Calculate(contract, CallType.Double, 8, Vulnerability.None, Compass.North);
        
        Assert.Equal(470, result.Points);
    }
}
