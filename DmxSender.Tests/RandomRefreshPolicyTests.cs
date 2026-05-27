using DmxSender;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DmxSender.Tests;

[TestClass]
public sealed class RandomRefreshPolicyTests
{
    [TestMethod]
    public void IntervalPolicy_RefreshesOnlyWhenIntervalHasElapsed()
    {
        var policy = new IntervalRandomRefreshPolicy(TimeSpan.FromMilliseconds(250));

        Assert.IsFalse(policy.ShouldRefresh(TimeSpan.FromMilliseconds(249)));
        Assert.IsTrue(policy.ShouldRefresh(TimeSpan.FromMilliseconds(250)));
    }

    [TestMethod]
    public void KeypressPolicy_RefreshesOnlyWhenKeyIsAvailable()
    {
        var keyPressSource = new FakeKeyPressSource();
        var policy = new KeypressRandomRefreshPolicy(keyPressSource);

        Assert.IsFalse(policy.ShouldRefresh(TimeSpan.Zero));

        keyPressSource.HasKeyPress = true;

        Assert.IsTrue(policy.ShouldRefresh(TimeSpan.Zero));
        Assert.IsTrue(keyPressSource.WasKeyConsumed);
    }

    private sealed class FakeKeyPressSource : IKeyPressSource
    {
        public bool HasKeyPress { get; set; }

        public bool WasKeyConsumed { get; private set; }

        public bool IsKeyAvailable => HasKeyPress;

        public void ConsumeKey()
        {
            WasKeyConsumed = true;
            HasKeyPress = false;
        }
    }
}
