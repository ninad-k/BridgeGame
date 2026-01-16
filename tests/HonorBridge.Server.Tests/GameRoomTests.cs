using System;
using System.Threading.Tasks;
using HonorBridge.Engine;
using HonorBridge.Server.Services;
using Xunit;

namespace HonorBridge.Server.Tests;

public class GameRoomTests
{
    [Fact]
    public void Sit_AssignsPlayerToSeat()
    {
        var room = new GameRoom("testRoom");
        bool success = room.Sit(Compass.North, "Alice");
        
        Assert.True(success);
        var state = room.GetState("Alice");
        Assert.Equal("Alice", state.Seats["North"]);
    }
    
    [Fact]
    public void FullTable_StartsGame()
    {
        var room = new GameRoom("testRoom");
        room.Sit(Compass.North, "Alice");
        room.Sit(Compass.South, "Bob");
        room.Sit(Compass.East, "Charlie");
        room.Sit(Compass.West, "Dave");
        
        Assert.Equal(GameRoom.RoomPhase.Bidding, room.Phase);
        Assert.NotNull(room.CurrentAuction);
        Assert.NotNull(room.Deck);
    }
    
    [Fact]
    public async Task BiddingFlow_UpdatesState()
    {
        var room = new GameRoom("testRoom");
        room.Sit(Compass.North, "Alice");
        room.Sit(Compass.South, "Bob");
        room.Sit(Compass.East, "Charlie");
        room.Sit(Compass.West, "Dave"); // Starts game. Dealer North.
        
        // North bids 1H
        await room.MakeBidAsync(Compass.North, new Bid(1, Strain.Hearts));
        
        var state = room.GetState("Bob");
        Assert.Equal("1H", state.CallHistory[0]);
        Assert.Equal("East", state.NextToAct);
    }
    
    [Fact]
    public async Task PlayFlow_ValidatesTurn()
    {
         var room = new GameRoom("testRoom");
        room.Sit(Compass.North, "Alice");
        room.Sit(Compass.South, "Bob");
        room.Sit(Compass.East, "Charlie");
        room.Sit(Compass.West, "Dave");
        
        // Quick auction: 1NT - P - P - P
        await room.MakeBidAsync(Compass.North, new Bid(1, Strain.NoTrump));
        await room.MakeBidAsync(Compass.East, Bid.Pass);
        await room.MakeBidAsync(Compass.South, Bid.Pass);
        await room.MakeBidAsync(Compass.West, Bid.Pass);
        
        Assert.Equal(GameRoom.RoomPhase.Play, room.Phase);
        Assert.Equal(Compass.North, room.CurrentPlay!.Declarer); // North Declarer
        Assert.Equal(Compass.East, room.CurrentPlay.Leader); // East leads
        
        // East tries to play
        var eastHand = room.Hands[Compass.East];
        var card = eastHand.Cards[0];
        
        await room.PlayCardAsync(Compass.East, card);
        
        var state = room.GetState("Alice");
        Assert.Equal(card.ToShortString(), state.CurrentTrick["East"]);
    }

    [Fact]
    public async Task AI_TakesTurn_Automatically()
    {
        var room = new GameRoom("aiRoom");
        room.Sit(Compass.North, "Alice");
        room.Sit(Compass.South, "Bob");
        room.Sit(Compass.West, "Charlie");
        // East is AI
        room.AddAI(Compass.East);
        
        // Start Game
        // Current Dealer North (Alice)
        Assert.Equal(Compass.North, room.CurrentAuction!.NextToAct);
        
        // Alice Bids
        await room.MakeBidAsync(Compass.North, Bid.Pass);
        
        // Now it's East (AI) turn. 
        // ProcessGameLoop should be running.
        // Wait for AI delay (1000ms) + buffer
        await Task.Delay(2000);
        
        var state = room.GetState("Alice");
        // East should have bid SOMETHING (Pass or Bid)
        Assert.Equal(2, state.CallHistory.Count); 
        // Assert.Equal("Pass", state.CallHistory[1]); // Removed brittle check
        Assert.Equal("South", state.NextToAct);
    }
}
