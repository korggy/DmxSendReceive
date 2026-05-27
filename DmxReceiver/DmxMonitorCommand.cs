using System.CommandLine;
using System.Globalization;

namespace DmxReceiver;

public static class DmxMonitorCommand
{
    public static RootCommand Create(Func<MonitorOptions, CancellationToken, Task<int>> monitorAsync)
    {
        ArgumentNullException.ThrowIfNull(monitorAsync);

        var portOption = new Option<string>("--port", "-p")
        {
            Description = "Serial COM port for the USB-to-RS485 adapter.",
            Required = true
        };

        var maxChannelsOption = new Option<int>("--max-channels")
        {
            Description = "Maximum channel values to print per frame. Use 0 to print only frame metadata.",
            DefaultValueFactory = _ => MonitorOptions.DefaultMaxChannelsToPrint,
            CustomParser = result =>
            {
                if (!int.TryParse(result.Tokens.Single().Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                    || value < 0)
                {
                    result.AddError("Expected --max-channels to be zero or a positive integer.");
                }

                return value;
            }
        };

        var rawOption = new Option<bool>("--raw")
        {
            Description = "Print the full packet as hex bytes."
        };

        var command = new RootCommand("Monitor incoming DMX512 frames from a USB-to-RS485 serial adapter.")
        {
            portOption,
            maxChannelsOption,
            rawOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var options = new MonitorOptions(
                parseResult.GetRequiredValue(portOption),
                parseResult.GetValue(maxChannelsOption),
                parseResult.GetValue(rawOption));

            return monitorAsync(options, cancellationToken);
        });

        return command;
    }
}
