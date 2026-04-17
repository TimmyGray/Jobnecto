public sealed class CoverLetter : SoftDeletableEntity
{
    public Guid UserId;
    public Guid VacancyId;
    public required string Content;
}
