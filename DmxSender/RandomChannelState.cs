namespace DmxSender;

public sealed class RandomChannelState
{
    private readonly int[] _channels;
    private readonly IReadOnlyDictionary<int, RandomValueRange> _ranges;
    private readonly IRandomByteSource _randomByteSource;
    private readonly Dictionary<int, byte> _values;

    private RandomChannelState(
        int[] channels,
        IReadOnlyDictionary<int, RandomValueRange> ranges,
        IRandomByteSource randomByteSource)
    {
        _channels = channels;
        _ranges = ranges;
        _randomByteSource = randomByteSource;
        _values = [];
    }

    public IReadOnlyDictionary<int, byte> Values => _values;

    public static RandomChannelState Create(
        IEnumerable<int> channels,
        IEnumerable<RandomChannelRange> ranges,
        IRandomByteSource randomByteSource)
    {
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(ranges);
        ArgumentNullException.ThrowIfNull(randomByteSource);

        Dictionary<int, RandomValueRange> rangeByChannel = ranges.ToDictionary(range => range.Channel, range => range.Range);
        var state = new RandomChannelState(channels.Distinct().Order().ToArray(), rangeByChannel, randomByteSource);
        state.Refresh();
        return state;
    }

    public void Refresh()
    {
        foreach (int channel in _channels)
        {
            RandomValueRange range = _ranges.GetValueOrDefault(channel, RandomValueRange.Full);
            _values[channel] = _randomByteSource.NextByte(range);
        }
    }
}
