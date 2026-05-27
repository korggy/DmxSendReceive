using System.CommandLine;
using DmxReceiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxReceiver.Tests;

[TestClass]
public sealed class DmxMonitorCommandTests
{
    [TestMethod]
    public async Task InvokeAsync_RequiresPortWithoutCallingMonitor()
    {
        var monitorCalled = false;
        RootCommand command = DmxMonitorCommand.Create((_, _) =>
        {
            monitorCalled = true;
            return Task.FromResult(0);
        });
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await command.Parse([]).InvokeAsync(new InvocationConfiguration
        {
            Output = output,
            Error = error
        });

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(monitorCalled);
        StringAssert.Contains(error.ToString(), "--port");
        StringAssert.Contains(output.ToString(), "Usage:");
    }

    [TestMethod]
    public async Task InvokeAsync_PrintsHelpWithoutCallingMonitor()
    {
        var monitorCalled = false;
        RootCommand command = DmxMonitorCommand.Create((_, _) =>
        {
            monitorCalled = true;
            return Task.FromResult(0);
        });
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await command.Parse("--help").InvokeAsync(new InvocationConfiguration
        {
            Output = output,
            Error = error
        });

        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(monitorCalled);
        StringAssert.Contains(output.ToString(), "Monitor incoming DMX512 frames");
        StringAssert.Contains(output.ToString(), "--port");
        Assert.AreEqual(string.Empty, error.ToString());
    }

    [TestMethod]
    public async Task InvokeAsync_PassesParsedOptionsToMonitor()
    {
        MonitorOptions? receivedOptions = null;
        RootCommand command = DmxMonitorCommand.Create((options, _) =>
        {
            receivedOptions = options;
            return Task.FromResult(23);
        });

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--max-channels",
            "4",
            "--raw"
        ]).InvokeAsync(new InvocationConfiguration());

        Assert.AreEqual(23, exitCode);
        Assert.IsNotNull(receivedOptions);
        Assert.AreEqual("COM8", receivedOptions.PortName);
        Assert.AreEqual(4, receivedOptions.MaxChannelsToPrint);
        Assert.IsTrue(receivedOptions.PrintRawPacket);
    }

    [TestMethod]
    public async Task InvokeAsync_ReadsEqualsSyntaxSupportedByCommandLineParserPackage()
    {
        MonitorOptions? receivedOptions = null;
        RootCommand command = DmxMonitorCommand.Create((options, _) =>
        {
            receivedOptions = options;
            return Task.FromResult(0);
        });

        int exitCode = await command.Parse([
            "--port=COM8",
            "--max-channels=4",
            "--raw"
        ]).InvokeAsync(new InvocationConfiguration());

        Assert.AreEqual(0, exitCode);
        Assert.IsNotNull(receivedOptions);
        Assert.AreEqual("COM8", receivedOptions.PortName);
        Assert.AreEqual(4, receivedOptions.MaxChannelsToPrint);
        Assert.IsTrue(receivedOptions.PrintRawPacket);
    }

    [TestMethod]
    public async Task InvokeAsync_RejectsInvalidMaxChannelsWithoutCallingMonitor()
    {
        var monitorCalled = false;
        RootCommand command = DmxMonitorCommand.Create((_, _) =>
        {
            monitorCalled = true;
            return Task.FromResult(0);
        });
        using var error = new StringWriter();

        int exitCode = await command.Parse([
            "--port",
            "COM8",
            "--max-channels",
            "-1"
        ]).InvokeAsync(new InvocationConfiguration
        {
            Error = error
        });

        Assert.AreEqual(1, exitCode);
        Assert.IsFalse(monitorCalled);
        StringAssert.Contains(error.ToString(), "--max-channels");
    }
}
