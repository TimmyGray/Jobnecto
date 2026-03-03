public interface IEditableRepository<T>
{
    Task<T> UpdateAsync(T entity, CancellationToken ct);

}