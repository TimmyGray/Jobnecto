
using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using JobNecto.Infrastructure.Repositories;

namespace JobNecto.Infrastructure.Persistance;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _userRepository;
    private IVacancyRepository? _vacancyRepository;
    private IMutableRepository<CoverLetter>? _coverLetterRepository;
    private IMutableRepository<Resume>? _resumeRepository;
    private IMutableRepository<Education>? _educationRepository;
    private IMutableRepository<CoverLetterTemplate>? _coverLetterTemplateRepository;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository UserRepository => 
        _userRepository ??= new UserRepository(_context);

    public IVacancyRepository VacancyRepository => 
        _vacancyRepository ??= new VacancyRepository(_context);

    public IMutableRepository<CoverLetter> CoverLetterRepository =>
        _coverLetterRepository ??= new CoverLetterRepository(_context);

    public IMutableRepository<Resume> ResumeRepository =>
        _resumeRepository ??= new ResumeRepository(_context);

    public IMutableRepository<Education> EducationRepository =>
        _educationRepository ??= new EducationRepository(_context);

    public IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository =>
        _coverLetterTemplateRepository ??= new CoverLetterTemplateRepository(_context);

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
        catch (Exception)
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
            _transaction = null;
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
