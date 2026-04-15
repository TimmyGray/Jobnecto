using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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
}
