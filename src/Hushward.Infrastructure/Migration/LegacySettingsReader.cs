using System.Text.Json;

namespace Hushward.Infrastructure.Migration;

public sealed record LegacySettings(
    bool Enabled,
    string StartTime,
    int IdleThresholdMinutes,
    bool ContextChecksEnabled);

public static class LegacySettingsReader
{
    public static async Task<LegacySettings?> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<LegacySettings>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
