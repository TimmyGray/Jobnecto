using Microsoft.EntityFrameworkCore;
using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Application.Exceptions;

namespace JobNecto.Infrastructure.Repositories;

public abstract class BaseRepository<T> : IRepository<T>
    where T : BaseEntity
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
            throw new NotFoundException($"Entity with id {id} not found");
        }

        _dbSet.Remove(entity);
        return id;
    }

    public virtual async Task<PagedResult<T>> GetAsync(PagedQuery pagedQuery, CancellationToken ct)
    {
        if (pagedQuery.PageSize <= 0)
        {
            throw new ArgumentOutOfRangeException("PageSize must be greater than 0.");
        }

        var query = _dbSet.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        query = query.OrderByDescending(e => e.UpdatedAt).ThenByDescending(e => e.Id);

        if (pagedQuery.LastSeenId is not null)
        {
            var cursorExists = await _dbSet
                .AsNoTracking()
                .AnyAsync(
                    e =>
                        e.Id == pagedQuery.LastSeenId
                        || e.UpdatedAt == pagedQuery.LastSeenUpdatedAt,
                    ct
                );

            if (cursorExists)
            {
                query = query.Where(e =>
                    e.UpdatedAt < pagedQuery.LastSeenUpdatedAt
                    || (e.UpdatedAt == pagedQuery.LastSeenUpdatedAt && e.Id < pagedQuery.LastSeenId)
                );
            }
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

    public virtual async Task<T> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbSet.FindAsync([id], ct);
        if (entity == null)
        {
            throw new NotFoundException($"Entity with id {id} not found");
        }
        return entity;
    }

    public virtual async Task<bool> IsExistsAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await _dbSet.AnyAsync(e => e.Id == id, ct);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
