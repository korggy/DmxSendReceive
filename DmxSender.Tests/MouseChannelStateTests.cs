using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class MouseChannelStateTests
{
    [TestMethod]
    public void GetValues_MapsXLeftAndRightToZeroAnd255()
    {
        var state = MouseChannelState.Create(
            [new MouseChannelMapping(1, MouseAxis.X)],
            [],
            new StubMousePositionSource(new MousePosition(0, 50), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(0, state.GetValues()[1]);

        state = MouseChannelState.Create(
            [new MouseChannelMapping(1, MouseAxis.X)],
            [],
            new StubMousePositionSource(new MousePosition(100, 50), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(255, state.GetValues()[1]);
    }

    [TestMethod]
    public void GetValues_MapsYBottomAndTopToZeroAnd255()
    {
        var state = MouseChannelState.Create(
            [new MouseChannelMapping(2, MouseAxis.Y)],
            [],
            new StubMousePositionSource(new MousePosition(50, 100), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(0, state.GetValues()[2]);

        state = MouseChannelState.Create(
            [new MouseChannelMapping(2, MouseAxis.Y)],
            [],
            new StubMousePositionSource(new MousePosition(50, 0), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(255, state.GetValues()[2]);
    }

    [TestMethod]
    public void GetValues_MapsInvertedXRightAndLeftToZeroAnd255()
    {
        var state = MouseChannelState.Create(
            [new MouseChannelMapping(1, MouseAxis.XInverted)],
            [],
            new StubMousePositionSource(new MousePosition(100, 50), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(0, state.GetValues()[1]);

        state = MouseChannelState.Create(
            [new MouseChannelMapping(1, MouseAxis.XInverted)],
            [],
            new StubMousePositionSource(new MousePosition(0, 50), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(255, state.GetValues()[1]);
    }

    [TestMethod]
    public void GetValues_MapsInvertedYTopAndBottomToZeroAnd255()
    {
        var state = MouseChannelState.Create(
            [new MouseChannelMapping(2, MouseAxis.YInverted)],
            [],
            new StubMousePositionSource(new MousePosition(50, 0), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(0, state.GetValues()[2]);

        state = MouseChannelState.Create(
            [new MouseChannelMapping(2, MouseAxis.YInverted)],
            [],
            new StubMousePositionSource(new MousePosition(50, 100), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(255, state.GetValues()[2]);
    }

    [TestMethod]
    public void GetValues_ClampsCoordinatesOutsideScreen()
    {
        var state = MouseChannelState.Create(
            [
                new MouseChannelMapping(1, MouseAxis.X),
                new MouseChannelMapping(2, MouseAxis.Y)
            ],
            [],
            new StubMousePositionSource(new MousePosition(150, -50), new ScreenBounds(0, 0, 101, 101)));

        IReadOnlyDictionary<int, byte> values = state.GetValues();

        Assert.AreEqual(255, values[1]);
        Assert.AreEqual(255, values[2]);
    }

    [TestMethod]
    public void GetValues_AppliesPerChannelRangeToMouseValues()
    {
        var state = MouseChannelState.Create(
            [new MouseChannelMapping(1, MouseAxis.X)],
            [new ChannelRange(1, new RandomValueRange(10, 20))],
            new StubMousePositionSource(new MousePosition(50, 50), new ScreenBounds(0, 0, 101, 101)));

        Assert.AreEqual(15, state.GetValues()[1]);
    }

    private sealed class StubMousePositionSource(MousePosition position, ScreenBounds screenBounds) : IMousePositionSource
    {
        public MousePosition GetPosition()
        {
            return position;
        }

        public ScreenBounds GetScreenBounds()
        {
            return screenBounds;
        }
    }
}
