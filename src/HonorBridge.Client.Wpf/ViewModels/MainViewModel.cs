using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HonorBridge.Client.Wpf.Services;
using HonorBridge.Shared.Models; // For AILevel
using Microsoft.Extensions.DependencyInjection; // For navigating to other VMs
using System;
using System.Threading.Tasks;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignalRClientService _signalR;
    
    [ObservableProperty]
    private object _currentView;

    public MainViewModel(IServiceProvider serviceProvider, SignalRClientService signalR)
    {
        _serviceProvider = serviceProvider;
        _signalR = signalR;
        
        // Start with Lobby
        CurrentView = _serviceProvider.GetRequiredService<LobbyViewModel>();
        
        _signalR.StateUpdated += OnStateUpdated;
    }
    
    private void OnStateUpdated(HonorBridge.Shared.Models.GameStateDto state)
    {
        // CurrentView = _serviceProvider.GetRequiredService<LobbyViewModel>(); // Original
        CurrentView = lobbyVm; // New
        
        // _signalR.StateUpdated += OnStateUpdated; // Original
        _signalR.StateUpdated += (state) => 
        {
            // If we are in Lobby and state shows valid room, switch?
            // Or just let Lobby button do it.
            // For now, we manually switch views via commands.
        };
    }
    
    // private void OnStateUpdated(HonorBridge.Shared.Models.GameStateDto state) // Original method removed
    // {
    //     // Auto-navigate to Table if joined a room
    //     if (CurrentView is LobbyViewModel && !string.IsNullOrEmpty(state.RoomId))
    //     {
    //         // Switch to GameTable
    //          App.Current.Dispatcher.Invoke(() => 
    //          {
    //              var vm = _serviceProvider.GetRequiredService<GameTableViewModel>();
    //              vm.UpdateState(state);
    //              CurrentView = vm;
    //          });
    //     }
    //     else if (CurrentView is GameTableViewModel vm)
    //     {
    //         // Update Table
    //         App.Current.Dispatcher.Invoke(() => vm.UpdateState(state));
    //     }
    // }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }
    
    [RelayCommand]
    private async Task NewGame()
    {
        // Restart logic
        if (CurrentView is GameTableViewModel)
        {
             await _signalR.RestartGame();
        }
        else
        {
            // If in Lobby, just stay there or reset?
        }
    }
    
    [RelayCommand]
    private async Task SetLevel(string levelName)
    {
        if (Enum.TryParse<AILevel>(levelName, out var level))
        {
            await _signalR.SetAILevel(level);
        }
    }

    [RelayCommand]
    private void NavigateAbout()
    {
        CurrentView = _serviceProvider.GetRequiredService<AboutViewModel>();
    }

    [RelayCommand]
    private void NavigateHelp()
    {
        CurrentView = _serviceProvider.GetRequiredService<HelpViewModel>();
    }
    
    [RelayCommand]
    private void NavigateHowToPlay()
    {
         CurrentView = _serviceProvider.GetRequiredService<HowToPlayViewModel>();
    }
    
    [RelayCommand]
    private void NavigateSettings()
    {
         CurrentView = _serviceProvider.GetRequiredService<SettingsViewModel>();
    }
    
    [RelayCommand]
    private void NavigateHome()
    {
        CurrentView = _serviceProvider.GetRequiredService<LobbyViewModel>();
    }
}
