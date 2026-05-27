namespace DmxSender;

public static class DmxOutputLogFormatter
{
    public static string FormatFrame(long frameNumber, SendOptions options, byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(packet);

        int endChannel = options.StartChannel + options.ChannelCount - 1;
        IEnumerable<string> values = Enumerable
            .Range(options.StartChannel, options.ChannelCount)
            .Select(channel => $"{channel}={GetChannelValue(packet, channel)}");

        return $"frame={frameNumber} channels {options.StartChannel}-{endChannel}: {string.Join(' ', values)}";
    }

    private static byte GetChannelValue(byte[] packet, int channel)
    {
        return channel < packet.Length ? packet[channel] : (byte)0;
    }
}
