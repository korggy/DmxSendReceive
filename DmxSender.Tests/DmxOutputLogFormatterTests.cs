using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class DmxOutputLogFormatterTests
{
    [TestMethod]
    public void FormatFrame_PrintsFrameNumberConfiguredRangeAndChannelValues()
    {
        var options = new SendOptions(
            "COM8",
            StartChannel: 5,
            ChannelCount: 3,
            FixedValues: [],
            RandomChannels: [],
            RandomRanges: [],
            RandomUpdateMode.Interval,
            RandomIntervalMs: 1000,
            RefreshHz: 30,
            OutputLogMode.Continuous);
        byte[] packet = [0, 0, 0, 0, 0, 255, 100, 7];

        string line = DmxOutputLogFormatter.FormatFrame(12, options, packet);

        Assert.AreEqual("frame=12 channels 5-7: 5=255 6=100 7=7", line);
    }
}
