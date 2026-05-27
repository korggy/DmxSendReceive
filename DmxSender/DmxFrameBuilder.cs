namespace DmxSender;

public static class DmxFrameBuilder
{
    public const int MaxDmxChannel = 512;

    public static byte[] BuildPacket(SendOptions options, IReadOnlyDictionary<int, byte> randomValues)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(randomValues);

        int endChannel = options.StartChannel + options.ChannelCount - 1;
        var packet = new byte[endChannel + 1];

        foreach (int channel in options.RandomChannels)
        {
            if (randomValues.TryGetValue(channel, out byte value))
            {
                packet[channel] = value;
            }
        }

        foreach (ChannelValue fixedValue in options.FixedValues)
        {
            packet[fixedValue.Channel] = fixedValue.Value;
        }

        return packet;
    }
}
