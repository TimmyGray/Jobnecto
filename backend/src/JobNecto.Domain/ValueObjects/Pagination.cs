public record PagedQuery
{
    public Guid? LastSeenId { get; init; } = null;
    public DateTime? LastSeenUpdatedAt { get; init; } = null;
    public int PageSize { get; init; } = 20;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, Guid? LastSeenId, DateTime? LastSeenUpdatedAt, int PageSize, bool HasNext)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
