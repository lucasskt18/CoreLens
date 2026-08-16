using CoreLens.Application.Abstractions;
using CoreLens.Application.Alerts;
using CoreLens.Application.History;
using CoreLens.Application.Ingest;
using CoreLens.Application.Insights;
using CoreLens.Application.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<AlertEvaluationState>();
        services.AddSingleton<IInsightProvider, NullInsightProvider>();
        services.AddScoped<EvaluateAlertsHandler>();
        services.AddScoped<IngestMetricsHandler>();
        services.AddScoped<GetInventoryHandler>();
        services.AddScoped<GetHistoryHandler>();
        services.AddScoped<GetAlertsHandler>();
        services.AddScoped<GetInsightsHandler>();
        return services;
    }
}
