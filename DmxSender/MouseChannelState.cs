namespace DmxSender;

public sealed class MouseChannelState
{
    private readonly MouseChannelMapping[] _channels;
    private readonly IReadOnlyDictionary<int, RandomValueRange> _ranges;
    private readonly IMousePositionSource _mousePositionSource;

    private MouseChannelState(
        MouseChannelMapping[] channels,
        IReadOnlyDictionary<int, RandomValueRange> ranges,
        IMousePositionSource mousePositionSource)
    {
        _channels = channels;
        _ranges = ranges;
        _mousePositionSource = mousePositionSource;
    }

    public static MouseChannelState Create(
        IEnumerable<MouseChannelMapping> channels,
        IEnumerable<ChannelRange> ranges,
        IMousePositionSource mousePositionSource)
    {
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(ranges);
        ArgumentNullException.ThrowIfNull(mousePositionSource);

        Dictionary<int, RandomValueRange> rangeByChannel = ranges.ToDictionary(range => range.Channel, range => range.Range);
        return new MouseChannelState(channels.ToArray(), rangeByChannel, mousePositionSource);
    }

    public IReadOnlyDictionary<int, byte> GetValues()
    {
        MousePosition position = _mousePositionSource.GetPosition();
        ScreenBounds bounds = _mousePositionSource.GetScreenBounds();
        var values = new Dictionary<int, byte>();

        foreach (MouseChannelMapping channel in _channels)
        {
            byte fullRangeValue = channel.Axis switch
            {
                MouseAxis.X => MapCoordinate(position.X, bounds.Left, bounds.Right, invert: false),
                MouseAxis.XInverted => MapCoordinate(position.X, bounds.Left, bounds.Right, invert: true),
                MouseAxis.Y => MapCoordinate(position.Y, bounds.Top, bounds.Bottom, invert: true),
                MouseAxis.YInverted => MapCoordinate(position.Y, bounds.Top, bounds.Bottom, invert: false),
                _ => throw new InvalidOperationException($"Unsupported mouse axis {channel.Axis}.")
            };
            values[channel.Channel] = ApplyRange(fullRangeValue, _ranges.GetValueOrDefault(channel.Channel, RandomValueRange.Full));
        }

        return values;
    }

    private static byte MapCoordinate(int value, int min, int max, bool invert)
    {
        if (max <= min)
        {
            return 0;
        }

        int clamped = Math.Clamp(value, min, max);
        double ratio = (double)(clamped - min) / (max - min);
        if (invert)
        {
            ratio = 1.0 - ratio;
        }

        return (byte)Math.Round(ratio * byte.MaxValue, MidpointRounding.AwayFromZero);
    }

    private static byte ApplyRange(byte value, RandomValueRange range)
    {
        double ratio = (double)value / byte.MaxValue;
        return (byte)Math.Round(range.Min + (ratio * (range.Max - range.Min)), MidpointRounding.AwayFromZero);
    }
}
