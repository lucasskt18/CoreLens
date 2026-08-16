using CoreLens.Application.Abstractions;
using CoreLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Timescale")
            ?? "Host=localhost;Port=5432;Database=corelens;Username=corelens;Password=corelens";

        services.AddDbContext<CoreLensDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IComputerRepository, ComputerRepository>();
        services.AddScoped<IComponentRepository, ComponentRepository>();
        services.AddScoped<IMetricSampleRepository, MetricSampleRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IAlertHistoryRepository, AlertHistoryRepository>();
        services.AddScoped<TimescaleSetup>();

        services.AddSingleton<MetricSampleBuffer>();
        services.AddSingleton<IMetricSampleBuffer>(sp => sp.GetRequiredService<MetricSampleBuffer>());
        services.AddHostedService<MetricPersistenceWorker>();

        return services;
    }
}
