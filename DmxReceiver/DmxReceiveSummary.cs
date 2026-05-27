namespace DmxReceiver;

public sealed class DmxReceiveSummary
{
    public DmxReceiveSummary(
        int ValidDmxFrames = 0,
        int InferredDmxFrames = 0,
        int DiscardedBytes = 0,
        int DiscardedNoBreakPackets = 0,
        int BreakUnknownPackets = 0,
        int AlternateStartPackets = 0,
        int FramingErrors = 0,
        TimeSpan? MinimumMarkAfterBreak = null,
        TimeSpan? MaximumMarkAfterBreak = null)
    {
        this.ValidDmxFrames = ValidDmxFrames;
        this.InferredDmxFrames = InferredDmxFrames;
        this.DiscardedBytes = DiscardedBytes;
        this.DiscardedNoBreakPackets = DiscardedNoBreakPackets;
        this.BreakUnknownPackets = BreakUnknownPackets;
        this.AlternateStartPackets = AlternateStartPackets;
        this.FramingErrors = FramingErrors;
        this.MinimumMarkAfterBreak = MinimumMarkAfterBreak;
        this.MaximumMarkAfterBreak = MaximumMarkAfterBreak;
    }

    public int ValidDmxFrames { get; private set; }

    public int InferredDmxFrames { get; private set; }

    public int DiscardedBytes { get; private set; }

    public int DiscardedNoBreakPackets { get; private set; }

    public int BreakUnknownPackets { get; private set; }

    public int AlternateStartPackets { get; private set; }

    public int FramingErrors { get; private set; }

    public TimeSpan? MinimumMarkAfterBreak { get; private set; }

    public TimeSpan? MaximumMarkAfterBreak { get; private set; }

    public void Record(DmxReceiveEvent receiveEvent)
    {
        ArgumentNullException.ThrowIfNull(receiveEvent);

        DiscardedBytes += receiveEvent.Diagnostics.DiscardedByteCount;
        FramingErrors += receiveEvent.Diagnostics.FramingErrorCount;
        RecordMarkAfterBreak(receiveEvent.Diagnostics.MarkAfterBreakLength);

        if (receiveEvent.Diagnostics.BreakStatus == DmxBreakStatus.UartFramingErrorNoDuration)
        {
            BreakUnknownPackets++;
        }

        switch (receiveEvent)
        {
            case DmxFrameReceived when receiveEvent.Diagnostics.BreakWasInferred:
                InferredDmxFrames++;
                break;

            case DmxFrameReceived:
                ValidDmxFrames++;
                break;

            case DmxAlternateStartCodeReceived:
                AlternateStartPackets++;
                break;

            case DmxPacketDiscarded { Diagnostics.DiscardedNoValidBreak: true }:
                DiscardedNoBreakPackets++;
                break;
        }
    }

    public void RecordFramingErrors(int count)
    {
        if (count > 0)
        {
            FramingErrors += count;
        }
    }

    public string Format()
    {
        return $"valid={ValidDmxFrames} "
            + $"inferred={InferredDmxFrames} "
            + $"discarded_bytes={DiscardedBytes} "
            + $"discarded_no_break={DiscardedNoBreakPackets} "
            + $"break_unknown={BreakUnknownPackets} "
            + $"alternate_start={AlternateStartPackets} "
            + $"framing_errors={FramingErrors} "
            + $"min_mab={FormatDuration(MinimumMarkAfterBreak)} "
            + $"max_mab={FormatDuration(MaximumMarkAfterBreak)}";
    }

    private void RecordMarkAfterBreak(TimeSpan? markAfterBreak)
    {
        if (markAfterBreak is null)
        {
            return;
        }

        MinimumMarkAfterBreak = MinimumMarkAfterBreak is null || markAfterBreak < MinimumMarkAfterBreak
            ? markAfterBreak
            : MinimumMarkAfterBreak;
        MaximumMarkAfterBreak = MaximumMarkAfterBreak is null || markAfterBreak > MaximumMarkAfterBreak
            ? markAfterBreak
            : MaximumMarkAfterBreak;
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        return duration is null
            ? "n/a"
            : $"{duration.Value.TotalMicroseconds:0.###}us";
    }
}
