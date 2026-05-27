namespace DmxSender;

public sealed record RandomValueRange(byte Min, byte Max)
{
    public static RandomValueRange Full { get; } = new(0, 255);
}
