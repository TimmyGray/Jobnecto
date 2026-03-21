public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<User> UserRepository { get; }
    IVacancyRepository VacancyRepository { get; }
    IRepository<CoverLetter> CoverLetterRepository { get; }
    IRepository<Resume> ResumeRepository { get; }
    IRepository<Education> EducationRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
}