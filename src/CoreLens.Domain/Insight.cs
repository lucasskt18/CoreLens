namespace CoreLens.Domain;

public sealed class Insight
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Provider { get; init; } = "none";
}
