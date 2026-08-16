using CoreLens.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreLens.Infrastructure.Persistence;

public sealed class CoreLensDbContext : DbContext
{
    public CoreLensDbContext(DbContextOptions<CoreLensDbContext> options) : base(options)
    {
    }

    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<MetricSample> MetricSamples => Set<MetricSample>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertHistory> AlertHistory => Set<AlertHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Computer>(e =>
        {
            e.ToTable("computers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Hostname).HasColumnName("hostname").HasMaxLength(256).IsRequired();
            e.Property(x => x.OsVersion).HasColumnName("os_version").HasMaxLength(256).IsRequired();
            e.Property(x => x.AgentVersion).HasColumnName("agent_version").HasMaxLength(64).IsRequired();
            e.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
            e.HasMany(x => x.Components).WithOne(x => x.Computer).HasForeignKey(x => x.ComputerId);
        });

        modelBuilder.Entity<Component>(e =>
        {
            e.ToTable("components");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ComputerId).HasColumnName("computer_id");
            e.Property(x => x.StableKey).HasColumnName("stable_key").HasMaxLength(256).IsRequired();
            e.Property(x => x.Manufacturer).HasColumnName("manufacturer").HasMaxLength(256);
            e.Property(x => x.Model).HasColumnName("model").HasMaxLength(256);
            e.Property(x => x.SpecsJson).HasColumnName("specs").HasColumnType("jsonb");
            e.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => new { x.ComputerId, x.StableKey }).IsUnique();
        });

        modelBuilder.Entity<MetricSample>(e =>
        {
            e.ToTable("metric_samples");
            e.HasKey(x => new { x.Time, x.ComputerId, x.ComponentId, x.Name });
            e.Property(x => x.Time).HasColumnName("time");
            e.Property(x => x.ComputerId).HasColumnName("computer_id");
            e.Property(x => x.ComponentId).HasColumnName("component_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
            e.Property(x => x.Value).HasColumnName("value");
            e.HasIndex(x => new { x.ComputerId, x.Name, x.Time });
            e.HasIndex(x => new { x.ComponentId, x.Time });
        });

        modelBuilder.Entity<AlertRule>(e =>
        {
            e.ToTable("alert_rules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ComputerId).HasColumnName("computer_id");
            e.Property(x => x.ComponentId).HasColumnName("component_id");
            e.Property(x => x.ComponentType).HasColumnName("component_type").HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.MetricName).HasColumnName("metric_name").HasMaxLength(64).IsRequired();
            e.Property(x => x.Operator).HasColumnName("comparison_op").HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Threshold).HasColumnName("threshold");
            e.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
            e.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        });

        modelBuilder.Entity<AlertHistory>(e =>
        {
            e.ToTable("alert_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AlertRuleId).HasColumnName("alert_rule_id");
            e.Property(x => x.ComputerId).HasColumnName("computer_id");
            e.Property(x => x.ComponentId).HasColumnName("component_id");
            e.Property(x => x.Time).HasColumnName("time");
            e.Property(x => x.Message).HasColumnName("message").HasMaxLength(1024).IsRequired();
            e.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Value).HasColumnName("value");
            e.HasIndex(x => new { x.ComputerId, x.Time });
        });
    }
}
