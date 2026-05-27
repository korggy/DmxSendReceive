namespace DmxReceiver;

public sealed record DmxFrame(int StartCode, IReadOnlyList<byte> Slots)
{
    public int ChannelCount => Slots.Count;
}
