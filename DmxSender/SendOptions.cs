namespace DmxSender;

public sealed record SendOptions(
    string PortName,
    int StartChannel,
    int ChannelCount,
    IReadOnlyList<ChannelValue> FixedValues,
    IReadOnlyList<int> RandomChannels,
    IReadOnlyList<RandomChannelRange> RandomRanges,
    RandomUpdateMode RandomUpdateMode,
    int RandomIntervalMs,
    int RefreshHz,
    OutputLogMode OutputLogMode);
