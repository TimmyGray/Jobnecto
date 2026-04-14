using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class EducationRepositoryTests : EditableRepositoryTestsBase<Education, EducationRepository>
{
    protected override EducationRepository CreateRepository(AppDbContext context) => new EducationRepository(context);

    protected override Education CreateEntity() => new Education
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Title = "Bachelor of Science",
        Specialization = "Computer Science",
        Degree = Degree.Bachelor,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    protected override void ModifyEntity(Education entity)
    {
        entity.Title = "Master of Science";
    }

    protected override void AssertEntityModified(Education entity, Education fromDb)
    {
        fromDb.Title.Should().Be("Master of Science");
    }

    protected override void AssertEntityNotModified(Education entity, Education fromDb)
    {
        fromDb.Title.Should().Be("Bachelor of Science");
    }
}
