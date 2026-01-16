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
}
