
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> UserRepository => new UserRepository(_context);

    public IVacancyRepository VacancyRepository => new VacancyRepository(_context);

    public IRepository<CoverLetter> CoverLetterRepository => new CoverLetterRepository(_context);

    public IRepository<Resume> ResumeRepository => new ResumeRepository(_context);

    public IRepository<Education> EducationRepository => new EducationRepository(_context);

    public Task BeginTransactionAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task CommitTransactionAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public Task RollbackTransactionAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
