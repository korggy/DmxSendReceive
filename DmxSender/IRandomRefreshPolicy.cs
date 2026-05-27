namespace DmxSender;

public interface IRandomRefreshPolicy
{
    bool ShouldRefresh(TimeSpan elapsedSinceLastRefresh);
}
