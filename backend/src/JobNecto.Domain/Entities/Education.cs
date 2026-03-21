public sealed class Education : BaseEntity
{
    public Guid UserId;
    public required string Title;
    public required string Specialization;
    public required Degree Degree;
    public ICollection<Resume>? Resumes;
}
