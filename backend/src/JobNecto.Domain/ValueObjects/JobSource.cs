public record JobSource
{
    public required string Name { get; init; }
    public string? Url { get; init; }
}