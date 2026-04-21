using JobNecto.Domain.Enums;

namespace JobNecto.Domain.Entities;

public sealed class Education : SoftDeletableEntity
{
    public Guid UserId;
    public required string Title;
    public required string Specialization;
    public required Degree Degree;
    public ICollection<Resume>? Resumes;
}
