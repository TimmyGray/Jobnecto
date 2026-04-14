using Microsoft.EntityFrameworkCore;

public class CoverLetterRepository : EditableRepository<CoverLetter>
{
    public CoverLetterRepository(DbContext context) : base(context)
    {
    }
}