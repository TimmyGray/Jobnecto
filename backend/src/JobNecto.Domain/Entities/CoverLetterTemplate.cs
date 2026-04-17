public class CoverLetterTemplate : SoftDeletableEntity
{
    public Guid UserId;
    public required string Name;
    public required string Content;
}