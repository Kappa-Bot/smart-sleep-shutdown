namespace SmartSleepShutdown.Infrastructure.System;

public sealed class SustainedSignalGate
{
    private readonly int _requiredActiveSamples;
    private readonly int _requiredClearSamples;
    private readonly TimeSpan _staleAfter;
    private DateTimeOffset? _lastObservedAt;
    private int _activeSamples;
    private int _clearSamples;

    public SustainedSignalGate(int requiredActiveSamples, int requiredClearSamples, TimeSpan staleAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(requiredActiveSamples, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(requiredClearSamples, 1);

        _requiredActiveSamples = requiredActiveSamples;
        _requiredClearSamples = requiredClearSamples;
        _staleAfter = staleAfter;
    }

    public bool IsActive { get; private set; }

    public bool Observe(bool isActiveSample, DateTimeOffset now)
    {
        if (_lastObservedAt is not null && now - _lastObservedAt.Value > _staleAfter)
        {
            Reset();
        }

        _lastObservedAt = now;

        if (isActiveSample)
        {
            _activeSamples++;
            _clearSamples = 0;
            if (_activeSamples >= _requiredActiveSamples)
            {
                IsActive = true;
            }
        }
        else
        {
            _clearSamples++;
            _activeSamples = 0;
            if (_clearSamples >= _requiredClearSamples)
            {
                IsActive = false;
            }
        }

        return IsActive;
    }

    private void Reset()
    {
        IsActive = false;
        _activeSamples = 0;
        _clearSamples = 0;
    }
}
