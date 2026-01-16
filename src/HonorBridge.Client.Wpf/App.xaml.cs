using System.Windows;
using HonorBridge.Client.Wpf.Services;
using HonorBridge.Client.Wpf.ViewModels;
using HonorBridge.Client.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HonorBridge.Client.Wpf;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<SignalRClientService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LobbyViewModel>();
        services.AddTransient<GameTableViewModel>();
        services.AddTransient<HelpViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<HowToPlayViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        // Views (if needed for NavigationService, but DataTemplates in MainWindow normally handle VM->View)

        return services.BuildServiceProvider();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure Serilog
        Serilog.Log.Logger = new Serilog.LoggerConfiguration()
            .WriteTo.Console()
            // .WriteTo.File("logs/client.log", rollingInterval: Serilog.RollingInterval.Day)
            .CreateLogger();
            
        Serilog.Log.Information("Honor Bridge Client Starting...");
        
        var mainVm = Services.GetRequiredService<MainViewModel>();
        var win = new MainWindow
        {
            DataContext = mainVm
        };
        win.Show();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Serilog.Log.CloseAndFlush();
        base.OnExit(e);
    }
}
