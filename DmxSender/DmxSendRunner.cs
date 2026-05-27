using System.Diagnostics;

namespace DmxSender;

public static class DmxSendRunner
{
    public static async Task RunAsync(
        SendOptions options,
        IDmxTransport transport,
        IRandomByteSource randomByteSource,
        IKeyPressSource keyPressSource,
        IDmxOutputLogger outputLogger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(randomByteSource);
        ArgumentNullException.ThrowIfNull(keyPressSource);
        ArgumentNullException.ThrowIfNull(outputLogger);

        RandomChannelState randomState = RandomChannelState.Create(options.RandomChannels, options.RandomRanges, randomByteSource);
        IRandomRefreshPolicy refreshPolicy = CreateRefreshPolicy(options, keyPressSource);
        TimeSpan frameDelay = TimeSpan.FromSeconds(1.0 / options.RefreshHz);
        var stopwatch = Stopwatch.StartNew();
        TimeSpan lastRandomRefreshAt = TimeSpan.Zero;
        var frameNumber = 0L;
        var randomValuesChanged = options.RandomChannels.Count > 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan now = stopwatch.Elapsed;
            if (options.RandomChannels.Count > 0 && refreshPolicy.ShouldRefresh(now - lastRandomRefreshAt))
            {
                randomState.Refresh();
                lastRandomRefreshAt = now;
                randomValuesChanged = true;
            }

            byte[] packet = DmxFrameBuilder.BuildPacket(options, randomState.Values);
            await transport.SendPacketAsync(packet, cancellationToken);
            frameNumber++;
            if (ShouldLogFrame(options.OutputLogMode, randomValuesChanged))
            {
                outputLogger.LogFrame(frameNumber, options, packet);
            }

            randomValuesChanged = false;
            await Task.Delay(frameDelay, cancellationToken);
        }
    }

    private static bool ShouldLogFrame(OutputLogMode outputLogMode, bool randomValuesChanged)
    {
        return outputLogMode switch
        {
            OutputLogMode.Continuous => true,
            OutputLogMode.RandomChange => randomValuesChanged,
            OutputLogMode.None => false,
            _ => false
        };
    }

    private static IRandomRefreshPolicy CreateRefreshPolicy(SendOptions options, IKeyPressSource keyPressSource)
    {
        return options.RandomUpdateMode switch
        {
            RandomUpdateMode.Interval => new IntervalRandomRefreshPolicy(TimeSpan.FromMilliseconds(options.RandomIntervalMs)),
            RandomUpdateMode.Keypress => new KeypressRandomRefreshPolicy(keyPressSource),
            _ => throw new InvalidOperationException($"Unsupported random update mode {options.RandomUpdateMode}.")
        };
    }
}
