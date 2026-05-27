namespace DmxSender;

public interface IKeyPressSource
{
    bool IsKeyAvailable { get; }

    void ConsumeKey();
}
