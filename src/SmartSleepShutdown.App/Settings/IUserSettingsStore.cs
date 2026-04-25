namespace SmartSleepShutdown.App.Settings;

public interface IUserSettingsStore
{
    UserSettingsSnapshot? Load();

    void Save(UserSettingsSnapshot snapshot);
}
