using System.CommandLine;
using DmxSender;

try
{
    return await DmxSendCommand.Create(RunAsync)
        .Parse(args)
        .InvokeAsync(new InvocationConfiguration
        {
            EnableDefaultExceptionHandler = false
        });
}
catch (OperationCanceledException)
{
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static async Task<int> RunAsync(SendOptions options, CancellationToken cancellationToken)
{
    using var transport = SerialDmxTransport.Open(options.PortName);

    int endChannel = options.StartChannel + options.ChannelCount - 1;
    Console.WriteLine($"Sending DMX on {options.PortName} at 250000 8N2 for channels {options.StartChannel}-{endChannel}. Press Ctrl+C to stop.");
    Console.WriteLine("Connect adapter A+ to DMX Data+, B- to DMX Data-, and RS485 GND to the DMX signal reference/ground when available.");

    if (options.RandomUpdateMode is RandomUpdateMode.Keypress && options.RandomChannels.Count > 0)
    {
        Console.WriteLine("Press any key to generate new random channel values.");
    }

    await DmxSendRunner.RunAsync(
        options,
        transport,
        new RandomByteSource(),
        new ConsoleKeyPressSource(),
        options.OutputLogMode is OutputLogMode.None ? new NullDmxOutputLogger() : new ConsoleDmxOutputLogger(),
        cancellationToken);

    return 0;
}
