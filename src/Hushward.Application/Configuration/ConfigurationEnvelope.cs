using System.Text.Json.Serialization;

namespace Hushward.Application.Configuration;

public sealed record ConfigurationEnvelope(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("productVersion")] string ProductVersion,
    [property: JsonPropertyName("writtenAt")] DateTimeOffset WrittenAt,
    [property: JsonPropertyName("settings")] HushwardConfiguration Settings)
{
    public const int CurrentSchemaVersion = 2;

    public static ConfigurationEnvelope SafeMode(DateTimeOffset now) => new(
        CurrentSchemaVersion,
        "recovery",
        now,
        HushwardConfiguration.SafeMode());

    public bool IsSupported() => SchemaVersion == CurrentSchemaVersion;
}
