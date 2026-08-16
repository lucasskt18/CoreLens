namespace CoreLens.Contracts.Dtos;

public sealed class HistoryPointDto
{
    public DateTimeOffset Time { get; set; }
    public string ComponentStableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
}

public sealed class HistoryResponseDto
{
    public Guid ComputerId { get; set; }
    public string Bucket { get; set; } = "1s";
    public IReadOnlyList<HistoryPointDto> Points { get; set; } = [];
}
