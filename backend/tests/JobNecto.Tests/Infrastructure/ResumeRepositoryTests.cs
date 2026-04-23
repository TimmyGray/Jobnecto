using FluentAssertions;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

public class ResumeRepositoryTests : EditableRepositoryTestsBase<Resume, ResumeRepository>
{
    protected override ResumeRepository CreateRepository(AppDbContext context) => new ResumeRepository(context);

    protected override Resume CreateEntity() => new Resume
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Title = "Original Resume",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    protected override void ModifyEntity(Resume entity)
    {
        entity.Title = "Modified Resume";
    }

    protected override void AssertEntityModified(Resume entity, Resume fromDb)
    {
        fromDb.Title.Should().Be("Modified Resume");
    }

    protected override void AssertEntityNotModified(Resume entity, Resume fromDb)
    {
        fromDb.Title.Should().Be("Original Resume");
    }

    [Fact]
    public async Task GetAsync_WithUserId_FiltersToOwnedResumesOnly()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);

        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        context.Resumes.AddRange(
            new Resume
            {
                Id = Guid.NewGuid(),
                UserId = ownerUserId,
                Title = "Owner resume A",
                CreatedAt = now.AddHours(-3),
                UpdatedAt = now.AddHours(-3),
            },
            new Resume
            {
                Id = Guid.NewGuid(),
                UserId = ownerUserId,
                Title = "Owner resume B",
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddHours(-2),
            },
            new Resume
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                Title = "Other user resume",
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now.AddHours(-1),
            }
        );

        await context.SaveChangesAsync();

        var result = await repository.GetAsync(
            new PagedQuery { UserId = ownerUserId, PageSize = 20 },
            CancellationToken.None
        );

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(r => r.UserId == ownerUserId);
        result.Items.Select(r => r.Title).Should().Equal("Owner resume B", "Owner resume A");
    }

    [Fact]
    public async Task GetAsync_WithForeignCursorAndUserId_IgnoresCursorOutsideUserScope()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);

        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var ownerNewest = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = ownerUserId,
            Title = "Owner newest",
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1),
        };

        var ownerOlder = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = ownerUserId,
            Title = "Owner older",
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-2),
        };

        var foreignCursor = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            Title = "Foreign cursor",
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Resumes.AddRange(ownerNewest, ownerOlder, foreignCursor);
        await context.SaveChangesAsync();

        var result = await repository.GetAsync(
            new PagedQuery
            {
                UserId = ownerUserId,
                PageSize = 20,
                LastSeenId = foreignCursor.Id,
                LastSeenUpdatedAt = foreignCursor.UpdatedAt,
            },
            CancellationToken.None
        );

        result.TotalCount.Should().Be(2);
        result.Items.Select(r => r.Id).Should().Equal(ownerNewest.Id, ownerOlder.Id);
    }
}
