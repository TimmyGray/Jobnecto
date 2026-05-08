using Microsoft.EntityFrameworkCore;
using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Application.Exceptions;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

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

        var userIdProperty = _context.Model.FindEntityType(typeof(T))?.FindProperty("UserId");

        if (pagedQuery.UserId is Guid userId && userIdProperty is not null)
        {
            if (userIdProperty.ClrType == typeof(Guid))
            {
                query = query.Where(entity => EF.Property<Guid>(entity, "UserId") == userId);
            }
            else if (userIdProperty.ClrType == typeof(Guid?))
            {
                query = query.Where(entity => EF.Property<Guid?>(entity, "UserId") == userId);
            }
        }

        query = ApplyAdditionalFilters(query, pagedQuery);

        var totalCount = await query.CountAsync(ct);

        query = query.OrderByDescending(e => e.UpdatedAt).ThenByDescending(e => e.Id);

        if (pagedQuery.LastSeenId is Guid lastSeenId)
        {
            var cursorExists = pagedQuery.LastSeenUpdatedAt is DateTime lastSeenUpdatedAt
                ? await query.AnyAsync(e => e.Id == lastSeenId && e.UpdatedAt == lastSeenUpdatedAt, ct)
                : await query.AnyAsync(e => e.Id == lastSeenId, ct);

            if (cursorExists)
            {
                var effectiveLastSeenUpdatedAt = pagedQuery.LastSeenUpdatedAt
                    ?? await query
                        .Where(e => e.Id == lastSeenId)
                        .Select(e => (DateTime?)e.UpdatedAt)
                        .FirstOrDefaultAsync(ct);

                if (effectiveLastSeenUpdatedAt is DateTime cursorUpdatedAt)
                {
                    query = query.Where(e =>
                        e.UpdatedAt < cursorUpdatedAt
                        || (e.UpdatedAt == cursorUpdatedAt && e.Id < lastSeenId)
                    );
                }
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
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity == null)
        {
            throw new NotFoundException($"Entity with id {id} not found");
        }
        return entity;
    }

    /// <summary>
    /// Override in a derived repository to apply additional entity-specific filters.
    /// Called after UserId scoping and before CountAsync/ordering.
    /// </summary>
    protected virtual IQueryable<T> ApplyAdditionalFilters(IQueryable<T> query, PagedQuery pagedQuery)
    {
        return query;
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
