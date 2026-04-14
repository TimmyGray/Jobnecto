using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class CoverLetterRepositoryTests : EditableRepositoryTestsBase<CoverLetter, CoverLetterRepository>
{
    protected override CoverLetterRepository CreateRepository(AppDbContext context) => new CoverLetterRepository(context);

    protected override CoverLetter CreateEntity() => new CoverLetter
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        VacancyId = Guid.NewGuid(),
        Content = "Original Content",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    protected override void ModifyEntity(CoverLetter entity)
    {
        entity.Content = "Modified Content";
    }

    protected override void AssertEntityModified(CoverLetter entity, CoverLetter fromDb)
    {
        fromDb.Content.Should().Be("Modified Content");
    }

    protected override void AssertEntityNotModified(CoverLetter entity, CoverLetter fromDb)
    {
        fromDb.Content.Should().Be("Original Content");
    }
}
