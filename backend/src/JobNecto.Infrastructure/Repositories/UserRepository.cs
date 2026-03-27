public class UserRepository : BaseRepository<User>, IEditableRepository<User>
{
    public UserRepository(AppDbContext context)
        : base(context) { }

    public Task<User> UpdateAsync(User entity, CancellationToken ct)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }
}
