namespace DmxSender;

public sealed class KeypressRandomRefreshPolicy(IKeyPressSource keyPressSource) : IRandomRefreshPolicy
{
    public bool ShouldRefresh(TimeSpan elapsedSinceLastRefresh)
    {
        if (!keyPressSource.IsKeyAvailable)
        {
            return false;
        }

        keyPressSource.ConsumeKey();
        return true;
    }
}
