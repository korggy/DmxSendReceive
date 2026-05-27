using System.Diagnostics;
using System.IO.Ports;
using System.CommandLine;
using System.Collections.Concurrent;
using DmxReceiver;

try
{
	return await DmxMonitorCommand.Create(RunAsync)
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

static async Task<int> RunAsync(MonitorOptions options, CancellationToken cancellationToken)
{
	using var serialPort = new SerialPort(options.PortName, 250000, Parity.None, 8, StopBits.Two)
	{
		Handshake = Handshake.None,
		ReadTimeout = 100,
		WriteTimeout = 100,
		DtrEnable = false,
		RtsEnable = false
	};

	var breakEvents = new ConcurrentQueue<TimeSpan>();
	var stopwatch = Stopwatch.StartNew();
	serialPort.ErrorReceived += (_, eventArgs) =>
	{
		if (eventArgs.EventType is SerialError.Frame)
		{
			breakEvents.Enqueue(stopwatch.Elapsed);
		}
	};

	serialPort.Open();

	Console.WriteLine($"Listening on {options.PortName} at 250000 8N2. Press Ctrl+C to stop.");
	Console.WriteLine("If frames do not appear, confirm the adapter is connected A+ to DMX Data+ and B- to DMX Data-, with signal ground connected.");
	Console.WriteLine("PC serial adapters usually cannot measure DMX BREAK length directly; UART framing errors followed by start code 0x00 are counted as inferred frames.");

	var receiver = new DmxBreakSynchronizedReceiver();
	var summary = new DmxReceiveSummary();
	TimeSpan lastSummaryAt = stopwatch.Elapsed;

	try
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			PrintBreakEvents(receiver, breakEvents, options, summary);
			PrintSummaryIfDue(summary, stopwatch.Elapsed, ref lastSummaryAt);

			int value;

			try
			{
				value = serialPort.ReadByte();
			}
			catch (TimeoutException)
			{
				await Task.Delay(1, cancellationToken);
				continue;
			}

			PrintBreakEvents(receiver, breakEvents, options, summary);

			DmxReceiveEvent? receiveEvent = receiver.AddByte((byte)value, stopwatch.Elapsed);
			PrintReceiveEvent(receiveEvent, options, summary);
			PrintSummaryIfDue(summary, stopwatch.Elapsed, ref lastSummaryAt);
		}

		DmxReceiveEvent? finalEvent = receiver.Flush();
		PrintReceiveEvent(finalEvent, options, summary);
	}
	finally
	{
		Console.WriteLine(summary.Format());
	}

	return 0;
}

static void PrintBreakEvents(
	DmxBreakSynchronizedReceiver receiver,
	ConcurrentQueue<TimeSpan> breakEvents,
	MonitorOptions options,
	DmxReceiveSummary summary)
{
	if (!breakEvents.TryDequeue(out TimeSpan observedAt))
	{
		return;
	}

	var framingErrorCount = 1;
	TimeSpan lastObservedAt = observedAt;
	while (breakEvents.TryDequeue(out TimeSpan additionalBreakAt))
	{
		framingErrorCount++;
		lastObservedAt = additionalBreakAt;
	}

	TimeSpan? estimatedBreakLength = framingErrorCount > 1
		&& lastObservedAt - observedAt >= DmxBreakSynchronizedReceiver.MinimumBreakLength
			? lastObservedAt - observedAt
		: null;

	DmxReceiveEvent? receiveEvent = receiver.NotifyBreak(observedAt, estimatedBreakLength, framingErrorCount);
	PrintReceiveEvent(receiveEvent, options, summary);
}

static void PrintReceiveEvent(DmxReceiveEvent? receiveEvent, MonitorOptions options, DmxReceiveSummary summary)
{
	if (receiveEvent is not null)
	{
		summary.Record(receiveEvent);
		if (options.PrintRawPacket && receiveEvent is not DmxPacketDiscarded)
		{
			Console.WriteLine(DmxFrameFormatter.Format(receiveEvent, options.MaxChannelsToPrint, includeRawPacket: true));
		}
	}
}

static void PrintSummaryIfDue(DmxReceiveSummary summary, TimeSpan now, ref TimeSpan lastSummaryAt)
{
	if (now - lastSummaryAt < TimeSpan.FromSeconds(1))
	{
		return;
	}

	Console.WriteLine(summary.Format());
	lastSummaryAt = now;
}
