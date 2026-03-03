public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<T>> GetAsync(PagedQuery pagedQuery, CancellationToken ct);
    Task<T> CreateAsync(T entity, CancellationToken ct);
    Task<Guid> DeleteAsync(Guid id, CancellationToken ct);
    Task<Boolean> IsExistsAsync(Guid id);
}