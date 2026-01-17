using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HonorBridge.Client.Wpf.Services;
using System.Threading.Tasks;
using System.Windows; // For MessageBox

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly SignalRClientService _signalR;

    [ObservableProperty]
    private string _playerName = "Player1";

    [ObservableProperty]
    private string _roomId = "Room1";

    [ObservableProperty]
    private bool _isBusy;

    public LobbyViewModel(SignalRClientService signalR)
    {
        _signalR = signalR;
    }

    [RelayCommand]
    private async Task Join()
    {
        if (string.IsNullOrWhiteSpace(PlayerName) || string.IsNullOrWhiteSpace(RoomId))
        {
            MessageBox.Show("Please enter name and room.");
            return;
        }

        IsBusy = true;
        try
        {
            await _signalR.Connect("http://localhost:5000/bridge"); // Hardcoded local for now
            await _signalR.JoinRoom(RoomId, PlayerName);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PlaySinglePlayer()
    {
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            MessageBox.Show("Please enter your name.");
            return;
        }

        IsBusy = true;
        try
        {
            // Connect first
            await _signalR.Connect("http://localhost:5000/bridge");
            
            // 1. Generate unique Room ID
            var spRoomId = "SP-" + System.Guid.NewGuid().ToString().Substring(0, 8);
            
            // 2. Join Room
            await _signalR.JoinRoom(spRoomId, PlayerName);
            
            // 3. Sit user at South (standard for single player)
            await _signalR.Sit("South");
            
            // 4. Add Bots
            await _signalR.AddBot("West");
            await _signalR.AddBot("North");
            await _signalR.AddBot("East");
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error starting single player: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
