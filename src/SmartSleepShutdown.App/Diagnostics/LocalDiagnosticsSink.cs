using System.IO;
using System.Text.Json;

namespace SmartSleepShutdown.App.Diagnostics;

public interface IDiagnosticsSink
{
    void Write(string eventName, object? data = null);
}

public sealed class LocalDiagnosticsSink : IDiagnosticsSink
{
    private const long MaxBytes = 512 * 1024;
    private readonly string _path;

    public LocalDiagnosticsSink(string path)
    {
        _path = path;
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartSleepShutdown",
        "diagnostics.jsonl");

    public static LocalDiagnosticsSink CreateDefault()
    {
        return new LocalDiagnosticsSink(DefaultPath);
    }

    public void Write(string eventName, object? data = null)
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            RotateIfNeeded();
            var line = JsonSerializer.Serialize(new
            {
                at = DateTimeOffset.Now,
                eventName,
                data
            });
            File.AppendAllText(_path, line + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void RotateIfNeeded()
    {
        var file = new FileInfo(_path);
        if (!file.Exists || file.Length <= MaxBytes)
        {
            return;
        }

        var backup = $"{_path}.1";
        File.Delete(backup);
        File.Move(_path, backup);
    }
}
