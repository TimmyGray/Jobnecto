using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Application.Interfaces;

/// <summary>
/// Unit of Work abstraction that exposes repository instances and manages
/// transaction and persistence boundaries for a single logical operation.
/// Implementations must be registered in DI and are responsible for committing
/// or rolling back transactions and disposing the underlying DbContext.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Repository for user-specific persistence operations.
    /// </summary>
    IUserRepository UserRepository { get; }

    /// <summary>
    /// Repository for vacancy persistence and queries.
    /// </summary>
    IVacancyRepository VacancyRepository { get; }

    /// <summary>
    /// Repository for cover letters with update support.
    /// </summary>
    IEditableRepository<CoverLetter> CoverLetterRepository { get; }

    /// <summary>
    /// Repository for resumes with update support.
    /// </summary>
    IEditableRepository<Resume> ResumeRepository { get; }

    /// <summary>
    /// Repository for educations with update support.
    /// </summary>
    IEditableRepository<Education> EducationRepository { get; }

    /// <summary>
    /// Persists pending changes to the database. Implementations should forward
    /// the provided <paramref name="ct"/> to the underlying DbContext save call.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Begins a new database transaction scope for grouped operations.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct);

    /// <summary>
    /// Commits the active transaction, persisting changes atomically.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken ct);

    /// <summary>
    /// Rolls back the active transaction and discards pending changes.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken ct);
}