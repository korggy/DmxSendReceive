namespace DmxSender;

public sealed class ConsoleKeyPressSource : IKeyPressSource
{
    public bool IsKeyAvailable => !Console.IsInputRedirected && Console.KeyAvailable;

    public void ConsumeKey()
    {
        if (!Console.IsInputRedirected && Console.KeyAvailable)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
