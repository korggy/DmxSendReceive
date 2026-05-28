using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;

namespace DmxSender;

public static class DmxSendCommand
{
    private const int DefaultStartChannel = 1;
    private const int DefaultChannelCount = 9;
    private const int DefaultRandomIntervalMs = 1000;
    private const int DefaultRefreshHz = 30;

    public static RootCommand Create(Func<SendOptions, CancellationToken, Task<int>> sendAsync)
    {
        ArgumentNullException.ThrowIfNull(sendAsync);

        var portOption = new Option<string>("--port", "-p")
        {
            Description = "Serial COM port for the USB-to-RS485 adapter.",
            Required = true
        };

        var startChannelOption = CreatePositiveIntegerOption(
            "--start-channel",
            "First DMX channel controlled by this command.",
            defaultValue: DefaultStartChannel);

        var channelCountOption = CreatePositiveIntegerOption(
            "--channel-count",
            "Number of consecutive DMX channels to send from the start channel.",
            defaultValue: DefaultChannelCount);

        var fixedOption = new Option<string[]>("--fixed", "-f")
        {
            Description = "Fixed channel value in channel=value form. Repeat for multiple channels.",
            DefaultValueFactory = _ => []
        };

        var randomOption = new Option<int[]>("--random", "-r")
        {
            Description = "Channel to assign random values. Repeat for multiple channels.",
            DefaultValueFactory = _ => []
        };

        var rangeOption = new Option<string[]>("--range")
        {
            Description = "Per-channel random or mouse value range in channel=min-max form. Repeat for multiple channels.",
            DefaultValueFactory = _ => []
        };

        var mouseOption = new Option<string[]>("--mouse", "-m")
        {
            Description = "Mouse-tracked channel in channel=x, channel=xi, channel=y, or channel=yi form. Repeat for multiple channels.",
            DefaultValueFactory = _ => []
        };

        var randomIntervalOption = CreatePositiveIntegerOption(
            "--random-interval-ms",
            "Milliseconds between random value changes.",
            defaultValue: DefaultRandomIntervalMs);

        var refreshHzOption = CreatePositiveIntegerOption(
            "--refresh-hz",
            "DMX frame refresh rate.",
            defaultValue: DefaultRefreshHz);

        var logOutputOption = new Option<OutputLogMode>("--log-output")
        {
            Description = "Log outgoing channel values: none, continuous, or change.",
            DefaultValueFactory = _ => OutputLogMode.None,
            CustomParser = result =>
            {
                string token = result.Tokens.Single().Value;
                return token.ToLowerInvariant() switch
                {
                    "none" => OutputLogMode.None,
                    "continuous" => OutputLogMode.Continuous,
                    "change" => OutputLogMode.Change,
                    _ => AddOutputLogModeError(result)
                };
            }
        };

        var command = new RootCommand("Send DMX512 frames through a USB-to-RS485 serial adapter.")
        {
            portOption,
            startChannelOption,
            channelCountOption,
            fixedOption,
            randomOption,
            rangeOption,
            mouseOption,
            randomIntervalOption,
            refreshHzOption,
            logOutputOption
        };

        command.Validators.Add(result =>
        {
            int startChannel = GetOptionValueOrDefault(result, startChannelOption, DefaultStartChannel);
            int channelCount = GetOptionValueOrDefault(result, channelCountOption, DefaultChannelCount);
            int endChannel = startChannel + channelCount - 1;

            if (endChannel > DmxFrameBuilder.MaxDmxChannel)
            {
                result.AddError($"Channel range {startChannel}-{endChannel} exceeds DMX channel {DmxFrameBuilder.MaxDmxChannel}.");
                return;
            }

            foreach (string value in result.GetValue(fixedOption) ?? [])
            {
                if (!TryParseChannelValue(value, out ChannelValue fixedValue, out string error))
                {
                    result.AddError(error);
                    continue;
                }

                AddRangeErrorIfNeeded(result, fixedValue.Channel, startChannel, endChannel);
            }

            foreach (int randomChannel in result.GetValue(randomOption) ?? [])
            {
                AddRangeErrorIfNeeded(result, randomChannel, startChannel, endChannel);
            }

            foreach (string value in result.GetValue(mouseOption) ?? [])
            {
                if (!TryParseMouseChannelMapping(value, out MouseChannelMapping mouseChannel, out string error))
                {
                    result.AddError(error);
                    continue;
                }

                AddRangeErrorIfNeeded(result, mouseChannel.Channel, startChannel, endChannel);
            }

            int[] randomChannels = result.GetValue(randomOption) ?? [];
            var randomChannelSet = randomChannels.ToHashSet();
            var mouseChannelSet = (result.GetValue(mouseOption) ?? [])
                .Select(value => TryParseMouseChannelMapping(value, out MouseChannelMapping mouseChannel, out _)
                    ? mouseChannel.Channel
                    : 0)
                .ToHashSet();
            var rangeChannels = new HashSet<int>();

            foreach (string value in result.GetValue(rangeOption) ?? [])
            {
                if (!TryParseChannelRange(value, out ChannelRange range, out string error))
                {
                    result.AddError(error);
                    continue;
                }

                AddRangeErrorIfNeeded(result, range.Channel, startChannel, endChannel);
                if (!randomChannelSet.Contains(range.Channel) && !mouseChannelSet.Contains(range.Channel))
                {
                    result.AddError($"Range channel {range.Channel} must also be specified with --random or --mouse.");
                }

                if (!rangeChannels.Add(range.Channel))
                {
                    result.AddError($"Range channel {range.Channel} is specified more than once.");
                }
            }
        });

        command.SetAction((parseResult, cancellationToken) =>
        {
            ChannelValue[] fixedValues = (parseResult.GetValue(fixedOption) ?? [])
                .Select(value => TryParseChannelValue(value, out ChannelValue fixedValue, out _)
                    ? fixedValue
                    : throw new InvalidOperationException("Fixed channel values should have been validated before invocation."))
                .ToArray();
            ChannelRange[] ranges = (parseResult.GetValue(rangeOption) ?? [])
                .Select(value => TryParseChannelRange(value, out ChannelRange range, out _)
                    ? range
                    : throw new InvalidOperationException("Ranges should have been validated before invocation."))
                .ToArray();
            MouseChannelMapping[] mouseChannels = (parseResult.GetValue(mouseOption) ?? [])
                .Select(value => TryParseMouseChannelMapping(value, out MouseChannelMapping mouseChannel, out _)
                    ? mouseChannel
                    : throw new InvalidOperationException("Mouse channels should have been validated before invocation."))
                .ToArray();

            var options = new SendOptions(
                parseResult.GetRequiredValue(portOption),
                parseResult.GetValue(startChannelOption),
                parseResult.GetValue(channelCountOption),
                fixedValues,
                parseResult.GetValue(randomOption) ?? [],
                ranges,
                mouseChannels,
                parseResult.GetValue(randomIntervalOption),
                parseResult.GetValue(refreshHzOption),
                parseResult.GetValue(logOutputOption));

            return sendAsync(options, cancellationToken);
        });

        return command;
    }

