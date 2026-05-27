namespace DmxSender;

public interface IDmxTransport
{
    Task SendPacketAsync(byte[] packet, CancellationToken cancellationToken);
}
