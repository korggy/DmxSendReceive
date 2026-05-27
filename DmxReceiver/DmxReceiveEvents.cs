namespace DmxReceiver;

public enum DmxBreakStatus
{
    NoBreakInformation,
    UartFramingErrorNoDuration,
    UartExplicitBreak,
    GpioMeasuredValidBreak,
    GpioMeasuredShortBreak,
    InferredFromUartFramingError
}

public sealed record DmxPacketDiagnostics(
    TimeSpan? BreakLength,
    TimeSpan? MarkAfterBreakLength,
    int FramingErrorCount,
    int SlotsReceivedBeforeNextBreak,
    bool DiscardedNoValidBreak,
    DmxBreakStatus BreakStatus = DmxBreakStatus.NoBreakInformation,
    int DiscardedByteCount = 0)
{
    public bool BreakWasInferred => BreakStatus == DmxBreakStatus.InferredFromUartFramingError;
}

public abstract record DmxReceiveEvent(DmxPacketDiagnostics Diagnostics);

public sealed record DmxFrameReceived(DmxFrame Frame, DmxPacketDiagnostics Diagnostics)
    : DmxReceiveEvent(Diagnostics);

public sealed record DmxAlternateStartCodeReceived(
    int StartCode,
    IReadOnlyList<byte> Payload,
    DmxPacketDiagnostics Diagnostics)
    : DmxReceiveEvent(Diagnostics);

public sealed record DmxPacketDiscarded(string Reason, DmxPacketDiagnostics Diagnostics)
    : DmxReceiveEvent(Diagnostics);
