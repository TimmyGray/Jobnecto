using JobNecto.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class UserRepository : SoftDeletableRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context)
        : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByLoginAsync(string login, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Login == login, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email, ct);
    }

    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken ct = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Login == login, ct);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Phone == phone, ct);
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Phone == phone, ct);
    }
}
