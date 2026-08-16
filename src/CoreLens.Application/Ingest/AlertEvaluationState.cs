using CoreLens.Domain;

namespace CoreLens.Application.Ingest;

public sealed class AlertEvaluationState
{
    private readonly Dictionary<(Guid RuleId, Guid ComponentId), DateTimeOffset> _breachingSince = new();
    private readonly Dictionary<(Guid RuleId, Guid ComponentId), DateTimeOffset> _lastFired = new();
    private readonly object _gate = new();

    public bool TryBeginBreach((Guid RuleId, Guid ComponentId) key, DateTimeOffset now, out DateTimeOffset since)
    {
        lock (_gate)
        {
            if (!_breachingSince.TryGetValue(key, out since))
            {
                since = now;
                _breachingSince[key] = now;
            }

            return true;
        }
    }

    public void ClearBreach((Guid RuleId, Guid ComponentId) key)
    {
        lock (_gate)
        {
            _breachingSince.Remove(key);
        }
    }

    public bool TryMarkFired((Guid RuleId, Guid ComponentId) key, DateTimeOffset now, TimeSpan cooldown)
    {
        lock (_gate)
        {
            if (_lastFired.TryGetValue(key, out var last) && now - last < cooldown)
            {
                return false;
            }

            _lastFired[key] = now;
            return true;
        }
    }
}
