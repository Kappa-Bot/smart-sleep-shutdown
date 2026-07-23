namespace Hushward.Infrastructure.Tests.Persistence;

public static class TestJson
{
    public const string ValidEnvelope = """
    {
      "schemaVersion": 2,
      "productVersion": "0.1.0-test",
      "writtenAt": "2026-07-23T01:00:00+00:00",
      "settings": {
        "routines": [],
        "tonightOverride": null,
        "protectionRules": [],
        "privacy": { "historyRetentionDays": 14 },
        "uiPreferences": { "reducedMotion": false },
        "installationState": { "startWithWindows": false, "wakeTasksEnabled": false },
        "requiresMigrationReview": false
      }
    }
    """;

    public const string ValidEnvelopeVersionB = """
    {
      "schemaVersion": 2,
      "productVersion": "0.1.1-test",
      "writtenAt": "2026-07-23T02:00:00+00:00",
      "settings": {
        "routines": [],
        "tonightOverride": null,
        "protectionRules": [],
        "privacy": { "historyRetentionDays": 14 },
        "uiPreferences": { "reducedMotion": false },
        "installationState": { "startWithWindows": false, "wakeTasksEnabled": false },
        "requiresMigrationReview": false
      }
    }
    """;

    public const string FutureEnvelope = """
    {
      "schemaVersion": 99,
      "productVersion": "99.0.0",
      "writtenAt": "2026-07-23T01:00:00+00:00",
      "settings": {
        "routines": [],
        "tonightOverride": null,
        "protectionRules": [],
        "privacy": { "historyRetentionDays": 14 },
        "uiPreferences": { "reducedMotion": false },
        "installationState": { "startWithWindows": false, "wakeTasksEnabled": false },
        "requiresMigrationReview": false
      }
    }
    """;
}
