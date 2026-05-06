using JobNecto.Domain.Entities;

namespace JobNecto.Application.Interfaces;

/// <summary>
/// Repository contract for entities that support soft deletion.
/// Soft delete is a first-class operation independent of update (<see cref="IEditableRepository{T}"/>) semantics.
/// </summary>
/// <typeparam name="T">An entity type that extends <see cref="SoftDeletableEntity"/>.</typeparam>
public interface ISoftDeleteRepository<T> : IRepository<T>
    where T : SoftDeletableEntity
{
    /// <summary>
    /// Marks <paramref name="entity"/> as soft-deleted by setting <c>IsDeleted</c> and <c>DeletedAt</c>,
    /// then stages the change for persistence on the next <c>SaveChangesAsync</c> call.
    /// </summary>
    Task SoftDeleteAsync(T entity, CancellationToken ct);
}
