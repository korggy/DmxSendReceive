namespace DmxSender;

public sealed class IntervalRandomRefreshPolicy(TimeSpan interval) : IRandomRefreshPolicy
{
    public bool ShouldRefresh(TimeSpan elapsedSinceLastRefresh)
    {
        return elapsedSinceLastRefresh >= interval;
    }
}
