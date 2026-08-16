using CoreLens.Agent;
using CoreLens.Agent.Collectors;
using CoreLens.Agent.Transport;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<InventoryCollector>();
builder.Services.AddSingleton<IMetricCollector, CpuRamCollector>();
builder.Services.AddSingleton<IMetricCollector, NetworkCollector>();
builder.Services.AddSingleton<IMetricCollector, DiskCollector>();
builder.Services.AddSingleton<IMetricCollector, LibreHardwareMonitorCollector>();
builder.Services.AddHttpClient<ApiIngestClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Agent:ApiBaseUrl"] ?? "http://localhost:5080";
    var token = config["Agent:Token"] ?? "dev-local-token-change-me";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("X-Agent-Token", token);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHostedService<HardwareAgentWorker>();

var host = builder.Build();
host.Run();
