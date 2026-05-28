using System.CommandLine;
using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class DmxSendCommandTests
{
    [TestMethod]
    public async Task InvokeAsync_PassesParsedOptionsToSender()
    {
        SendOptions? receivedOptions = null;
        RootCommand command = DmxSendCommand.Create((options, _) =>
        {
            receivedOptions = options;
            return Task.FromResult(42);
        });

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--start-channel",
            "10",
            "--channel-count",
            "9",
            "--fixed",
            "15=255",
            "--fixed",
            "16=32",
            "--random",
            "10",
            "--random",
            "11",
            "--random-interval-ms",
            "250",
            "--mouse",
            "12=x",
            "-m",
            "13=y",
            "--mouse",
            "14=xi",
            "-m",
            "15=yi",
            "--refresh-hz",
            "30",
            "--log-output",
            "continuous"
        ]).InvokeAsync(new InvocationConfiguration());

        Assert.AreEqual(42, exitCode);
        Assert.IsNotNull(receivedOptions);
        Assert.AreEqual("COM8", receivedOptions.PortName);
        Assert.AreEqual(10, receivedOptions.StartChannel);
        Assert.AreEqual(9, receivedOptions.ChannelCount);
        Assert.AreEqual(250, receivedOptions.RandomIntervalMs);
        Assert.AreEqual(30, receivedOptions.RefreshHz);
        Assert.AreEqual(OutputLogMode.Continuous, receivedOptions.OutputLogMode);
        CollectionAssert.AreEqual(new[] { new ChannelValue(15, 255), new ChannelValue(16, 32) }, receivedOptions.FixedValues.ToArray());
        CollectionAssert.AreEqual(new[] { 10, 11 }, receivedOptions.RandomChannels.ToArray());
        CollectionAssert.AreEqual(new[]
        {
            new MouseChannelMapping(12, MouseAxis.X),
            new MouseChannelMapping(13, MouseAxis.Y),
            new MouseChannelMapping(14, MouseAxis.XInverted),
            new MouseChannelMapping(15, MouseAxis.YInverted)
        }, receivedOptions.MouseChannels.ToArray());
    }

    [TestMethod]
    public async Task InvokeAsync_UsesDefaultChannelRangeWhenRangeOptionsAreOmitted()
    {
        SendOptions? receivedOptions = null;
        RootCommand command = DmxSendCommand.Create((options, _) =>
        {
            receivedOptions = options;
            return Task.FromResult(0);
        });
        using var error = new StringWriter();

        int exitCode = await command.Parse([
            "-p",
            "com3",
            "-f",
            "5=255",
            "-f",
            "6=100",
            "-r",
            "1",
            "-r",
            "2"
        ]).InvokeAsync(new InvocationConfiguration
        {
            Error = error
        });

        Assert.AreEqual(0, exitCode, error.ToString());
        Assert.IsNotNull(receivedOptions);
        Assert.AreEqual(1, receivedOptions.StartChannel);
        Assert.AreEqual(9, receivedOptions.ChannelCount);
        Assert.AreEqual(OutputLogMode.None, receivedOptions.OutputLogMode);
        CollectionAssert.AreEqual(new[] { new ChannelValue(5, 255), new ChannelValue(6, 100) }, receivedOptions.FixedValues.ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2 }, receivedOptions.RandomChannels.ToArray());
    }

    [TestMethod]
    public async Task InvokeAsync_ParsesChangeOutputLogging()
    {
        SendOptions? receivedOptions = null;
        RootCommand command = DmxSendCommand.Create((options, _) =>
        {
            receivedOptions = options;
            return Task.FromResult(0);
        });

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--log-output",
            "change"
        ]).InvokeAsync(new InvocationConfiguration());

        Assert.AreEqual(0, exitCode);
        Assert.IsNotNull(receivedOptions);
        Assert.AreEqual(OutputLogMode.Change, receivedOptions.OutputLogMode);
    }

    [TestMethod]
    public async Task InvokeAsync_ParsesPerChannelRanges()
    {
        SendOptions? receivedOptions = null;
        RootCommand command = DmxSendCommand.Create((options, _) =>
        {
            receivedOptions = options;
            return Task.FromResult(0);
        });

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--random",
            "1",
            "--random",
            "2",
            "--mouse",
            "3=x",
            "--range",
            "1=40-180",
            "--range",
            "3=10-20"
        ]).InvokeAsync(new InvocationConfiguration());

        Assert.AreEqual(0, exitCode);
        Assert.IsNotNull(receivedOptions);
        CollectionAssert.AreEqual(new[]
        {
            new ChannelRange(1, new RandomValueRange(40, 180)),
            new ChannelRange(3, new RandomValueRange(10, 20))
        }, receivedOptions.Ranges.ToArray());
    }

    [TestMethod]
    public async Task InvokeAsync_RejectsRangeForUncontrolledChannel()
    {
        var senderCalled = false;
        RootCommand command = DmxSendCommand.Create((_, _) =>
        {
            senderCalled = true;
            return Task.FromResult(0);
        });
        using var error = new StringWriter();

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--random",
            "1",
            "--range",
            "2=10-20"
        ]).InvokeAsync(new InvocationConfiguration
        {
            Error = error
        });

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(senderCalled);
        StringAssert.Contains(error.ToString(), "Range channel 2 must also be specified with --random or --mouse.");
    }

    [TestMethod]
    public async Task InvokeAsync_RejectsInvalidRange()
    {
        var senderCalled = false;
        RootCommand command = DmxSendCommand.Create((_, _) =>
        {
            senderCalled = true;
            return Task.FromResult(0);
        });
        using var error = new StringWriter();

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--random",
            "1",
            "--range",
            "1=200-100"
        ]).InvokeAsync(new InvocationConfiguration
        {
            Error = error
        });

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(senderCalled);
        StringAssert.Contains(error.ToString(), "1=200-100");
    }

    [TestMethod]
    public async Task InvokeAsync_RejectsChannelOutsideConfiguredRange()
    {
        var senderCalled = false;
        RootCommand command = DmxSendCommand.Create((_, _) =>
        {
            senderCalled = true;
            return Task.FromResult(0);
        });
        using var error = new StringWriter();

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--start-channel",
            "10",
            "--channel-count",
            "9",
            "--fixed",
            "20=255"
        ]).InvokeAsync(new InvocationConfiguration
        {
            Error = error
        });

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(senderCalled);
        StringAssert.Contains(error.ToString(), "20");
        StringAssert.Contains(error.ToString(), "10-18");
    }

    [TestMethod]
    public async Task InvokeAsync_RejectsInvalidMouseAxis()
    {
        var senderCalled = false;
        RootCommand command = DmxSendCommand.Create((_, _) =>
        {
            senderCalled = true;
            return Task.FromResult(0);
        });
        using var error = new StringWriter();

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--mouse",
            "1=z"
        ]).InvokeAsync(new InvocationConfiguration
        {
            Error = error
        });

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(senderCalled);
        StringAssert.Contains(error.ToString(), "Expected --mouse value '1=z' to use channel=x, channel=xi, channel=y, or channel=yi with channel >= 1.");
    }
}
