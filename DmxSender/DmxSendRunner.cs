using System.Diagnostics;

namespace DmxSender;

public static class DmxSendRunner
{
    public static async Task RunAsync(
        SendOptions options,
        IDmxTransport transport,
        IRandomByteSource randomByteSource,
        IMousePositionSource mousePositionSource,
        IKeyPressSource keyPressSource,
        IDmxOutputLogger outputLogger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(randomByteSource);
        ArgumentNullException.ThrowIfNull(mousePositionSource);
        ArgumentNullException.ThrowIfNull(keyPressSource);
        ArgumentNullException.ThrowIfNull(outputLogger);

        RandomChannelState randomState = RandomChannelState.Create(options.RandomChannels, options.Ranges, randomByteSource);
        MouseChannelState mouseState = MouseChannelState.Create(options.MouseChannels, options.Ranges, mousePositionSource);
        var intervalRefreshPolicy = new IntervalRandomRefreshPolicy(TimeSpan.FromMilliseconds(options.RandomIntervalMs));
        var keypressRefreshPolicy = new KeypressRandomRefreshPolicy(keyPressSource);
        TimeSpan frameDelay = TimeSpan.FromSeconds(1.0 / options.RefreshHz);
        var stopwatch = Stopwatch.StartNew();
        TimeSpan lastRandomRefreshAt = TimeSpan.Zero;
        var frameNumber = 0L;
        byte[]? previousPacket = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan now = stopwatch.Elapsed;
            bool randomValuesChanged = options.RandomChannels.Count > 0 && (
                intervalRefreshPolicy.ShouldRefresh(now - lastRandomRefreshAt)
                || keypressRefreshPolicy.ShouldRefresh(now - lastRandomRefreshAt));

            if (randomValuesChanged)
            {
                randomState.Refresh();
                lastRandomRefreshAt = now;
            }

            byte[] packet = DmxFrameBuilder.BuildPacket(options, randomState.Values, mouseState.GetValues());
            await transport.SendPacketAsync(packet, cancellationToken);
            frameNumber++;
            bool packetChanged = previousPacket is null
                ? HasChangeTrackedChannels(options)
                : !packet.SequenceEqual(previousPacket);
            if (ShouldLogFrame(options.OutputLogMode, packetChanged))
            {
                outputLogger.LogFrame(frameNumber, options, packet);
            }

            previousPacket = packet;
            await Task.Delay(frameDelay, cancellationToken);
        }
    }

    private static bool ShouldLogFrame(OutputLogMode outputLogMode, bool packetChanged)
    {
        return outputLogMode switch
        {
            OutputLogMode.Continuous => true,
            OutputLogMode.Change => packetChanged,
            OutputLogMode.None => false,
            _ => false
        };
    }

    private static bool HasChangeTrackedChannels(SendOptions options)
    {
        return options.RandomChannels.Count > 0 || options.MouseChannels.Count > 0;
    }
}
