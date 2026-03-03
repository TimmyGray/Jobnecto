public record PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize {get; init; } = 20;
    public int Offset => (Page - 1)*PageSize;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => TotalPages > Page;
    public bool HasPrev => Page > 1;
}
    