using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;
using JobNecto.Domain.Entities;

namespace JobNecto.Tests.Infrastructure;

public class SoftDeletableRepositoryTests
{
    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        var dbName = Guid.NewGuid().ToString();
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsIsDeletedAndDeletedAtUtc()
    {
        var options = CreateOptions();
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Test Resume",
            Skills = ["C#"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await using (var context = new AppDbContext(options))
        {
            await context.Resumes.AddAsync(resume);
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var repo = new ResumeRepository(context);
            var entity = await repo.GetByIdAsync(resume.Id, CancellationToken.None);
            await repo.SoftDeleteAsync(entity, CancellationToken.None);

            entity.IsDeleted.Should().BeTrue();
            entity.DeletedAt.Should().NotBeNull();
            entity.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_AfterSaveChanges_EntityExcludedFromQuery()
    {
        var options = CreateOptions();
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Test Resume",
            Skills = ["C#"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await using (var context = new AppDbContext(options))
        {
            await context.Resumes.AddAsync(resume);
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var repo = new ResumeRepository(context);
            var entity = await repo.GetByIdAsync(resume.Id, CancellationToken.None);
            await repo.SoftDeleteAsync(entity, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var results = await context.Resumes.ToListAsync();
            results.Should().NotContain(r => r.Id == resume.Id);
        }
    }
}
