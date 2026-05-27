using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class DmxSendRunnerTests
{
    [TestMethod]
    public async Task RunAsync_ContinuousLoggingLogsEverySentFrame()
    {
        using var cancellation = new CancellationTokenSource();
        var logger = new CapturingOutputLogger();
        var transport = new CancellingTransport(cancellation, cancelAfterSends: 3);
        SendOptions options = CreateOptions(OutputLogMode.Continuous);

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => DmxSendRunner.RunAsync(
            options,
            transport,
            new SequenceRandomByteSource(10, 20, 30, 40),
            new NoKeyPressSource(),
            logger,
            cancellation.Token));

        Assert.AreEqual(3, logger.Frames.Count);
        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, logger.Frames.Select(frame => frame.FrameNumber).ToArray());
    }

    [TestMethod]
    public async Task RunAsync_RandomChangeLoggingLogsOnlyInitialAndChangedRandomValues()
    {
        using var cancellation = new CancellationTokenSource();
        var logger = new CapturingOutputLogger();
        var transport = new CancellingTransport(cancellation, cancelAfterSends: 3);
        SendOptions options = CreateOptions(OutputLogMode.RandomChange);

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => DmxSendRunner.RunAsync(
            options,
            transport,
            new SequenceRandomByteSource(10, 20, 30, 40),
            new SecondPollKeyPressSource(),
            logger,
            cancellation.Token));

        Assert.AreEqual(2, logger.Frames.Count);
        CollectionAssert.AreEqual(new long[] { 1, 2 }, logger.Frames.Select(frame => frame.FrameNumber).ToArray());
    }

    private static SendOptions CreateOptions(OutputLogMode outputLogMode)
    {
        return new SendOptions(
            "COM8",
            StartChannel: 1,
            ChannelCount: 2,
            FixedValues: [],
            RandomChannels: [1, 2],
            RandomRanges: [],
            RandomUpdateMode.Keypress,
            RandomIntervalMs: 1000,
            RefreshHz: 1000,
            outputLogMode);
    }

    private sealed class CancellingTransport(CancellationTokenSource cancellation, int cancelAfterSends) : IDmxTransport
    {
        private int _sendCount;

        public Task SendPacketAsync(byte[] packet, CancellationToken cancellationToken)
        {
            _sendCount++;
            if (_sendCount >= cancelAfterSends)
            {
                cancellation.Cancel();
            }

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingOutputLogger : IDmxOutputLogger
    {
        public List<(long FrameNumber, byte[] Packet)> Frames { get; } = [];

        public void LogFrame(long frameNumber, SendOptions options, byte[] packet)
        {
            Frames.Add((frameNumber, packet.ToArray()));
        }
    }

    private sealed class SequenceRandomByteSource(params byte[] values) : IRandomByteSource
    {
        private int _index;

        public byte NextByte(RandomValueRange range)
        {
            return values[_index++];
        }
    }

    private sealed class NoKeyPressSource : IKeyPressSource
    {
        public bool IsKeyAvailable => false;

        public void ConsumeKey()
        {
        }
    }

    private sealed class SecondPollKeyPressSource : IKeyPressSource
    {
        private int _pollCount;

        public bool IsKeyAvailable
        {
            get
            {
                _pollCount++;
                return _pollCount == 2;
            }
        }

        public void ConsumeKey()
        {
        }
    }
}
