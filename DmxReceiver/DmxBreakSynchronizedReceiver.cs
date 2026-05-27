namespace DmxReceiver;

public sealed class DmxBreakSynchronizedReceiver
{
    public const int MaximumSlotCount = 512;
    public static readonly TimeSpan MinimumBreakLength = TimeSpan.FromMicroseconds(88);
    public static readonly TimeSpan MinimumMarkAfterBreakLength = TimeSpan.FromMicroseconds(8);

    private readonly TimeSpan _minimumBreakLength;
    private readonly TimeSpan _minimumMarkAfterBreakLength;
    private readonly List<byte> _packet = [];
    private PendingBreak? _pendingBreak;
    private ActivePacket? _activePacket;

    public DmxBreakSynchronizedReceiver()
        : this(MinimumBreakLength, MinimumMarkAfterBreakLength)
    {
    }

    public DmxBreakSynchronizedReceiver(TimeSpan minimumBreakLength, TimeSpan minimumMarkAfterBreakLength)
    {
        if (minimumBreakLength <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumBreakLength), "Minimum break length must be greater than zero.");
        }

        if (minimumMarkAfterBreakLength <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumMarkAfterBreakLength), "Minimum mark-after-break length must be greater than zero.");
        }

        _minimumBreakLength = minimumBreakLength;
        _minimumMarkAfterBreakLength = minimumMarkAfterBreakLength;
    }

    public DmxReceiveEvent? NotifyBreak(TimeSpan observedAt, TimeSpan? breakLength, int framingErrorCount)
    {
        DmxReceiveEvent? completedPacket = CompleteActivePacket();

        _packet.Clear();
        _activePacket = null;
        _pendingBreak = new PendingBreak(
            observedAt,
            breakLength,
            Math.Max(0, framingErrorCount),
            GetBreakStatus(breakLength));

        return completedPacket;
    }

    public DmxReceiveEvent? AddByte(byte value, TimeSpan observedAt)
    {
        if (_activePacket is not null)
        {
            _packet.Add(value);

            if (_packet.Count > MaximumSlotCount)
            {
                DmxPacketDiagnostics diagnostics = CreateDiagnostics(_activePacket, discardedNoValidBreak: false);
                _activePacket = null;
                _packet.Clear();
                return new DmxPacketDiscarded("Packet exceeded 512 DMX slots.", diagnostics);
            }

            return null;
        }

        if (_pendingBreak is null)
        {
            return new DmxPacketDiscarded(
                "Discarded byte because no valid BREAK was seen.",
                new DmxPacketDiagnostics(
                    null,
                    null,
                    0,
                    0,
                    DiscardedNoValidBreak: true,
                    DmxBreakStatus.NoBreakInformation,
                    DiscardedByteCount: 1));
        }

        TimeSpan markAfterBreak = observedAt - _pendingBreak.ObservedAt;
        PendingBreak pendingBreak = _pendingBreak;
        _pendingBreak = null;

        if (pendingBreak.BreakLength is null)
        {
            if (value == 0x00 && markAfterBreak >= _minimumMarkAfterBreakLength)
            {
                _activePacket = new ActivePacket(
                    value,
                    null,
                    markAfterBreak,
                    pendingBreak.FramingErrorCount,
                    DmxBreakStatus.InferredFromUartFramingError);
                _packet.Clear();
                return null;
            }

            return new DmxPacketDiscarded(
                "Unconfirmed BREAK did not have a normal DMX start code.",
                new DmxPacketDiagnostics(
                    null,
                    markAfterBreak,
                    pendingBreak.FramingErrorCount,
                    0,
                    DiscardedNoValidBreak: false,
                    pendingBreak.BreakStatus,
                    DiscardedByteCount: 1));
        }

        if (!IsValidBreakAndMarkAfterBreak(pendingBreak.BreakLength.Value, markAfterBreak))
        {
            return new DmxPacketDiscarded(
                "Invalid BREAK/MAB sequence.",
                new DmxPacketDiagnostics(
                    pendingBreak.BreakLength,
                    markAfterBreak,
                    pendingBreak.FramingErrorCount,
                    0,
                    DiscardedNoValidBreak: false,
                    pendingBreak.BreakStatus,
                    DiscardedByteCount: 1));
        }

        _activePacket = new ActivePacket(
            value,
            pendingBreak.BreakLength,
            markAfterBreak,
            pendingBreak.FramingErrorCount,
            pendingBreak.BreakStatus);
        _packet.Clear();
        return null;
    }

    public DmxReceiveEvent? Flush()
    {
        DmxReceiveEvent? completedPacket = CompleteActivePacket();
        _activePacket = null;
        _pendingBreak = null;
        _packet.Clear();
        return completedPacket;
    }

    private bool IsValidBreakAndMarkAfterBreak(TimeSpan breakLength, TimeSpan markAfterBreak)
    {
        return breakLength >= _minimumBreakLength
            && markAfterBreak >= _minimumMarkAfterBreakLength;
    }

    private DmxBreakStatus GetBreakStatus(TimeSpan? breakLength)
    {
        if (breakLength is null)
        {
            return DmxBreakStatus.UartFramingErrorNoDuration;
        }

        return breakLength >= _minimumBreakLength
            ? DmxBreakStatus.GpioMeasuredValidBreak
            : DmxBreakStatus.GpioMeasuredShortBreak;
    }

    private DmxReceiveEvent? CompleteActivePacket()
    {
        if (_activePacket is null)
        {
            return null;
        }

        DmxPacketDiagnostics diagnostics = CreateDiagnostics(_activePacket, discardedNoValidBreak: false);
        byte[] slots = _packet.ToArray();

        if (_activePacket.StartCode == 0x00)
        {
            return new DmxFrameReceived(new DmxFrame(_activePacket.StartCode, slots), diagnostics);
        }

        return new DmxAlternateStartCodeReceived(_activePacket.StartCode, slots, diagnostics);
    }

    private DmxPacketDiagnostics CreateDiagnostics(ActivePacket activePacket, bool discardedNoValidBreak)
    {
        return new DmxPacketDiagnostics(
            activePacket.BreakLength,
            activePacket.MarkAfterBreakLength,
            activePacket.FramingErrorCount,
            _packet.Count,
            discardedNoValidBreak,
            activePacket.BreakStatus);
    }

    private sealed record PendingBreak(
        TimeSpan ObservedAt,
        TimeSpan? BreakLength,
        int FramingErrorCount,
        DmxBreakStatus BreakStatus);

    private sealed record ActivePacket(
        byte StartCode,
        TimeSpan? BreakLength,
        TimeSpan MarkAfterBreakLength,
        int FramingErrorCount,
        DmxBreakStatus BreakStatus);
}
