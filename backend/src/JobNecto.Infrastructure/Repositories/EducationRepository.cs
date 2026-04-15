using Microsoft.EntityFrameworkCore;

public class EducationRepository : EditableRepository<Education>
{
    public EducationRepository(DbContext context) : base(context)
    {
    }
}