using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HonorBridge.Server.Services;

public class LobbyService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom GetOrCreateRoom(string roomId)
    {
        return _rooms.GetOrAdd(roomId, id => new GameRoom(id));
    }

    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }
}
