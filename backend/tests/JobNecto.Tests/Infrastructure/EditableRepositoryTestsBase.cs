using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

/// <summary>
/// A generic base class to standardize and enforce testing of <see cref="IEditableRepository{T}"/>.
/// It ensures that <see cref="IEditableRepository{T}.UpdateAsync"/> behaves consistently across all implementations,
/// specifically validating Entity Framework change tracking and deferred persistence behaviors.
/// </summary>
/// <typeparam name="TEntity">The domain entity type being tested.</typeparam>
/// <typeparam name="TRepository">The specific repository implementation type.</typeparam>
public abstract class EditableRepositoryTestsBase<TEntity, TRepository>
    where TEntity : BaseEntity
    where TRepository : IEditableRepository<TEntity>
{
    /// <summary>
    /// Creates an isolated, in-memory AppDbContext instance with a unique database name
    /// to prevent data bleed between individual test executions.
    /// </summary>
    protected static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Factory method to instantiate the specific repository being tested.
    /// </summary>
    protected abstract TRepository CreateRepository(AppDbContext context);

    /// <summary>
    /// Factory method to construct a valid, base entity used for insertion during setup.
    /// </summary>
    protected abstract TEntity CreateEntity();

    /// <summary>
    /// Mutates the entity to simulate a business logic update before passing it to <see cref="IEditableRepository{T}.UpdateAsync"/>.
    /// </summary>
    protected abstract void ModifyEntity(TEntity entity);

    /// <summary>
    /// Validates that the entity persisted to the database contains the modifications applied in <see cref="ModifyEntity"/>.
    /// </summary>
    protected abstract void AssertEntityModified(TEntity entity, TEntity fromDb);

    /// <summary>
    /// Validates that the entity persisted to the database remains in its original state and was NOT mutated.
    /// </summary>
    protected abstract void AssertEntityNotModified(TEntity entity, TEntity fromDb);

    /// <summary>
    /// Validates that invoking <c>UpdateAsync</c> only registers the intent to update, 
    /// and does not prematurely commit changes to the underlying database without an explicit <c>SaveChanges</c>.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_does_not_persist_until_SaveChanges()
    {
        await using var context = CreateContext();
        var entity = CreateEntity();
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();

        var repo = CreateRepository(context);
        ModifyEntity(entity);
        var returned = await repo.UpdateAsync(entity, CancellationToken.None);

        returned.Should().BeSameAs(entity);
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        var fromDb = await context.Set<TEntity>().AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        AssertEntityNotModified(entity, fromDb);
    }

    /// <summary>
    /// Validates the full update cycle: mutating the entity, calling <c>UpdateAsync</c>, 
    /// explicitly saving the context, and ensuring the database correctly reflects the new state.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_then_SaveChangesAsync_persists_changes()
    {
        await using var context = CreateContext();
        var entity = CreateEntity();
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();

        var repo = CreateRepository(context);
        ModifyEntity(entity);
        await repo.UpdateAsync(entity, CancellationToken.None);
        await context.SaveChangesAsync();

        var fromDb = await context.Set<TEntity>().AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        AssertEntityModified(entity, fromDb);
    }

    /// <summary>
    /// Validates that <c>UpdateAsync</c> correctly interacts with the EF Core Change Tracker 
    /// by transitioning the entity's state to <see cref="EntityState.Modified"/>.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_marks_entity_as_Modified()
    {
        await using var context = CreateContext();
        var entity = CreateEntity();
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        ModifyEntity(entity);

        var repo = CreateRepository(context);
        await repo.UpdateAsync(entity, CancellationToken.None);

        context.Entry(entity).State.Should().Be(EntityState.Modified);
    }
}
