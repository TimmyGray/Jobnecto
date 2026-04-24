namespace JobNecto.Domain.ValueObjects;

public record PagedQuery
{
    public Guid? UserId { get; init; } = null;
    public Guid? LastSeenId { get; init; } = null;
    public DateTime? LastSeenUpdatedAt { get; init; } = null;
    public int PageSize { get; init; } = 20;
}

public record PagedResult<BaseEntity>(
    IReadOnlyList<BaseEntity> Items,
    int TotalCount,
    Guid? LastSeenId,
    DateTime? LastSeenUpdatedAt,
    int PageSize,
    bool HasNext
)
{
    public int TotalPages
    {
        get
        {
            if (PageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PageSize),
                    "PageSize must be greater than 0."
                );
            }

            return (int)Math.Ceiling(TotalCount / (double)PageSize);
        }
    }
}
