using FluentAssertions;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;
using JobNecto.Domain.Entities;

public class UserRepositoryTests : EditableRepositoryTestsBase<User, UserRepository>
{
    protected override UserRepository CreateRepository(AppDbContext context) => new UserRepository(context);

    protected override User CreateEntity() => new User
    {
        Id = Guid.NewGuid(),
        Login = "testuser",
        Password = "password",
        Email = "original@example.com",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    protected override void ModifyEntity(User entity)
    {
        entity.Email = "modified@example.com";
    }

    protected override void AssertEntityModified(User entity, User fromDb)
    {
        fromDb.Email.Should().Be("modified@example.com");
    }

    protected override void AssertEntityNotModified(User entity, User fromDb)
    {
        fromDb.Email.Should().Be("original@example.com");
    }

    [Fact]
    public void UserModel_HasUniqueIndexes_ForEmailAndLogin()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(User));

        entityType.Should().NotBeNull();

        var uniqueIndexPropertySets = entityType!
            .GetIndexes()
            .Where(index => index.IsUnique)
            .Select(index => index.Properties.Select(p => p.Name).ToArray())
            .ToList();

        uniqueIndexPropertySets.Should().Contain(set =>
            set.Length == 1 && set[0] == nameof(User.Email));
        uniqueIndexPropertySets.Should().Contain(set =>
            set.Length == 1 && set[0] == nameof(User.Login));
    }
}
