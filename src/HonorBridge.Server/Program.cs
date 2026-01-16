using HonorBridge.Server.Services;
using HonorBridge.Server.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, config) => 
{
    config.WriteTo.Console();
    // config.WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day);
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<LobbyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHub<BridgeHub>("/bridge");

app.Run();
