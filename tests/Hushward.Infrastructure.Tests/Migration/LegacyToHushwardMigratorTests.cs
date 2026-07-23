using Hushward.Infrastructure.Migration;

namespace Hushward.Infrastructure.Tests.Migration;

public sealed class LegacyToHushwardMigratorTests
{
    [Fact]
    public async Task Legacy_enabled_setting_migrates_to_disabled_review_required_routine()
    {
        var legacy = new LegacySettings(
            Enabled: true,
            StartTime: "01:00",
            IdleThresholdMinutes: 15,
            ContextChecksEnabled: true);

        var migrated = await LegacyToHushwardMigrator.MigrateAsync(legacy, CancellationToken.None);

        Assert.Single(migrated.Configuration.Routines);
        Assert.False(migrated.Configuration.Routines[0].Enabled);
        Assert.True(migrated.Configuration.RequiresMigrationReview);
    }
}
