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

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartSleepShutdown",
        "settings.json");

    public static JsonUserSettingsStore CreateDefault()
    {
        return new JsonUserSettingsStore(DefaultPath);
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
            return Deserialize(json);
        }
        catch (JsonException)
        {
            return TryRecoverBackup();
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

    private UserSettingsSnapshot? TryRecoverBackup()
    {
        var backupPath = $"{_path}.bak";
        if (!File.Exists(backupPath))
        {
            QuarantineCorruptPrimary();
            return null;
        }

        try
        {
            var recovered = Deserialize(File.ReadAllText(backupPath));
            if (recovered is null)
            {
                QuarantineCorruptPrimary();
                return null;
            }

            QuarantineCorruptPrimary();
            File.Copy(backupPath, _path, overwrite: true);
            return recovered;
        }
        catch (JsonException)
        {
            QuarantineCorruptPrimary();
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

    private UserSettingsSnapshot? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<UserSettingsSnapshot>(json, JsonOptions);
    }

    private void QuarantineCorruptPrimary()
    {
        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            var corruptPath = $"{_path}.corrupt-{DateTimeOffset.Now:yyyyMMddHHmmssfff}";
            File.Move(_path, corruptPath, overwrite: false);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
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
