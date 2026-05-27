using System.IO.Ports;

namespace DmxSender;

public sealed class SerialDmxTransport : IDmxTransport, IDisposable
{
    private static readonly TimeSpan BreakDuration = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan MarkAfterBreakDuration = TimeSpan.FromMilliseconds(1);
    private readonly SerialPort _serialPort;

    private SerialDmxTransport(SerialPort serialPort)
    {
        _serialPort = serialPort;
    }

    public static SerialDmxTransport Open(string portName)
    {
        var serialPort = new SerialPort(portName, 250000, Parity.None, 8, StopBits.Two)
        {
            Handshake = Handshake.None,
            ReadTimeout = 100,
            WriteTimeout = 100,
            DtrEnable = false,
            RtsEnable = false
        };

        serialPort.Open();
        return new SerialDmxTransport(serialPort);
    }

    public async Task SendPacketAsync(byte[] packet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        _serialPort.BreakState = true;
        try
        {
            await Task.Delay(BreakDuration, cancellationToken);
        }
        finally
        {
            _serialPort.BreakState = false;
        }

        await Task.Delay(MarkAfterBreakDuration, cancellationToken);
        _serialPort.Write(packet, 0, packet.Length);
    }

    public void Dispose()
    {
        _serialPort.Dispose();
    }
}
