using JobNecto.Domain.Entities;

namespace JobNecto.Application.Interfaces;

public interface IEditableRepository<T> : IRepository<T> where T : BaseEntity
{
    Task<T> UpdateAsync(T entity, CancellationToken ct);
}