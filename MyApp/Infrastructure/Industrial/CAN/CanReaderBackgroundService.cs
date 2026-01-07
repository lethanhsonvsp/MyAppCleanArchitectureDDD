using Microsoft.Extensions.Hosting;

namespace MyApp.Infrastructure.Industrial.CAN;

/// <summary>
/// Background service to continuously read CAN frames from SocketCAN
/// Only runs on Linux with CAN interface available
/// </summary>
public class CanReaderBackgroundService : BackgroundService
{
    private readonly SocketCan _can;

    public CanReaderBackgroundService(SocketCan can)
    {
        _can = can;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            if (!OperatingSystem.IsLinux())
            {
                Console.WriteLine("⚠️  Not running on Linux - CAN reader disabled");
                return;
            }

            if (!_can.IsConnected)
            {
                Console.WriteLine("⚠️  CAN interface not connected - reader disabled");
                return;
            }

            Console.WriteLine("✅ CAN reader started");
            _can.StartReading(stoppingToken);
        }, stoppingToken);
    }
}