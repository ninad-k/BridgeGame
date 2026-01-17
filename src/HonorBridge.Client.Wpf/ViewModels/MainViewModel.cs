using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HonorBridge.Client.Wpf.Services;
using Microsoft.Extensions.DependencyInjection; // For navigating to other VMs
using System;

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
    
    private void OnStateUpdated(HonorBridge.Server.Models.GameStateDto state)
    {
        // Auto-navigate to Table if joined a room
        if (CurrentView is LobbyViewModel && !string.IsNullOrEmpty(state.RoomId))
        {
            // Switch to GameTable
             App.Current.Dispatcher.Invoke(() => 
             {
                 var vm = _serviceProvider.GetRequiredService<GameTableViewModel>();
                 vm.UpdateState(state);
                 CurrentView = vm;
             });
        }
        else if (CurrentView is GameTableViewModel vm)
        {
            // Update Table
            App.Current.Dispatcher.Invoke(() => vm.UpdateState(state));
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
