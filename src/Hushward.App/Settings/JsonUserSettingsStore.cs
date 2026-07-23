using System.IO;
using System.Text.Json;

namespace Hushward.App.Settings;

public sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;
    private readonly string? _legacyPath;

    public JsonUserSettingsStore(string path, string? legacyPath = null)
    {
        _path = path;
        _legacyPath = legacyPath;
    }

    public static JsonUserSettingsStore CreateDefault()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Hushward");
        return new JsonUserSettingsStore(
            Path.Combine(directory, "runtime-settings.json"),
            Path.Combine(directory, "settings.json"));
    }

    public UserSettingsSnapshot? Load()
    {
        var loaded = TryLoad(_path);
        if (loaded is not null)
        {
            return loaded;
        }

        loaded = TryLoad($"{_path}.bak") ?? TryLoad(_legacyPath);
        if (loaded is not null)
        {
            Save(loaded);
        }

        return loaded;
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

    private static UserSettingsSnapshot? TryLoad(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserSettingsSnapshot>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
