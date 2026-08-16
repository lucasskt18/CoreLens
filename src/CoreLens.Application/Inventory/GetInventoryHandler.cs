using System.Text.Json;
using CoreLens.Application.Abstractions;
using CoreLens.Contracts.Dtos;
using CoreLens.Domain;

namespace CoreLens.Application.Inventory;

public sealed class GetInventoryHandler
{
    private readonly IComputerRepository _computers;
    private readonly IComponentRepository _components;

    public GetInventoryHandler(IComputerRepository computers, IComponentRepository components)
    {
        _computers = computers;
        _components = components;
    }

    public async Task<IReadOnlyList<ComputerSummaryDto>> ListComputersAsync(CancellationToken cancellationToken)
    {
        var computers = await _computers.ListAsync(cancellationToken);
        return computers.Select(ToSummary).ToList();
    }

    public async Task<InventoryDto?> GetAsync(Guid computerId, CancellationToken cancellationToken)
    {
        var computer = await _computers.GetAsync(computerId, cancellationToken);
        if (computer is null)
        {
            return null;
        }

        var components = await _components.ListByComputerAsync(computerId, cancellationToken);
        return new InventoryDto
        {
            Computer = ToSummary(computer),
            Components = components.Select(c => new ComponentDto
            {
                Id = c.Id,
                StableKey = c.StableKey,
                Type = c.Type.ToContract(),
                Manufacturer = c.Manufacturer,
                Model = c.Model,
                Specs = DeserializeSpecs(c.SpecsJson)
            }).ToList()
        };
    }

    private static ComputerSummaryDto ToSummary(Computer computer) => new()
    {
        Id = computer.Id,
        Hostname = computer.Hostname,
        OsVersion = computer.OsVersion,
        AgentVersion = computer.AgentVersion,
        LastSeenAt = computer.LastSeenAt
    };

    private static Dictionary<string, string>? DeserializeSpecs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }
}
