namespace DmxSender;

public sealed record SendOptions(
    string PortName,
    int StartChannel,
    int ChannelCount,
    IReadOnlyList<ChannelValue> FixedValues,
    IReadOnlyList<int> RandomChannels,
    IReadOnlyList<ChannelRange> Ranges,
    IReadOnlyList<MouseChannelMapping> MouseChannels,
    int RandomIntervalMs,
    int RefreshHz,
    OutputLogMode OutputLogMode);
