using System.IO;
using System.Text.Json;

namespace SmartSleepShutdown.App.Settings;

public sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public JsonUserSettingsStore(string path)
    {
        _path = path;
    }

    public static JsonUserSettingsStore CreateDefault()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartSleepShutdown");
        return new JsonUserSettingsStore(Path.Combine(directory, "settings.json"));
    }

    public UserSettingsSnapshot? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<UserSettingsSnapshot>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save(UserSettingsSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        var tempPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        var backupPath = $"{_path}.bak";

        try
        {
            File.WriteAllText(tempPath, json);

            if (File.Exists(_path))
            {
                File.Replace(tempPath, _path, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, _path);
            }
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
