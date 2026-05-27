namespace DmxReceiver;

public sealed record MonitorOptions(
    string PortName,
    int MaxChannelsToPrint,
    bool PrintRawPacket)
{
    public const int DefaultMaxChannelsToPrint = 32;
}
