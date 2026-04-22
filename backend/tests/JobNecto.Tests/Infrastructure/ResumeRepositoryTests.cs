using FluentAssertions;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;
using JobNecto.Domain.Entities;

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
}
