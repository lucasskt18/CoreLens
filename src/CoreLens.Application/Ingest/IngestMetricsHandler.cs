using System.Text.Json;
using CoreLens.Application.Abstractions;
using CoreLens.Contracts;
using CoreLens.Contracts.Dtos;
using CoreLens.Domain;

namespace CoreLens.Application.Ingest;

public sealed class IngestMetricsHandler
{
    private readonly IComputerRepository _computers;
    private readonly IComponentRepository _components;
    private readonly IMetricSampleBuffer _buffer;
    private readonly IMetricsBroadcaster _broadcaster;
    private readonly EvaluateAlertsHandler _alerts;

    public IngestMetricsHandler(
        IComputerRepository computers,
        IComponentRepository components,
        IMetricSampleBuffer buffer,
        IMetricsBroadcaster broadcaster,
        EvaluateAlertsHandler alerts)
    {
        _computers = computers;
        _components = components;
        _buffer = buffer;
        _broadcaster = broadcaster;
        _alerts = alerts;
    }

    public async Task HandleAsync(IngestRequest request, CancellationToken cancellationToken)
    {
        await _computers.UpsertAsync(new Computer
        {
            Id = request.ComputerId,
            Hostname = request.Hostname,
            OsVersion = request.OsVersion,
            AgentVersion = request.AgentVersion,
            LastSeenAt = request.Timestamp
        }, cancellationToken);

        if (request.Components is { Count: > 0 })
        {
            var entities = request.Components.Select(c => new Component
            {
                Id = Guid.NewGuid(),
                ComputerId = request.ComputerId,
                StableKey = c.StableKey,
                Type = HardwareTypeExtensions.FromContract(c.Type),
                Manufacturer = c.Manufacturer,
                Model = c.Model,
                SpecsJson = c.Specs is null ? null : JsonSerializer.Serialize(c.Specs)
            }).ToList();

            await _components.UpsertRangeAsync(request.ComputerId, entities, cancellationToken);
        }

        var map = await _components.GetMapAsync(request.ComputerId, cancellationToken);
        var missing = new List<Component>();

        foreach (var sample in request.Samples)
        {
            if (map.ContainsKey(sample.ComponentStableKey))
            {
                continue;
            }

            var stub = new Component
            {
                Id = Guid.NewGuid(),
                ComputerId = request.ComputerId,
                StableKey = sample.ComponentStableKey,
                Type = HardwareTypeExtensions.FromContract(HardwareTypes.FromStableKey(sample.ComponentStableKey))
            };
            missing.Add(stub);
            map = new Dictionary<string, Component>(map) { [stub.StableKey] = stub };
        }

        if (missing.Count > 0)
        {
            await _components.UpsertRangeAsync(request.ComputerId, missing, cancellationToken);
            map = await _components.GetMapAsync(request.ComputerId, cancellationToken);
        }

        var samples = new List<MetricSample>(request.Samples.Count);
        foreach (var dto in request.Samples)
        {
            if (!map.TryGetValue(dto.ComponentStableKey, out var component))
            {
                continue;
            }

            samples.Add(new MetricSample
            {
                Time = request.Timestamp,
                ComputerId = request.ComputerId,
                ComponentId = component.Id,
                Name = dto.Name,
                Value = dto.Value
            });
        }

        await _broadcaster.BroadcastMetricsAsync(new MetricsBroadcastDto
        {
            ComputerId = request.ComputerId,
            Timestamp = request.Timestamp,
            Samples = request.Samples
        }, cancellationToken);

        if (samples.Count > 0)
        {
            await _buffer.EnqueueAsync(samples, cancellationToken);
            await _alerts.EvaluateAsync(request.ComputerId, samples, map, cancellationToken);
        }
    }
}
