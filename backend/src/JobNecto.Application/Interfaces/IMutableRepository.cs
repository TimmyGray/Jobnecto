using JobNecto.Domain.Entities;

namespace JobNecto.Application.Interfaces;

/// <summary>
/// Role-based composition for entities that support all write operations: update and soft delete.
/// <see cref="IEditableRepository{T}"/> alone does NOT imply soft delete capability.
/// </summary>
/// <typeparam name="T">An entity type that extends <see cref="SoftDeletableEntity"/>.</typeparam>
public interface IMutableRepository<T> : IEditableRepository<T>, ISoftDeleteRepository<T>
    where T : SoftDeletableEntity
{
}
