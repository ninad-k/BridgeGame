using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace HonorBridge.Client.Wpf.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppName => "Honor Bridge";
    public string Version => "1.0.0 (Beta)";
    public string Developer => "Ninad Kulkarni";
    public string Roles => "Author, Developer, Designer";
    public string Dedication => "Building the best logic for the best game.";

    [RelayCommand]
    private void OpenLink(string url)
    {
        // Simple process start for opening links
        // Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
