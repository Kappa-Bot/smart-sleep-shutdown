using Hushward.Application.Abstractions;
using Hushward.Application.Results;
using Hushward.Core.Protections;

namespace Hushward.Infrastructure.Detectors;

public abstract class DetectorBase : IProtectionDetector
{
    protected DetectorBase(string id, ProtectionCategory category)
    {
        Id = id;
        Category = category;
    }

    public string Id { get; }

    protected ProtectionCategory Category { get; }

    public async Task<OperationResult<ProtectionSignal>> ObserveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var signal = await ObserveCoreAsync(cancellationToken).ConfigureAwait(false);
            return OperationResult<ProtectionSignal>.Success(signal);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<ProtectionSignal>.Success(
                ProtectionSignal.Unknown(Id, Category, DateTimeOffset.Now, "detector.failure"));
        }
    }

    protected abstract Task<ProtectionSignal> ObserveCoreAsync(CancellationToken cancellationToken);

    protected ProtectionSignal Active(
        ProtectionClass protectionClass,
        string reasonCode,
        string explanationKey,
        DateTimeOffset observedAt,
        DateTimeOffset? expiresAt = null,
        string? friendlyApplicationLabel = null) =>
        new(
            Id,
            Category,
            protectionClass,
            ObservationState.Active,
            reasonCode,
            explanationKey,
            observedAt,
            expiresAt,
            DetectorEvidenceSanitizer.FriendlyLabel(friendlyApplicationLabel));

    protected ProtectionSignal Inactive(DateTimeOffset observedAt) =>
        new(
            Id,
            Category,
            ProtectionClass.Contextual,
            ObservationState.Inactive,
            "protection.inactive",
            "Protection.Inactive",
            observedAt,
            null,
            null);

    protected ProtectionSignal Unknown(DateTimeOffset observedAt, string reasonCode = "detector.unavailable") =>
        ProtectionSignal.Unknown(Id, Category, observedAt, reasonCode);
}
