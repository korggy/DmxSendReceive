using System.Text;

namespace DmxReceiver;

public static class DmxFrameFormatter
{
    public static string Format(DmxReceiveEvent receiveEvent, int maxChannelsToPrint, bool includeRawPacket)
    {
        return receiveEvent switch
        {
            DmxFrameReceived frameReceived => Format(frameReceived.Frame, frameReceived.Diagnostics, maxChannelsToPrint, includeRawPacket),
            DmxAlternateStartCodeReceived alternate => FormatAlternateStartCode(alternate, includeRawPacket),
            DmxPacketDiscarded discarded => FormatDiscardedPacket(discarded),
            _ => throw new ArgumentOutOfRangeException(nameof(receiveEvent), "Unknown DMX receive event.")
        };
    }

    public static string Format(DmxFrame frame, int maxChannelsToPrint, bool includeRawPacket)
    {
        return Format(frame, null, maxChannelsToPrint, includeRawPacket);
    }

    private static string Format(DmxFrame frame, DmxPacketDiagnostics? diagnostics, int maxChannelsToPrint, bool includeRawPacket)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxChannelsToPrint);

        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("HH:mm:ss.fff"));
        builder.Append(" start=0x");
        builder.Append(frame.StartCode.ToString("X2"));
        builder.Append(" channels=");
        builder.Append(frame.ChannelCount);
        AppendDiagnostics(builder, diagnostics);

        if (maxChannelsToPrint > 0 && frame.ChannelCount > 0)
        {
            int count = Math.Min(maxChannelsToPrint, frame.ChannelCount);
            builder.Append(" values=");

            for (var index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(index + 1);
                builder.Append(':');
                builder.Append(frame.Slots[index].ToString("D3"));
            }

            if (count < frame.ChannelCount)
            {
                builder.Append(" ...");
            }
        }

        if (includeRawPacket)
        {
            builder.Append(" raw=");
            builder.Append(frame.StartCode.ToString("X2"));

            foreach (byte value in frame.Slots)
            {
                builder.Append(' ');
                builder.Append(value.ToString("X2"));
            }
        }

        return builder.ToString();
    }

    private static string FormatAlternateStartCode(DmxAlternateStartCodeReceived alternate, bool includeRawPacket)
    {
        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("HH:mm:ss.fff"));
        builder.Append(" unusual-start=0x");
        builder.Append(alternate.StartCode.ToString("X2"));
        builder.Append(" payload-bytes=");
        builder.Append(alternate.Payload.Count);
        AppendDiagnostics(builder, alternate.Diagnostics);

        if (includeRawPacket)
        {
            builder.Append(" raw=");
            builder.Append(alternate.StartCode.ToString("X2"));

            foreach (byte value in alternate.Payload)
            {
                builder.Append(' ');
                builder.Append(value.ToString("X2"));
            }
        }

        return builder.ToString();
    }

    private static string FormatDiscardedPacket(DmxPacketDiscarded discarded)
    {
        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("HH:mm:ss.fff"));
        builder.Append(" discarded=\"");
        builder.Append(discarded.Reason);
        builder.Append('"');
        AppendDiagnostics(builder, discarded.Diagnostics);
        return builder.ToString();
    }

    private static void AppendDiagnostics(StringBuilder builder, DmxPacketDiagnostics? diagnostics)
    {
        if (diagnostics is null)
        {
            return;
        }

        builder.Append(" break=");
        builder.Append(diagnostics.BreakWasInferred ? "inferred" : FormatDuration(diagnostics.BreakLength));
        builder.Append(" break-status=");
        builder.Append(diagnostics.FormatBreakStatus());
        builder.Append(" mab=");
        builder.Append(FormatDuration(diagnostics.MarkAfterBreakLength));
        builder.Append(" framing-errors=");
        builder.Append(diagnostics.FramingErrorCount);
        builder.Append(" slots-before-break=");
        builder.Append(diagnostics.SlotsReceivedBeforeNextBreak);
        builder.Append(" discarded-no-break=");
        builder.Append(diagnostics.DiscardedNoValidBreak ? "yes" : "no");
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        return duration is null
            ? "unknown"
            : $"{duration.Value.TotalMicroseconds:0.###}us";
    }
}
