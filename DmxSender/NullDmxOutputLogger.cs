namespace DmxSender;

public sealed class NullDmxOutputLogger : IDmxOutputLogger
{
    public void LogFrame(long frameNumber, SendOptions options, byte[] packet)
    {
    }
}
