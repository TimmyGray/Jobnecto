
using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> UserRepository => new UserRepository(_context);

    public IVacancyRepository VacancyRepository => new VacancyRepository(_context);

    public IRepository<CoverLetter> CoverLetterRepository => new CoverLetterRepository(_context);

    public IRepository<Resume> ResumeRepository => new ResumeRepository(_context);

    public IRepository<Education> EducationRepository => new EducationRepository(_context);

    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Transaction not started");
        }

        try
        {
            await SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
        }
        catch (System.Exception)
        {
            await _transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
        await _context.DisposeAsync();
    }

    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await DisposeTransactionAsync();
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
