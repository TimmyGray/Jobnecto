using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;
namespace JobNecto.Infrastructure.Repositories;

public abstract class EditableRepository<T> : BaseRepository<T>, IEditableRepository<T> where T : BaseEntity
{
    public EditableRepository(AppDbContext context) : base(context)
    {
    }

    public Task<T> UpdateAsync(T entity, CancellationToken ct)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }
}
