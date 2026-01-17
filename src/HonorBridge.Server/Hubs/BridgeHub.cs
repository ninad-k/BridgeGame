using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using HonorBridge.Server.Services;
using HonorBridge.Shared.Models;
using HonorBridge.Engine;

namespace HonorBridge.Server.Hubs;

public class BridgeHub : Hub
{
    private readonly LobbyService _lobbyService;

    // Context Items Keys
    private const string RoomIdKey = "RoomId";
    private const string PlayerNameKey = "PlayerName";

    // Maps connection ID to seat? 
    // Actually GameRoom handles mapping Compass->PlayerName.
    // We assume PlayerName is unique per room or session.
    
    public BridgeHub(LobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task JoinRoom(string roomId, string playerName)
    {
        Context.Items[RoomIdKey] = roomId;
        Context.Items[PlayerNameKey] = playerName;
        
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        
        var room = _lobbyService.GetOrCreateRoom(roomId);
        
        // Subscribe to state changes? 
        // Hub instances are transient per call. We cannot subscribe instance method to Room event.
        // GameRoom needs a way to broadcast.
        // We can pass `IHubContext` to GameRoom? Or use LobbyService as broadcaster?
        // Architecture issue: GameRoom (Singleton/Persistent) -> Hub (Transient) communication.
        // Solution: GameRoom takes `IHubContext<BridgeHub>` in constructor if resolved via DI, 
        // OR LobbyService manages the subscription.
        // For MVP: GameRoom just fires event, and we need a Singleton listener.
        // Simpler MVP: GameRoom holds reference to a broadcast delegate?
        // Let's keep it polling-based from Client as implied by previous "StateUpdated" message.
        // BUT `ProcessGameLoop` runs in background. Clients won't know to poll.
        // So `ProcessGameLoop` MUST send "StateUpdated".
        // Let's Inject IHubContext into LobbyService or GameRoom factory.
        
        // For now, let's just send the initial state.
        await SendStateToGroup(room);
    }
    
    public async Task AddBot(string compass)
    {
         var (room, playerName) = GetRoomAndPlayer();
        if (room == null) return;
        
        if (Enum.TryParse<Compass>(compass, out var c))
        {
            room.AddAI(c);
            await SendStateToGroup(room);
        }
    }
    
    public async Task Sit(Compass seat)
    {
        var (room, playerName) = GetRoomAndPlayer();
        if (room == null || playerName == null) return;

        try
        {
            bool success = room.Sit(seat, playerName);
            if (!success)
            {
                await Clients.Caller.SendAsync("ReceiveError", "Seat taken.");
            }
            else
            {
                await SendStateToGroup(room);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveError", ex.Message);
        }
    }

    public async Task PlaceBid(int level, string strain, string callType)
    {
        var (room, playerName) = GetRoomAndPlayer();
        if (room == null || playerName == null) return;
        
        var seatKvp = room.GetState(playerName).Seats.FirstOrDefault(s => s.Value == playerName);
        if (seatKvp.Value == null) 
        {
             await Clients.Caller.SendAsync("ReceiveError", "You are not seated.");
             return;
        }
        if(!Enum.TryParse<Compass>(seatKvp.Key, out var seat)) return;

        try
        {
            Bid bid;
            if (callType == "Pass") bid = Bid.Pass;
            else if (callType == "Double") bid = Bid.Double;
            else if (callType == "Redouble") bid = Bid.Redouble;
            else 
            {
                if(!Enum.TryParse<Strain>(strain, out var s)) throw new ArgumentException("Invalid strain");
                bid = new Bid(level, s);
            }

            await room.MakeBidAsync(seat, bid);
            await SendStateToGroup(room);
        }
        catch (Exception ex)
        {
             await Clients.Caller.SendAsync("ReceiveError", ex.Message);
        }
    }

    public async Task PlayCard(string cardShortString)
    {
        var (room, playerName) = GetRoomAndPlayer();
        if (room == null || playerName == null) return;
        
        var seatKvp = room.GetState(playerName).Seats.FirstOrDefault(s => s.Value == playerName);
        if (seatKvp.Value == null) 
        {
             await Clients.Caller.SendAsync("ReceiveError", "You are not seated.");
             return;
        }
         if(!Enum.TryParse<Compass>(seatKvp.Key, out var seat)) return;

        try
        {
            Card card = ParseCard(cardShortString);
            await room.PlayCardAsync(seat, card);
            await SendStateToGroup(room);
        }
        catch (Exception ex)
        {
             await Clients.Caller.SendAsync("ReceiveError", ex.Message);
        }
    }

    private async Task SendStateToGroup(GameRoom room)
    {
        // We need to send custom state to EACH player because "MyHand" is private.
        // Iterate connections in group? 
        // SignalR doesn't give easy access to "All connections in Group and their Context Items".
        // Solution: Client receives "PublicState" and "MyHand" separately? 
        // Or we notify "StateUpdated" and Clients call "GetMyState"? << Safest for v1 privacy.
        // But "Push" is better for "Realtime".
        
        // For v1 Prototype: Send *Full State including all hands* to everyone? NO. Cheating risk.
        // Better: Iterate known players in Room, map them to UserIDs/ConnectionIDs?
        // SignalR Hub doesn't track "PlayerName -> ConnectionId" unless we implement IUserTracker.
        // 
        // Simplified approach for MVP:
        // Use `Clients.Group(roomId).SendAsync("ReceiveState", publicDto)`
        // AND `Clients.Client(connId).SendAsync("ReceiveHand", hand)`
        
        // But we don't track ConnectionIDs in GameRoom.
        // Let's change strategy: Broadcast "UpdatePending". Clients call "FetchState".
        // Protocol: Server -> Client: "StateUpdated". Client -> Server: "GetState".
        
        await Clients.Group(room.RoomId).SendAsync("StateUpdated");
    }

    public async Task<GameStateDto?> GetState()
    {
        var (room, playerName) = GetRoomAndPlayer();
        if (room == null) return null;
        
        return room.GetState(playerName ?? "");
    }

    private (GameRoom?, string?) GetRoomAndPlayer()
    {
        if (Context.Items.TryGetValue(RoomIdKey, out var rObj) && rObj is string roomId &&
            Context.Items.TryGetValue(PlayerNameKey, out var pObj) && pObj is string playerName)
        {
            return (_lobbyService.GetRoom(roomId), playerName);
        }
        return (null, null);
    }
    
    private Card ParseCard(string s)
    {
        // Format: "AS", "TD", "2C"
        if(s.Length < 2) throw new ArgumentException("Invalid card format");
        
        char rankChar = s[0];
        char suitChar = s[^1]; // Last char is always suit
        
        Suit suit = suitChar switch {
            'C' => Suit.Clubs,
            'D' => Suit.Diamonds,
            'H' => Suit.Hearts,
            'S' => Suit.Spades,
            _ => throw new ArgumentException("Invalid suit")
        };
        
        Rank rank = rankChar switch {
            'A' => Rank.Ace,
            'K' => Rank.King,
            'Q' => Rank.Queen,
            'J' => Rank.Jack,
            'T' => Rank.Ten,
            '9' => Rank.Nine,
            '8' => Rank.Eight,
            '7' => Rank.Seven,
            '6' => Rank.Six,
            '5' => Rank.Five,
            '4' => Rank.Four,
            '3' => Rank.Three,
            '2' => Rank.Two,
            _ => throw new ArgumentException("Invalid rank")
        };
        
        return new Card(suit, rank);
    }
}
