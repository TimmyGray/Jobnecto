using Microsoft.EntityFrameworkCore;

public class ResumeRepository : EditableRepository<Resume>
{
    public ResumeRepository(DbContext context) : base(context)
    {
    }
}