using DmxReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxReceiver.Tests;

[TestClass]
public sealed class DmxBreakSynchronizedReceiverTests
{
    [TestMethod]
    public void AddByte_DiscardsBytesBeforeValidBreak()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        DmxReceiveEvent? receiveEvent = receiver.AddByte(0xFF, TimeSpan.Zero);

        var discarded = receiveEvent as DmxPacketDiscarded;
        Assert.IsNotNull(discarded);
        Assert.IsTrue(discarded.Diagnostics.DiscardedNoValidBreak);
    }

    [TestMethod]
    public void AddByte_ParsesNormalFrameOnlyAfterBreakAndMarkAfterBreak()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        Assert.IsNull(receiver.NotifyBreak(TimeSpan.Zero, TimeSpan.FromMicroseconds(92), framingErrorCount: 1));
        Assert.IsNull(receiver.AddByte(0x00, TimeSpan.FromMicroseconds(104)));
        Assert.IsNull(receiver.AddByte(0x10, TimeSpan.FromMicroseconds(148)));
        Assert.IsNull(receiver.AddByte(0x20, TimeSpan.FromMicroseconds(192)));

        DmxReceiveEvent? receiveEvent = receiver.NotifyBreak(TimeSpan.FromMicroseconds(250), TimeSpan.FromMicroseconds(96), framingErrorCount: 1);

        var frameReceived = receiveEvent as DmxFrameReceived;
        Assert.IsNotNull(frameReceived);
        Assert.AreEqual(0x00, frameReceived.Frame.StartCode);
        CollectionAssert.AreEqual(new byte[] { 0x10, 0x20 }, frameReceived.Frame.Slots.ToArray());
        Assert.AreEqual(TimeSpan.FromMicroseconds(92), frameReceived.Diagnostics.BreakLength);
        Assert.AreEqual(TimeSpan.FromMicroseconds(104), frameReceived.Diagnostics.MarkAfterBreakLength);
        Assert.AreEqual(1, frameReceived.Diagnostics.FramingErrorCount);
        Assert.AreEqual(2, frameReceived.Diagnostics.SlotsReceivedBeforeNextBreak);
        Assert.IsFalse(frameReceived.Diagnostics.DiscardedNoValidBreak);
    }

    [TestMethod]
    public void AddByte_DiscardsPacketWhenBreakLengthIsUnknown()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        receiver.NotifyBreak(TimeSpan.Zero, breakLength: null, framingErrorCount: 1);

        DmxReceiveEvent? receiveEvent = receiver.AddByte(0xFF, TimeSpan.FromMicroseconds(104));

        var discarded = receiveEvent as DmxPacketDiscarded;
        Assert.IsNotNull(discarded);
        Assert.AreEqual("Unconfirmed BREAK did not have a normal DMX start code.", discarded.Reason);
        Assert.IsNull(discarded.Diagnostics.BreakLength);
        Assert.AreEqual(DmxBreakStatus.UartFramingErrorNoDuration, discarded.Diagnostics.BreakStatus);
        Assert.AreEqual(1, discarded.Diagnostics.FramingErrorCount);
    }

    [TestMethod]
    public void NotifyBreak_EmitsInferredFrameAfterUnknownBreakWithNormalStartCode()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        receiver.NotifyBreak(TimeSpan.Zero, breakLength: null, framingErrorCount: 1);
        receiver.AddByte(0x00, TimeSpan.FromMicroseconds(104));
        receiver.AddByte(0x10, TimeSpan.FromMicroseconds(148));

        DmxReceiveEvent? receiveEvent = receiver.NotifyBreak(TimeSpan.FromMicroseconds(250), TimeSpan.FromMicroseconds(96), framingErrorCount: 1);

        var frame = receiveEvent as DmxFrameReceived;
        Assert.IsNotNull(frame);
        Assert.IsTrue(frame.Diagnostics.BreakWasInferred);
        Assert.AreEqual(DmxBreakStatus.InferredFromUartFramingError, frame.Diagnostics.BreakStatus);
        Assert.AreEqual(1, frame.Frame.ChannelCount);
    }

    [TestMethod]
    public void AddByte_DiscardsBreakWhenMarkAfterBreakIsTooShort()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        receiver.NotifyBreak(TimeSpan.Zero, TimeSpan.FromMicroseconds(92), framingErrorCount: 1);

        DmxReceiveEvent? receiveEvent = receiver.AddByte(0x00, TimeSpan.FromMicroseconds(4));

        var discarded = receiveEvent as DmxPacketDiscarded;
        Assert.IsNotNull(discarded);
        Assert.AreEqual("Invalid BREAK/MAB sequence.", discarded.Reason);
        Assert.AreEqual(TimeSpan.FromMicroseconds(4), discarded.Diagnostics.MarkAfterBreakLength);
    }

    [TestMethod]
    public void NotifyBreak_ReportsAlternateStartCodeSeparately()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        receiver.NotifyBreak(TimeSpan.Zero, TimeSpan.FromMicroseconds(92), framingErrorCount: 1);
        receiver.AddByte(0xFF, TimeSpan.FromMicroseconds(104));
        receiver.AddByte(0x00, TimeSpan.FromMicroseconds(148));
        receiver.AddByte(0x7F, TimeSpan.FromMicroseconds(192));

        DmxReceiveEvent? receiveEvent = receiver.NotifyBreak(TimeSpan.FromMicroseconds(250), TimeSpan.FromMicroseconds(96), framingErrorCount: 1);

        var alternate = receiveEvent as DmxAlternateStartCodeReceived;
        Assert.IsNotNull(alternate);
        Assert.AreEqual(0xFF, alternate.StartCode);
        Assert.AreEqual(DmxBreakStatus.GpioMeasuredValidBreak, alternate.Diagnostics.BreakStatus);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x7F }, alternate.Payload.ToArray());
        Assert.AreEqual(2, alternate.Diagnostics.SlotsReceivedBeforeNextBreak);
    }

    [TestMethod]
    public void AddByte_DiscardsMeasuredShortBreak()
    {
        var receiver = new DmxBreakSynchronizedReceiver();

        receiver.NotifyBreak(TimeSpan.Zero, TimeSpan.FromMicroseconds(40), framingErrorCount: 1);

        DmxReceiveEvent? receiveEvent = receiver.AddByte(0x00, TimeSpan.FromMicroseconds(104));

        var discarded = receiveEvent as DmxPacketDiscarded;
        Assert.IsNotNull(discarded);
        Assert.AreEqual(DmxBreakStatus.GpioMeasuredShortBreak, discarded.Diagnostics.BreakStatus);
    }
}
