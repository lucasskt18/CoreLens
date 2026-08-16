using CoreLens.Application.Abstractions;
using CoreLens.Contracts.Dtos;
using CoreLens.Domain;

namespace CoreLens.Application.Alerts;

public sealed class GetAlertsHandler
{
    private readonly IAlertHistoryRepository _history;
    private readonly IAlertRuleRepository _rules;
    private readonly IComponentRepository _components;

    public GetAlertsHandler(
        IAlertHistoryRepository history,
        IAlertRuleRepository rules,
        IComponentRepository components)
    {
        _history = history;
        _rules = rules;
        _components = components;
    }

    public async Task<IReadOnlyList<AlertEventDto>> ListHistoryAsync(Guid computerId, int take, CancellationToken cancellationToken)
    {
        var rows = await _history.ListAsync(computerId, take, cancellationToken);
        var map = await _components.GetMapAsync(computerId, cancellationToken);
        var byId = map.Values.ToDictionary(c => c.Id, c => c.StableKey);

        return rows.Select(r => new AlertEventDto
        {
            Id = r.Id,
            ComputerId = r.ComputerId,
            ComponentId = r.ComponentId,
            ComponentStableKey = byId.GetValueOrDefault(r.ComponentId, r.ComponentId.ToString()),
            Time = r.Time,
            Message = r.Message,
            Severity = r.Severity.ToString().ToLowerInvariant(),
            Value = r.Value
        }).ToList();
    }

    public async Task<IReadOnlyList<AlertRuleDto>> ListRulesAsync(CancellationToken cancellationToken)
    {
        var rules = await _rules.ListEnabledAsync(cancellationToken);
        return rules.Select(r => new AlertRuleDto
        {
            Id = r.Id,
            ComponentType = r.ComponentType?.ToContract(),
            MetricName = r.MetricName,
            Operator = r.Operator.ToString(),
            Threshold = r.Threshold,
            DurationSeconds = r.DurationSeconds,
            Severity = r.Severity.ToString().ToLowerInvariant(),
            IsEnabled = r.IsEnabled
        }).ToList();
    }
}
