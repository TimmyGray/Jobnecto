
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> UserRepository => new UserRepository(_context);

    public IVacancyRepository VacancyRepository => throw new NotImplementedException();

    public IRepository<CoverLetter> CoverLetterRepository => throw new NotImplementedException();

    public IRepository<Resume> ResumeRepository => throw new NotImplementedException();

    public IRepository<Education> EducationRepository => throw new NotImplementedException();

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
