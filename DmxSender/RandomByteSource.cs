namespace DmxSender;

public sealed class RandomByteSource : IRandomByteSource
{
    public byte NextByte(RandomValueRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        return (byte)Random.Shared.Next(range.Min, range.Max + 1);
    }
}
