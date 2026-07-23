using Hushward.Core.Actions;
using Hushward.Core.Warnings;

namespace Hushward.Core.Tests.Warnings;

public sealed class WarningPolicyTests
{
    [Theory]
    [InlineData(NightAction.ShutDown, 60)]
    [InlineData(NightAction.Hibernate, 45)]
    [InlineData(NightAction.Sleep, 30)]
    [InlineData(NightAction.Lock, 10)]
    [InlineData(NightAction.WarnOnly, 0)]
    public void Defaults_match_action_risk(NightAction action, int expectedSeconds)
    {
        WarningPolicy.DefaultFor(action).ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Theory]
    [InlineData(NightAction.ShutDown, 60, true)]
    [InlineData(NightAction.ShutDown, 59, false)]
    [InlineData(NightAction.Hibernate, 180, true)]
    [InlineData(NightAction.Sleep, 121, false)]
    [InlineData(NightAction.Lock, 10, true)]
    [InlineData(NightAction.WarnOnly, 1, false)]
    public void Validation_uses_spec_bounds(NightAction action, int seconds, bool expected)
    {
        WarningPolicy.IsValid(action, TimeSpan.FromSeconds(seconds)).ShouldBe(expected);
    }
}
