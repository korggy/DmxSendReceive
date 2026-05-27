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

        var randomRangeOption = new Option<string[]>("--random-range")
        {
            Description = "Per-channel random value range in channel=min-max form. Repeat for multiple random channels.",
            DefaultValueFactory = _ => []
        };

        var randomUpdateOption = new Option<RandomUpdateMode>("--random-update")
        {
            Description = "When random channels change: interval or keypress.",
            DefaultValueFactory = _ => RandomUpdateMode.Interval,
            CustomParser = result =>
            {
                string token = result.Tokens.Single().Value;
                return token.ToLowerInvariant() switch
                {
                    "interval" => RandomUpdateMode.Interval,
                    "keypress" => RandomUpdateMode.Keypress,
                    _ => AddRandomUpdateError(result)
                };
            }
        };

        var randomIntervalOption = CreatePositiveIntegerOption(
            "--random-interval-ms",
            "Milliseconds between random value changes when --random-update is interval.",
            defaultValue: DefaultRandomIntervalMs);

        var refreshHzOption = CreatePositiveIntegerOption(
            "--refresh-hz",
            "DMX frame refresh rate.",
            defaultValue: DefaultRefreshHz);

        var logOutputOption = new Option<OutputLogMode>("--log-output")
        {
            Description = "Log outgoing channel values: none, continuous, or random-change.",
            DefaultValueFactory = _ => OutputLogMode.None,
            CustomParser = result =>
            {
                string token = result.Tokens.Single().Value;
                return token.ToLowerInvariant() switch
                {
                    "none" => OutputLogMode.None,
                    "continuous" => OutputLogMode.Continuous,
                    "random-change" => OutputLogMode.RandomChange,
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
            randomRangeOption,
            randomUpdateOption,
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

            int[] randomChannels = result.GetValue(randomOption) ?? [];
            var randomChannelSet = randomChannels.ToHashSet();
            var randomRangeChannels = new HashSet<int>();

            foreach (string value in result.GetValue(randomRangeOption) ?? [])
            {
                if (!TryParseRandomChannelRange(value, out RandomChannelRange randomRange, out string error))
                {
                    result.AddError(error);
                    continue;
                }

                AddRangeErrorIfNeeded(result, randomRange.Channel, startChannel, endChannel);
                if (!randomChannelSet.Contains(randomRange.Channel))
                {
                    result.AddError($"Random range channel {randomRange.Channel} must also be specified with --random.");
                }

                if (!randomRangeChannels.Add(randomRange.Channel))
                {
                    result.AddError($"Random range channel {randomRange.Channel} is specified more than once.");
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
            RandomChannelRange[] randomRanges = (parseResult.GetValue(randomRangeOption) ?? [])
                .Select(value => TryParseRandomChannelRange(value, out RandomChannelRange randomRange, out _)
                    ? randomRange
                    : throw new InvalidOperationException("Random ranges should have been validated before invocation."))
                .ToArray();

            var options = new SendOptions(
                parseResult.GetRequiredValue(portOption),
                parseResult.GetValue(startChannelOption),
                parseResult.GetValue(channelCountOption),
                fixedValues,
                parseResult.GetValue(randomOption) ?? [],
                randomRanges,
                parseResult.GetValue(randomUpdateOption),
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

    private static RandomUpdateMode AddRandomUpdateError(ArgumentResult result)
    {
        result.AddError("Expected --random-update to be interval or keypress.");
        return RandomUpdateMode.Interval;
    }

    private static OutputLogMode AddOutputLogModeError(ArgumentResult result)
    {
        result.AddError("Expected --log-output to be none, continuous, or random-change.");
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

    private static bool TryParseRandomChannelRange(string text, out RandomChannelRange randomRange, out string error)
    {
        randomRange = new RandomChannelRange(0, RandomValueRange.Full);
        error = string.Empty;

        string[] parts = text.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)
            || channel <= 0)
        {
            error = $"Expected --random-range value '{text}' to use channel=min-max with channel >= 1 and values 0-255.";
            return false;
        }

        string[] rangeParts = parts[1].Split('-', 2, StringSplitOptions.TrimEntries);
        if (rangeParts.Length != 2
            || !byte.TryParse(rangeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte min)
            || !byte.TryParse(rangeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte max)
            || min > max)
        {
            error = $"Expected --random-range value '{text}' to use channel=min-max with min <= max and values 0-255.";
            return false;
        }

        randomRange = new RandomChannelRange(channel, new RandomValueRange(min, max));
        return true;
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
