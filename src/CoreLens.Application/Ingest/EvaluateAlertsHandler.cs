using CoreLens.Application.Abstractions;
using CoreLens.Contracts.Dtos;
using CoreLens.Domain;

namespace CoreLens.Application.Ingest;

public sealed class EvaluateAlertsHandler
{
    private readonly IAlertRuleRepository _rules;
    private readonly IAlertHistoryRepository _history;
    private readonly IMetricsBroadcaster _broadcaster;
    private readonly AlertEvaluationState _state;

    public EvaluateAlertsHandler(
        IAlertRuleRepository rules,
        IAlertHistoryRepository history,
        IMetricsBroadcaster broadcaster,
        AlertEvaluationState state)
    {
        _rules = rules;
        _history = history;
        _broadcaster = broadcaster;
        _state = state;
    }

    public async Task EvaluateAsync(
        Guid computerId,
        IReadOnlyList<MetricSample> samples,
        IReadOnlyDictionary<string, Component> componentsByKey,
        CancellationToken cancellationToken)
    {
        var rules = await _rules.ListEnabledAsync(cancellationToken);
        if (rules.Count == 0 || samples.Count == 0)
        {
            return;
        }

        var componentsById = componentsByKey.Values.ToDictionary(c => c.Id);
        var now = samples[0].Time;

        foreach (var sample in samples)
        {
            if (!componentsById.TryGetValue(sample.ComponentId, out var component))
            {
                continue;
            }

            foreach (var rule in rules)
            {
                if (!Matches(rule, computerId, component, sample.Name))
                {
                    continue;
                }

                var key = (rule.Id, sample.ComponentId);
                if (!IsBreached(rule, sample.Value))
                {
                    _state.ClearBreach(key);
                    continue;
                }

                _state.TryBeginBreach(key, now, out var since);
                if (now - since < TimeSpan.FromSeconds(Math.Max(rule.DurationSeconds, 0)))
                {
                    continue;
                }

                if (!_state.TryMarkFired(key, now, TimeSpan.FromMinutes(5)))
                {
                    continue;
                }

                var history = new AlertHistory
                {
                    Id = Guid.NewGuid(),
                    AlertRuleId = rule.Id,
                    ComputerId = computerId,
                    ComponentId = component.Id,
                    Time = now,
                    Severity = rule.Severity,
                    Value = sample.Value,
                    Message = BuildMessage(component, sample, rule)
                };

                await _history.AddAsync(history, cancellationToken);
                await _broadcaster.BroadcastAlertAsync(new AlertEventDto
                {
                    Id = history.Id,
                    ComputerId = computerId,
                    ComponentId = component.Id,
                    ComponentStableKey = component.StableKey,
                    Time = now,
                    Message = history.Message,
                    Severity = rule.Severity.ToString().ToLowerInvariant(),
                    Value = sample.Value,
                    MetricName = sample.Name
                }, cancellationToken);
            }
        }
    }

    private static bool Matches(AlertRule rule, Guid computerId, Component component, string metricName)
    {
        if (rule.ComputerId is Guid cid && cid != computerId)
        {
            return false;
        }

        if (rule.ComponentId is Guid compId && compId != component.Id)
        {
            return false;
        }

        if (rule.ComponentType is HardwareType type && type != component.Type)
        {
            return false;
        }

        return string.Equals(rule.MetricName, metricName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBreached(AlertRule rule, double value) => rule.Operator switch
    {
        AlertOperator.GreaterThan => value > rule.Threshold,
        AlertOperator.GreaterOrEqual => value >= rule.Threshold,
        AlertOperator.LessThan => value < rule.Threshold,
        AlertOperator.LessOrEqual => value <= rule.Threshold,
        _ => false
    };

    private static string BuildMessage(Component component, MetricSample sample, AlertRule rule)
    {
        var severity = rule.Severity.ToString().ToLowerInvariant();
        return $"{severity}: {component.StableKey} {sample.Name}={sample.Value:0.##} (threshold {rule.Operator} {rule.Threshold} for {rule.DurationSeconds}s)";
    }
}
