using DmxReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxReceiver.Tests;

[TestClass]
public sealed class DmxReceiveSummaryTests
{
    [TestMethod]
    public void Record_TracksReceiveCounts()
    {
        var summary = new DmxReceiveSummary();

        summary.Record(new DmxFrameReceived(
            new DmxFrame(0x00, new byte[] { 1, 2, 3 }),
            new DmxPacketDiagnostics(TimeSpan.FromMicroseconds(92), TimeSpan.FromMicroseconds(12), 2, 3, false, DmxBreakStatus.GpioMeasuredValidBreak, DiscardedByteCount: 0)));
        summary.Record(new DmxFrameReceived(
            new DmxFrame(0x00, new byte[] { 1 }),
            new DmxPacketDiagnostics(null, TimeSpan.FromMicroseconds(10), 1, 1, false, DmxBreakStatus.InferredFromUartFramingError, DiscardedByteCount: 0)));
        summary.Record(new DmxAlternateStartCodeReceived(
            0xCC,
            new byte[] { 1 },
            new DmxPacketDiagnostics(TimeSpan.FromMicroseconds(92), TimeSpan.FromMicroseconds(12), 1, 1, false, DmxBreakStatus.GpioMeasuredValidBreak, DiscardedByteCount: 0)));
        summary.Record(new DmxPacketDiscarded(
            "Discarded byte because no valid BREAK was seen.",
            new DmxPacketDiagnostics(null, null, 0, 0, true, DmxBreakStatus.NoBreakInformation, DiscardedByteCount: 1)));
        summary.Record(new DmxPacketDiscarded(
            "BREAK length was not confirmed.",
            new DmxPacketDiagnostics(null, TimeSpan.FromMicroseconds(12), 1, 0, false, DmxBreakStatus.UartFramingErrorNoDuration, DiscardedByteCount: 1)));

        Assert.AreEqual(1, summary.ValidDmxFrames);
        Assert.AreEqual(1, summary.InferredDmxFrames);
        Assert.AreEqual(2, summary.DiscardedBytes);
        Assert.AreEqual(1, summary.DiscardedNoBreakPackets);
        Assert.AreEqual(1, summary.BreakUnknownPackets);
        Assert.AreEqual(1, summary.AlternateStartPackets);
        Assert.AreEqual(5, summary.FramingErrors);
        Assert.AreEqual(TimeSpan.FromMicroseconds(10), summary.MinimumMarkAfterBreak);
        Assert.AreEqual(TimeSpan.FromMicroseconds(12), summary.MaximumMarkAfterBreak);
    }

    [TestMethod]
    public void RecordFramingErrors_AddsErrorsWithoutPacketEvent()
    {
        var summary = new DmxReceiveSummary();

        summary.RecordFramingErrors(3);

        Assert.AreEqual(3, summary.FramingErrors);
    }

    [TestMethod]
    public void Format_PrintsSummaryCounts()
    {
        var summary = new DmxReceiveSummary(
            ValidDmxFrames: 3,
            InferredDmxFrames: 12,
            DiscardedBytes: 11,
            DiscardedNoBreakPackets: 7,
            BreakUnknownPackets: 53,
            AlternateStartPackets: 2,
            FramingErrors: 5,
            MinimumMarkAfterBreak: TimeSpan.FromMicroseconds(9),
            MaximumMarkAfterBreak: TimeSpan.FromMicroseconds(22));

        string formatted = summary.Format();

        StringAssert.Contains(formatted, "valid=3");
        StringAssert.Contains(formatted, "inferred=12");
        StringAssert.Contains(formatted, "discarded_bytes=11");
        StringAssert.Contains(formatted, "discarded_no_break=7");
        StringAssert.Contains(formatted, "break_unknown=53");
        StringAssert.Contains(formatted, "alternate_start=2");
        StringAssert.Contains(formatted, "framing_errors=5");
        StringAssert.Contains(formatted, "min_mab=9us");
        StringAssert.Contains(formatted, "max_mab=22us");
    }
}
