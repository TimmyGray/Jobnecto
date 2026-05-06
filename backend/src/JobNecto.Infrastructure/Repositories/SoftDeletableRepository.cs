using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Infrastructure.Persistance;

namespace JobNecto.Infrastructure.Repositories;

/// <summary>
/// Abstract repository base for entities that support soft deletion.
/// Encapsulates the <c>IsDeleted = true; DeletedAt = DateTime.UtcNow</c> flag-setting logic
/// and stages the change via <c>_dbSet.Update</c>.
/// </summary>
/// <typeparam name="T">An entity type that extends <see cref="SoftDeletableEntity"/>.</typeparam>
public abstract class SoftDeletableRepository<T> : EditableRepository<T>, IMutableRepository<T>
    where T : SoftDeletableEntity
{
    protected SoftDeletableRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Marks <paramref name="entity"/> as soft-deleted and stages the update for the next
    /// <c>SaveChangesAsync</c> call. UTC timestamp is set here, not in the handler.
    /// </summary>
    public Task SoftDeleteAsync(T entity, CancellationToken ct)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }
}
