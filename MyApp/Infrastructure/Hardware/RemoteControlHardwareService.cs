using MyApp.Domain.Events;
using MyApp.Domain.RemoteControl;
using MyApp.Infrastructure.Industrial.Modbus.Adapters;

namespace MyApp.Infrastructure.Hardware;

public class RemoteControlHardwareService : BackgroundService
{
    private readonly RemoteControlModbusAdapter _adapter;
    private RemoteControlState? _lastState;
    private RemoteControlState? _currentState;

    // Events
    public event Action<RemoteControlUpdatedEvent>? OnStateChanged;

    public RemoteControlState? CurrentState => _currentState;

    public RemoteControlHardwareService(string portName = "COM8", byte slaveId = 0x01)
    {
        _adapter = new RemoteControlModbusAdapter(portName, slaveId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[RemoteControlService] Started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var state = _adapter.ReadState();

                if (state != null)
                {
                    _currentState = state;

                    if (!state.Equals(_lastState))
                    {
                        // State changed - raise domain event
                        var domainEvent = RemoteControlUpdatedEvent.FromState(state);
                        OnStateChanged?.Invoke(domainEvent);

                        _lastState = state;

                        //Console.WriteLine($"[RemoteControlService] Updated: {state.Action}");
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[RemoteControlService] Error: {ex.Message}");
            }

            await Task.Delay(200, stoppingToken); // Poll every 200ms
        }

        Console.WriteLine("[RemoteControlService] Stopped");
    }

    public override void Dispose()
    {
        _adapter?.Dispose();
        base.Dispose();
    }
}