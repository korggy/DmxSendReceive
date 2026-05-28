using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class DmxFrameBuilderTests
{
    [TestMethod]
    public void BuildPacket_UsesStartCodeLeadingZeroSlotsAndConfiguredValues()
    {
        var options = new SendOptions(
            "COM8",
            StartChannel: 10,
            ChannelCount: 4,
            FixedValues: [new ChannelValue(12, 255)],
            RandomChannels: [10, 13],
            Ranges: [],
            MouseChannels: [new MouseChannelMapping(11, MouseAxis.X)],
            RandomIntervalMs: 1000,
            RefreshHz: 30,
            OutputLogMode: OutputLogMode.None);

        byte[] packet = DmxFrameBuilder.BuildPacket(
            options,
            new Dictionary<int, byte>
            {
                [10] = 7,
                [13] = 99
            },
            new Dictionary<int, byte>
            {
                [11] = 128
            });

        Assert.AreEqual(14, packet.Length);
        Assert.AreEqual(0, packet[0], "DMX start code should be 0.");
        CollectionAssert.AreEqual(new byte[9], packet[1..10], "Slots before the start channel should be zero-filled.");
        CollectionAssert.AreEqual(new byte[] { 7, 128, 255, 99 }, packet[10..14]);
    }

    [TestMethod]
    public void BuildPacket_FixedValuesOverrideRandomValuesOnSameChannel()
    {
        var options = new SendOptions(
            "COM8",
            StartChannel: 1,
            ChannelCount: 2,
            FixedValues: [new ChannelValue(2, 200)],
            RandomChannels: [2],
            Ranges: [],
            MouseChannels: [new MouseChannelMapping(2, MouseAxis.X)],
            RandomIntervalMs: 1000,
            RefreshHz: 30,
            OutputLogMode: OutputLogMode.None);

        byte[] packet = DmxFrameBuilder.BuildPacket(
            options,
            new Dictionary<int, byte>
            {
                [2] = 25
            },
            new Dictionary<int, byte>
            {
                [2] = 128
            });

        CollectionAssert.AreEqual(new byte[] { 0, 0, 200 }, packet);
    }
}
