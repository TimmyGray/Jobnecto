public sealed class CoverLetter : BaseEntity
{
    public Guid UserId;
    public Guid VacancyId;
    public required string Content;
}
