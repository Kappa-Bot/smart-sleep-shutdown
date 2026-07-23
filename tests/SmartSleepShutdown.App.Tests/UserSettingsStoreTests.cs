using SmartSleepShutdown.App.Settings;

namespace SmartSleepShutdown.App.Tests;

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
    public void JsonStoreRecoversValidBackupWhenPrimaryIsCorrupt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var backupPath = $"{path}.bak";
        var store = new JsonUserSettingsStore(path);
        var snapshot = new UserSettingsSnapshot(
            IsEnabled: true,
            StartTimeText: "01:30",
            IdleThresholdMinutes: 25,
            ContextChecksEnabled: true,
            TemporarilyDisabledUntil: null,
            ResumeAfterTemporaryDisable: false);

        try
        {
            store.Save(snapshot);
            File.Copy(path, backupPath, overwrite: true);
            File.WriteAllText(path, "{ bad json");

            Assert.Equal(snapshot, store.Load());
            Assert.Equal(snapshot, new JsonUserSettingsStore(path).Load());
            Assert.Contains(Directory.EnumerateFiles(Path.GetDirectoryName(path)!), file => file.StartsWith(path + ".corrupt-", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
            File.Delete(backupPath);
            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".corrupt-*"))
            {
                File.Delete(file);
            }
        }
    }
}
