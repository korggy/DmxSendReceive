namespace DmxSender;

public sealed class ConsoleDmxOutputLogger : IDmxOutputLogger
{
    public void LogFrame(long frameNumber, SendOptions options, byte[] packet)
    {
        Console.WriteLine(DmxOutputLogFormatter.FormatFrame(frameNumber, options, packet));
    }
}
