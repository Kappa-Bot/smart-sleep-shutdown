using Hushward.Core.Protections;

namespace Hushward.Core.Tests.Protections;

public sealed class ProtectionPolicyTests
{
    [Fact]
    public void Unknown_required_detector_is_critical()
    {
        var signal = ProtectionSignal.Unknown(
            detectorId: "audio",
            category: ProtectionCategory.Media,
            observedAt: DateTimeOffset.Parse("2026-07-23T01:00:00Z"),
            reasonCode: "detector.timeout");

        var summary = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T01:00:01Z"));

        summary.HasCriticalBlock.ShouldBeTrue();
        Assert.Contains(signal, summary.Critical);
    }

    [Fact]
    public void Expired_temporary_signal_does_not_block()
    {
        var signal = new ProtectionSignal(
            "network",
            ProtectionCategory.Transfer,
            ProtectionClass.Temporary,
            ObservationState.Active,
            "transfer.sustained",
            "Protection.Transfer",
            DateTimeOffset.Parse("2026-07-23T00:00:00Z"),
            DateTimeOffset.Parse("2026-07-23T00:05:00Z"),
            null);

        var summary = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T00:06:00Z"));

        summary.HasTemporaryBlock.ShouldBeFalse();
        Assert.Contains(signal, summary.Expired);
    }

    [Fact]
    public void Contextual_signal_never_blocks_on_its_own()
    {
        var signal = new ProtectionSignal(
            "cpu",
            ProtectionCategory.ResourceWorkload,
            ProtectionClass.Contextual,
            ObservationState.Active,
            "cpu.moderate",
            "Protection.ResourceWorkload.Contextual",
            DateTimeOffset.Parse("2026-07-23T00:00:00Z"),
            null,
            "Compilacion");

        var summary = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T00:01:00Z"));

        summary.HasCriticalBlock.ShouldBeFalse();
        summary.HasTemporaryBlock.ShouldBeFalse();
        Assert.Contains(signal, summary.Contextual);
    }

    [Fact]
    public void Inactive_signal_is_separated_from_blocking_evidence()
    {
        var signal = new ProtectionSignal(
            "media",
            ProtectionCategory.Media,
            ProtectionClass.Temporary,
            ObservationState.Inactive,
            "media.inactive",
            "Protection.Media.Inactive",
            DateTimeOffset.Parse("2026-07-23T00:00:00Z"),
            null,
            null);

        var summary = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T00:01:00Z"));

        summary.HasTemporaryBlock.ShouldBeFalse();
        Assert.Contains(signal, summary.Inactive);
    }

    [Fact]
    public void Signal_contract_has_no_forbidden_content_fields()
    {
        var forbiddenFragments = new[]
        {
            "Title",
            "Url",
            "BrowserTab",
            "Document",
            "FilePath",
            "CommandLine",
            "Clipboard",
            "Screenshot",
            "AudioContent",
            "VideoContent",
            "Keystroke"
        };
        var propertyNames = typeof(ProtectionSignal)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        foreach (var forbidden in forbiddenFragments)
        {
            Assert.DoesNotContain(propertyNames, name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }
}
