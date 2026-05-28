namespace DmxSender;

public static class DmxFrameBuilder
{
    public const int MaxDmxChannel = 512;

    public static byte[] BuildPacket(
        SendOptions options,
        IReadOnlyDictionary<int, byte> randomValues,
        IReadOnlyDictionary<int, byte> mouseValues)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(randomValues);
        ArgumentNullException.ThrowIfNull(mouseValues);

        int endChannel = options.StartChannel + options.ChannelCount - 1;
        var packet = new byte[endChannel + 1];

        foreach (int channel in options.RandomChannels)
        {
            if (randomValues.TryGetValue(channel, out byte value))
            {
                packet[channel] = value;
            }
        }

        foreach (MouseChannelMapping channel in options.MouseChannels)
        {
            if (mouseValues.TryGetValue(channel.Channel, out byte value))
            {
                packet[channel.Channel] = value;
            }
        }

        foreach (ChannelValue fixedValue in options.FixedValues)
        {
            packet[fixedValue.Channel] = fixedValue.Value;
        }

        return packet;
    }
}
