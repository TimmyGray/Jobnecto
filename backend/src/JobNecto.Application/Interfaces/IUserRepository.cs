namespace JobNecto.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="User"/> persistence operations that are specific
/// to the <c>User</c> aggregate and are not covered by the generic <see cref="IRepository{T}"/>.
/// Implementations live in the Infrastructure layer and are registered via <c>IUnitOfWork</c>.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Retrieves a user by their email address.
    /// Returns <c>null</c> when no user with the provided email exists.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their login name.
    /// Returns <c>null</c> when no user with the provided login exists.
    /// </summary>
    Task<User?> GetByLoginAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when a non-deleted user with the given email exists.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when a non-deleted user with the given login exists.
    /// </summary>
    Task<bool> ExistsByLoginAsync(string login, CancellationToken ct = default);
}