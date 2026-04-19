namespace JobNecto.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository UserRepository { get; }
    IVacancyRepository VacancyRepository { get; }
    IEditableRepository<CoverLetter> CoverLetterRepository { get; }
    IEditableRepository<Resume> ResumeRepository { get; }
    IEditableRepository<Education> EducationRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
}