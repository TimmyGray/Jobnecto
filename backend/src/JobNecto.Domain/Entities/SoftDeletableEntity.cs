/// <summary>
/// Base class for entities that support soft deletion.
/// Soft-deleted records are excluded from queries via EF Core global query filters.
/// </summary>
public class SoftDeletableEntity : BaseEntity
{
    /// <summary>
    /// Indicates whether this entity has been soft-deleted.
    /// Soft-deleted entities are automatically excluded from all queries.
    /// </summary>
    public bool IsDeleted;

    /// <summary>
    /// Timestamp when this entity was soft-deleted.
    /// Used for grace period tracking before hard-delete (Phase E).
    /// </summary>
    public DateTime? DeletedAt;
}
