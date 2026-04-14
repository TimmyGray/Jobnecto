using Microsoft.EntityFrameworkCore;

public abstract class EditableRepository<T> : BaseRepository<T>, IEditableRepository<T> where T : BaseEntity
{
    public EditableRepository(DbContext context) : base(context)
    {
    }

    public Task<T> UpdateAsync(T entity, CancellationToken ct)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }
}
