
using Microsoft.EntityFrameworkCore;

public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T> CreateAsync(T entity, CancellationToken ct)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual async Task<Guid> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity == null)
        {
            throw new Exception($"Entity with id {id} not found");
        }

        _dbSet.Remove(entity);
        return id;
    }

    public virtual async Task<PagedResult<T>> GetAsync(PagedQuery pagedQuery, CancellationToken ct)
    {
        var query = _dbSet.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        query = query
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.Id);

        if (pagedQuery.LastSeenId is not null)
        {
            query = query
            .Where(e => e.UpdatedAt < pagedQuery.LastSeenUpdatedAt
                || (e.UpdatedAt == pagedQuery.LastSeenUpdatedAt && e.Id < pagedQuery.LastSeenId));
        }

        var take = Math.Max(1, pagedQuery.PageSize);

        var pagePlusOne = await query.Take(take + 1).ToListAsync(ct);

        var items = pagePlusOne.Take(take).ToList();

        var hasNext = pagePlusOne.Count > take;

        Guid? nextLastSeenId = items.Count > 0 ? items[^1].Id : null;
        DateTime? nextLastSeenUpdatedAt = items.Count > 0 ? items[^1].UpdatedAt : null;

        return new PagedResult<T>(
            items,
            totalCount,
            nextLastSeenId,
            nextLastSeenUpdatedAt,
            pagedQuery.PageSize,
            hasNext
        );


    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbSet.FindAsync([id], ct);
    }

    public virtual async Task<bool> IsExistsAsync(Guid id, CancellationToken ct)
    {
        return await GetByIdAsync(id, ct) is not null;
    }
}