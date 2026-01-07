
using MyApp.Domain.RemoteControl;

namespace MyApp.Infrastructure.Industrial.Modbus.Adapters;

public class RemoteControlModbusAdapter : IDisposable
{
    private readonly ModbusRtuClient _modbusClient;
    private readonly byte _slaveId;

    public RemoteControlModbusAdapter(string portName, byte slaveId = 0x01)
    {
        _modbusClient = new ModbusRtuClient(portName);
        _slaveId = slaveId;
    }

    /// <summary>
    /// Read remote control state from Modbus
    /// </summary>
    public RemoteControlState? ReadState()
    {
        try
        {
            // Read 4 registers starting from 0x0001
            var regs = _modbusClient.ReadHoldingRegisters(_slaveId, 0x0001, 4);
            var bytes = ToBytes(regs);

            // Create domain state and update from raw data
            var state = new RemoteControlState();
            state.UpdateFromModbus(bytes);

            return state;
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"[RemoteControlAdapter] Read failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Write command to remote control (if needed)
    /// </summary>
    public bool WriteCommand(ushort address, ushort[] values)
    {
        try
        {
            _modbusClient.WriteMultipleRegisters(_slaveId, address, values);
            return true;
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"[RemoteControlAdapter] Write failed: {ex.Message}");
            return false;
        }
    }

    public bool IsConnected => _modbusClient.IsConnected;

    public void ForceReconnect()
    {
        _modbusClient.ForceReconnect();
    }

    private static byte[] ToBytes(ushort[] regs)
    {
        if (regs.Length < 4) throw new ArgumentException("Need 4 registers");

        return new[]
        {
            (byte)(regs[0] >> 8), (byte)regs[0],
            (byte)(regs[1] >> 8), (byte)regs[1],
            (byte)(regs[2] >> 8), (byte)regs[2],
            (byte)(regs[3] >> 8), (byte)regs[3],
        };
    }

    public void Dispose()
    {
        _modbusClient?.Dispose();
    }
}