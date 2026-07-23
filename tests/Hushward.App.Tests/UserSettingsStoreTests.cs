using Hushward.App.Settings;

namespace Hushward.App.Tests;

public sealed class UserSettingsStoreTests
{
    [Fact]
    public void JsonStoreRoundTripsSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new JsonUserSettingsStore(path);
        var snapshot = new UserSettingsSnapshot(
            IsEnabled: true,
            StartTimeText: "01:30",
            IdleThresholdMinutes: 25,
            ContextChecksEnabled: false,
            TemporarilyDisabledUntil: new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero),
            ResumeAfterTemporaryDisable: true);

        store.Save(snapshot);

        Assert.Equal(snapshot, store.Load());
    }

    [Fact]
    public void JsonStoreKeepsBackupWhenReplacingExistingSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var backupPath = $"{path}.bak";
        var store = new JsonUserSettingsStore(path);
        var original = new UserSettingsSnapshot(
            IsEnabled: true,
            StartTimeText: "01:30",
            IdleThresholdMinutes: 25,
            ContextChecksEnabled: false,
            TemporarilyDisabledUntil: null,
            ResumeAfterTemporaryDisable: false);
        var updated = original with { StartTimeText = "02:00" };

        try
        {
            store.Save(original);
            store.Save(updated);

            Assert.Equal(updated, store.Load());
            Assert.True(File.Exists(backupPath));
        }
        finally
        {
            File.Delete(path);
            File.Delete(backupPath);
        }
    }

    [Fact]
    public void JsonStoreRecoversCorruptLiveFileFromBackup()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var backupPath = $"{path}.bak";
        var store = new JsonUserSettingsStore(path);
        var expected = new UserSettingsSnapshot(true, "01:00", 20, true, null, false, true);

        try
        {
            File.WriteAllText(path, "{invalid");
            File.WriteAllText(backupPath, System.Text.Json.JsonSerializer.Serialize(expected));

            Assert.Equal(expected, store.Load());
            Assert.Equal(expected, store.Load());
        }
        finally
        {
            File.Delete(path);
            File.Delete(backupPath);
        }
    }
}