    private static Option<int> CreatePositiveIntegerOption(string name, string description, int defaultValue)
    {
        var option = new Option<int>(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue,
            CustomParser = result =>
            {
                if (!int.TryParse(result.Tokens.Single().Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                    || value <= 0)
                {
                    result.AddError($"Expected {name} to be a positive integer.");
                }

                return value;
            }
        };

        return option;
    }

    private static OutputLogMode AddOutputLogModeError(ArgumentResult result)
    {
        result.AddError("Expected --log-output to be none, continuous, or change.");
        return OutputLogMode.None;
    }

    private static bool TryParseChannelValue(string text, out ChannelValue channelValue, out string error)
    {
        channelValue = new ChannelValue(0, 0);
        error = string.Empty;

        string[] parts = text.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)
            || !byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value)
            || channel <= 0)
        {
            error = $"Expected --fixed value '{text}' to use channel=value with channel >= 1 and value 0-255.";
            return false;
        }

        channelValue = new ChannelValue(channel, value);
        return true;
    }

    private static bool TryParseChannelRange(string text, out ChannelRange range, out string error)
    {
        range = new ChannelRange(0, RandomValueRange.Full);
        error = string.Empty;

        string[] parts = text.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)
            || channel <= 0)
        {
            error = $"Expected --range value '{text}' to use channel=min-max with channel >= 1 and values 0-255.";
            return false;
        }

        string[] rangeParts = parts[1].Split('-', 2, StringSplitOptions.TrimEntries);
        if (rangeParts.Length != 2
            || !byte.TryParse(rangeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte min)
            || !byte.TryParse(rangeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte max)
            || min > max)
        {
            error = $"Expected --range value '{text}' to use channel=min-max with min <= max and values 0-255.";
            return false;
        }

        range = new ChannelRange(channel, new RandomValueRange(min, max));
        return true;
    }

    private static bool TryParseMouseChannelMapping(string text, out MouseChannelMapping mouseChannel, out string error)
    {
        mouseChannel = new MouseChannelMapping(0, MouseAxis.X);
        error = string.Empty;

        string[] parts = text.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)
            || channel <= 0)
        {
            error = GetMouseChannelError(text);
            return false;
        }

        MouseAxis axis;
        switch (parts[1].ToLowerInvariant())
        {
            case "x":
                axis = MouseAxis.X;
                break;
            case "xi":
                axis = MouseAxis.XInverted;
                break;
            case "y":
                axis = MouseAxis.Y;
                break;
            case "yi":
                axis = MouseAxis.YInverted;
                break;
            default:
                error = GetMouseChannelError(text);
                return false;
        }

        mouseChannel = new MouseChannelMapping(channel, axis);
        return true;
    }

    private static string GetMouseChannelError(string text)
    {
        return $"Expected --mouse value '{text}' to use channel=x, channel=xi, channel=y, or channel=yi with channel >= 1.";
    }

    private static void AddRangeErrorIfNeeded(CommandResult result, int channel, int startChannel, int endChannel)
    {
        if (channel < startChannel || channel > endChannel)
        {
            result.AddError($"Channel {channel} is outside configured channel range {startChannel}-{endChannel}.");
        }
    }

    private static int GetOptionValueOrDefault(CommandResult result, Option<int> option, int defaultValue)
    {
        return result.GetResult(option)?.GetValueOrDefault<int>() ?? defaultValue;
    }
}
