using System.Collections.Generic;

namespace HonorBridge.Shared.Models;

public class GameStateDto
{
    public string RoomId { get; set; } = "";
    public string Phase { get; set; } = ""; // Waiting, Bidding, Play, Scoring
    
    // Seat -> PlayerName
    public Dictionary<string, string?> Seats { get; set; } = new();
    
    public string? MySeat { get; set; } // "North", "South" etc.

    // My Hand (Card strings like "AS", "2H")
    public List<string> MyHand { get; set; } = new();
    
    // Hand Counts for other players
    public Dictionary<string, int> HandCounts { get; set; } = new();

    // Auction Info
    public string Dealer { get; set; } = "";
    public string NextToAct { get; set; } = ""; // Compass
    public List<string> CallHistory { get; set; } = new();
    public string? Contract { get; set; }
    public string? Declarer { get; set; }

    // Play Info
    public Dictionary<string, string> CurrentTrick { get; set; } = new(); // Seat -> Card
    public List<string> DummyHand { get; set; } = new(); // Visible during play
    public List<string> PartnerHand { get; set; } = new(); // Visible for Double Dummy / Partner Takeover
    public int TricksNS { get; set; }
    public int TricksEW { get; set; }
    
    // Last Score
    public string? LastScore { get; set; }
    public int LastPoints { get; set; }
    
    // Last Trick Info (for delay phase)
    public string? LastTrickWinner { get; set; }
}
