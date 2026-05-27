namespace DmxSender;

public interface IDmxOutputLogger
{
    void LogFrame(long frameNumber, SendOptions options, byte[] packet);
}
