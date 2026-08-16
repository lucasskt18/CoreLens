using CoreLens.Application.Abstractions;
using CoreLens.Contracts;
using CoreLens.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreLens.Infrastructure.Persistence;

public sealed class AlertRuleRepository : IAlertRuleRepository
{
    private readonly CoreLensDbContext _db;

    public AlertRuleRepository(CoreLensDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken) =>
        await _db.AlertRules.AsNoTracking().Where(r => r.IsEnabled).ToListAsync(cancellationToken);

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        if (await _db.AlertRules.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.AlertRules.AddRange(
            new AlertRule
            {
                Id = Guid.NewGuid(),
                ComponentType = HardwareType.Cpu,
                MetricName = MetricKeys.LoadPct,
                Operator = AlertOperator.GreaterThan,
                Threshold = 90,
                DurationSeconds = 30,
                Severity = AlertSeverity.Warning,
                IsEnabled = true
            },
            new AlertRule
            {
                Id = Guid.NewGuid(),
                ComponentType = HardwareType.Cpu,
                MetricName = MetricKeys.TempC,
                Operator = AlertOperator.GreaterThan,
                Threshold = 80,
                DurationSeconds = 0,
                Severity = AlertSeverity.Critical,
                IsEnabled = true
            },
            new AlertRule
            {
                Id = Guid.NewGuid(),
                ComponentType = HardwareType.Ram,
                MetricName = MetricKeys.UsedPct,
                Operator = AlertOperator.GreaterThan,
                Threshold = 90,
                DurationSeconds = 30,
                Severity = AlertSeverity.Warning,
                IsEnabled = true
            });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
