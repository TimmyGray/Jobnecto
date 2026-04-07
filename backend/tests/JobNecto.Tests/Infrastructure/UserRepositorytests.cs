using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class UserRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UpdateAsync_does_not_persist_until_SaveChanges()
    {
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Login = "login",
            Password = "pass",
            Email = "before@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.Email = "after@example.com";
        var repo = new UserRepository(context);
        var returned = await repo.UpdateAsync(user, CancellationToken.None);

        returned.Should().BeSameAs(user);
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        var fromDb = await context.Users.AsNoTracking().SingleAsync(u => u.Id == id);
        fromDb.Email.Should().Be("before@example.com");
    }

    [Fact]
    public async Task UpdateAsync_then_SaveChangesAsync_persists_changes()
    {
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Login = "login",
            Password = "pass",
            Email = "before@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.Email = "after@example.com";
        var repo = new UserRepository(context);
        await repo.UpdateAsync(user, CancellationToken.None);
        await context.SaveChangesAsync();

        var fromDb = await context.Users.AsNoTracking().SingleAsync(u => u.Id == id);
        fromDb.Email.Should().Be("after@example.com");
    }

    [Fact]
    public async Task UpdateAsync_marks_entity_as_Modified()
    {
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Login = "login",
            Password = "pass",
            Email = "a@b.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        user.Email = "changed@example.com";

        var repo = new UserRepository(context);
        await repo.UpdateAsync(user, CancellationToken.None);

        context.Entry(user).State.Should().Be(EntityState.Modified);
    }
}
