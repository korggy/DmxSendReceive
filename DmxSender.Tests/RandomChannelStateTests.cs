using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class RandomChannelStateTests
{
    [TestMethod]
    public void Create_InitializesEachRandomChannel()
    {
        var state = RandomChannelState.Create([1, 3], [], new SequenceRandomByteSource(10, 20));

        CollectionAssert.AreEqual(new[] { 1, 3 }, state.Values.Keys.ToArray());
        Assert.AreEqual(10, state.Values[1]);
        Assert.AreEqual(20, state.Values[3]);
    }

    [TestMethod]
    public void Refresh_ChangesEveryRandomChannel()
    {
        var state = RandomChannelState.Create([1, 3], [], new SequenceRandomByteSource(10, 20, 30, 40));

        state.Refresh();

        Assert.AreEqual(30, state.Values[1]);
        Assert.AreEqual(40, state.Values[3]);
    }

    [TestMethod]
    public void Refresh_UsesConfiguredRangeForEachChannel()
    {
        var state = RandomChannelState.Create(
            [1, 3],
            [new ChannelRange(1, new RandomValueRange(40, 180))],
            new RangeAwareRandomByteSource());

        Assert.AreEqual(40, state.Values[1]);
        Assert.AreEqual(0, state.Values[3]);
    }

    private sealed class SequenceRandomByteSource(params byte[] values) : IRandomByteSource
    {
        private int _index;

        public byte NextByte(RandomValueRange range)
        {
            return values[_index++];
        }
    }

    private sealed class RangeAwareRandomByteSource : IRandomByteSource
    {
        public byte NextByte(RandomValueRange range)
        {
            return range.Min;
        }
    }
}
