using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;

public class CoverLetterTemplateRepositoryTests : EditableRepositoryTestsBase<CoverLetterTemplate, CoverLetterTemplateRepository>
{
    protected override CoverLetterTemplateRepository CreateRepository(AppDbContext context) => new CoverLetterTemplateRepository(context);

    protected override CoverLetterTemplate CreateEntity() => new CoverLetterTemplate
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Default Template",
        Content = "Original Content",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    protected override void ModifyEntity(CoverLetterTemplate entity)
    {
        entity.Name = "Updated Template";
    }

    protected override void AssertEntityModified(CoverLetterTemplate entity, CoverLetterTemplate fromDb)
    {
        fromDb.Name.Should().Be("Updated Template");
    }

    protected override void AssertEntityNotModified(CoverLetterTemplate entity, CoverLetterTemplate fromDb)
    {
        fromDb.Name.Should().Be("Default Template");
    }
}
