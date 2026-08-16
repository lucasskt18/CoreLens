namespace CoreLens.Contracts.Dtos;

public sealed class InsightDto
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Provider { get; set; } = "none";
}
