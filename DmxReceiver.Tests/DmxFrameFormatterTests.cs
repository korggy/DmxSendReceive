using DmxReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxReceiver.Tests;

[TestClass]
public sealed class DmxFrameFormatterTests
{
    [TestMethod]
    public void Format_IncludesReceiveDiagnostics()
    {
        var receiveEvent = new DmxFrameReceived(
            new DmxFrame(0x00, new byte[] { 0x10, 0x20 }),
            new DmxPacketDiagnostics(
                TimeSpan.FromMicroseconds(92),
                TimeSpan.FromMicroseconds(12),
                FramingErrorCount: 2,
                SlotsReceivedBeforeNextBreak: 2,
                DiscardedNoValidBreak: false,
                BreakStatus: DmxBreakStatus.GpioMeasuredValidBreak));

        string formatted = DmxFrameFormatter.Format(receiveEvent, maxChannelsToPrint: 2, includeRawPacket: false);

        StringAssert.Contains(formatted, "break=92us");
        StringAssert.Contains(formatted, "mab=12us");
        StringAssert.Contains(formatted, "framing-errors=2");
        StringAssert.Contains(formatted, "slots-before-break=2");
        StringAssert.Contains(formatted, "discarded-no-break=no");
        StringAssert.Contains(formatted, "break-status=gpio-valid");
    }

    [TestMethod]
    public void Format_LabelsAlternateStartCodeAsUnusual()
    {
        var receiveEvent = new DmxAlternateStartCodeReceived(
            0xFF,
            new byte[] { 0x00, 0x7F },
            new DmxPacketDiagnostics(TimeSpan.FromMicroseconds(92), TimeSpan.FromMicroseconds(12), 1, 2, false, DmxBreakStatus.GpioMeasuredValidBreak));

        string formatted = DmxFrameFormatter.Format(receiveEvent, maxChannelsToPrint: 2, includeRawPacket: true);

        StringAssert.Contains(formatted, "unusual-start=0xFF");
        StringAssert.Contains(formatted, "payload-bytes=2");
        StringAssert.Contains(formatted, "raw=FF 00 7F");
    }
}
