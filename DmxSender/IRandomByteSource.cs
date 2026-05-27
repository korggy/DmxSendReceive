namespace DmxSender;

public interface IRandomByteSource
{
    byte NextByte(RandomValueRange range);
}
