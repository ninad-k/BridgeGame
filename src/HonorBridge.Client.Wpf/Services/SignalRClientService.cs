using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using HonorBridge.Shared.Models;
using HonorBridge.Engine; // For Compass enum if needed, though DTOs use strings mostly.

namespace HonorBridge.Client.Wpf.Services;

public class SignalRClientService
{
    private HubConnection _connection;
    
    public event Action<GameStateDto>? StateUpdated;
    public event Action<string>? ErrorReceived;
    public event Action? ConnectionReconnecting;
    public event Action? ConnectionReconnected;
    public event Action? ConnectionClosed;

    public string? ConnectionId => _connection?.ConnectionId;

    public async Task Connect(string serverUrl) // e.g., "https://localhost:5001/bridge"
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On("StateUpdated", () => 
        {
            // Server says state changed. We should fetch it?
            // Wait, my Hub logic says: await Clients.Group(room.RoomId).SendAsync("StateUpdated");
            // So we need to Fetch.
            // But we also need to trigger the UI to fetch.
             // We can fire an event that causes the ViewModel to call GetState().
             // OR we can just fetch it here and propagate.
             // Let's fetch it here to keep VMs cleaner.
            _ = FetchAndNotifyState();
        });
        
        _connection.On<string>("ReceiveError", (msg) => 
        {
            ErrorReceived?.Invoke(msg);
        });

        _connection.Reconnecting += (error) => 
        {
            ConnectionReconnecting?.Invoke();
            return Task.CompletedTask;
        };
        
        _connection.Reconnected += (id) => 
        {
            ConnectionReconnected?.Invoke();
             _ = FetchAndNotifyState();
            return Task.CompletedTask;
        };
        
        _connection.Closed += (error) => 
        {
            ConnectionClosed?.Invoke();
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }
    
    private async Task FetchAndNotifyState()
    {
        try
        {
            var state = await _connection.InvokeAsync<GameStateDto?>("GetState");
            if (state != null)
            {
                StateUpdated?.Invoke(state);
            }
        }
        catch (Exception)
        {
            // ErrorReceived?.Invoke("Failed to sync state: " + ex.Message);
        }
    }

    public async Task JoinRoom(string roomId, string playerName)
    {
        if (_connection.State != HubConnectionState.Connected) throw new InvalidOperationException("Not connected");
        await _connection.InvokeAsync("JoinRoom", roomId, playerName);
        // Implicitly expect StateUpdated or fetch immediately?
        await FetchAndNotifyState();
    }
    
    public async Task Sit(string compass)
    {
        // Hub expects Compass enum. SignalR JSON serialization might handle string->Enum if configured, 
        // but typically it sends int unless using StringEnumConverter.
        // It's safer to send int or handle conversion.
        // My Hub implementation: `public async Task Sit(Compass seat)`
        // If client sends String "North", System.Text.Json default might fail unless configured.
        // Let's rely on default int for safety if we don't know server config.
        // Compass: N=0, E=1, S=2, W=3.
        
        if (Enum.TryParse<Compass>(compass, out var c))
        {
            await _connection.InvokeAsync("Sit", c);
        }
    }

    public async Task PlaceBid(int level, string strain, string callType)
    {
         await _connection.InvokeAsync("PlaceBid", level, strain, callType);
    }
    
    public async Task PlayCard(string cardShortString)
    {
        await _connection.InvokeAsync("PlayCard", cardShortString);
    }

    public async Task AddBot(string compass)
    {
        // Hub expects string for AddBot?
        // Hub signature: public async Task AddBot(string compass)
        await _connection.InvokeAsync("AddBot", compass);
    }

    public async Task SetBiddingSystem(string systemName)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("SetBiddingSystem", systemName);
    }
}
