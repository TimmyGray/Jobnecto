public interface IEditableRepository<T> where T : BaseEntity
{
    Task<T> UpdateAsync(T entity, CancellationToken ct);
}