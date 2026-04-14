using Microsoft.EntityFrameworkCore;

public class CoverLetterTemplateRepository : EditableRepository<CoverLetterTemplate>
{
    public CoverLetterTemplateRepository(DbContext context) : base(context)
    {
    }
}