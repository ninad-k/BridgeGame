using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.AspNetCore.SignalR;
using HonorBridge.Server.Hubs;

namespace HonorBridge.Server.Services;

public class LobbyService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly IHubContext<BridgeHub> _hubContext;

    public LobbyService(IHubContext<BridgeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public GameRoom GetOrCreateRoom(string roomId)
    {
        return _rooms.GetOrAdd(roomId, id => 
        {
            var room = new GameRoom(id);
            // Subscribe to state changes to notify clients
            room.OnStateChanged += async () => 
            {
                await _hubContext.Clients.Group(room.RoomId).SendAsync("StateUpdated");
            };
            return room;
        });
    }

    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }
}
